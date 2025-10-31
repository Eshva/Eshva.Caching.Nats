using System.Buffers;
using CommunityToolkit.HighPerformance;
using Eshva.Caching.Abstractions;
using JetBrains.Annotations;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NATS.Client.ObjectStore;
using NATS.Client.ObjectStore.Models;

#pragma warning disable VSTHRD002
#pragma warning disable VSTHRD200

namespace Eshva.Caching.Nats;

/// <summary>
/// NATS object-store based distributed cache.
/// </summary>
[PublicAPI]
public sealed class NatsObjectStoreBasedCache : IBufferDistributedCache {
  /// <summary>
  /// Initializes a new instance of a NATS object-store based distributed cache.
  /// </summary>
  /// <param name="cacheBucket">NATS object-store cache bucket.</param>
  /// <param name="cacheInvalidation">Cache invalidation.</param>
  /// <param name="logger">Logger.</param>
  /// <exception cref="ArgumentNullException">
  /// Value of a required argument isn't specified.
  /// </exception>
  public NatsObjectStoreBasedCache(
    INatsObjStore cacheBucket,
    ObjectStoreBasedCacheInvalidation cacheInvalidation,
    ILogger<NatsObjectStoreBasedCache>? logger = null) {
    _cacheBucket = cacheBucket ?? throw new ArgumentNullException(nameof(cacheBucket));
    _cacheInvalidation = cacheInvalidation ?? throw new ArgumentNullException(nameof(cacheInvalidation));
    _logger = logger ?? new NullLogger<NatsObjectStoreBasedCache>();
  }

  /// <summary>
  /// Get value of a cache key <paramref name="key"/>.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Read the key <paramref name="key"/> value from the object-store bucket and returns it as a byte array if it's
  /// found. If entry has a sliding expiration its expiration time could be refreshed (depends on the purger used).
  /// </para>
  /// <para>
  /// If it's time purge all expired entries in the cache.
  /// </para>
  /// </remarks>
  /// <param name="key">Cache entry key.</param>
  /// <returns>
  /// Depending on different circumstances returns:
  /// <list type="bullet">
  /// <item>byte array - read key value,</item>
  /// <item>null - cache key <paramref name="key"/> isn't found.</item>
  /// </list>
  /// </returns>
  /// <exception cref="ArgumentException">
  /// The key is not specified.
  /// </exception>
  /// <exception cref="InvalidOperationException">
  /// One of:
  /// <list type="bullet">
  /// <item>Failed to read cache key value.</item>
  /// <item>Length of the read value less than length of the cache value.</item>
  /// </list>
  /// </exception>
  public byte[]? Get(string key) => GetAsync(key).GetAwaiter().GetResult();

  /// <summary>
  /// Get value of a cache key <paramref name="key"/>.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Read the key <paramref name="key"/> value from the object-store bucket and returns it as a byte array if it's
  /// found. If entry has a sliding expiration its expiration time could be refreshed (depends on the purger used).
  /// </para>
  /// <para>
  /// If it's time purge all expired entries in the cache.
  /// </para>
  /// </remarks>
  /// <param name="key">Cache entry key.</param>
  /// <param name="token">Cancellation token.</param>
  /// <returns>
  /// Depending on different circumstances returns:
  /// <list type="bullet">
  /// <item>byte array - read key value,</item>
  /// <item>null - cache key <paramref name="key"/> isn't found.</item>
  /// </list>
  /// </returns>
  /// <exception cref="ArgumentException">
  /// The key is not specified.
  /// </exception>
  /// <exception cref="InvalidOperationException">
  /// One of:
  /// <list type="bullet">
  /// <item>Failed to read cache key value.</item>
  /// <item>Length of the read value less than length of the cache value.</item>
  /// </list>
  /// </exception>
  public async Task<byte[]?> GetAsync(string key, CancellationToken token = default) {
    ValidateKey(key);
    _cacheInvalidation.PurgeEntriesIfRequired(token);

    var valueStream = new MemoryStream();

    try {
      var objectMetadata = await _cacheBucket.GetAsync(
          key,
          valueStream,
          leaveOpen: true,
          token)
        .ConfigureAwait(continueOnCapturedContext: false);
      _logger.LogDebug(
        "An object with the key '{Key}' has been read. Object meta-data: @{ObjectMetadata}",
        key,
        objectMetadata);

      await RefreshExpiresAt(objectMetadata, token).ConfigureAwait(continueOnCapturedContext: false);
    }
    catch (NatsObjNotFoundException) {
      return null;
    }
    catch (NatsObjException exception) {
      throw new InvalidOperationException($"Failed to read cache key '{key}' value.", exception);
    }

    if (valueStream.Length == 0) {
      _logger.LogDebug("No key '{Key}' has been found in the object-store", key);
      return null;
    }

    valueStream.Seek(offset: 0, SeekOrigin.Begin);
    var buffer = new byte[valueStream.Length];
    var bytesRead = await valueStream.ReadAsync(buffer, token).ConfigureAwait(continueOnCapturedContext: false);

    if (bytesRead != valueStream.Length) {
      throw new InvalidOperationException(
        $"Should be read {valueStream.Length} bytes but read {bytesRead} bytes for a cache entry wih ID '{key}'.");
    }

    return buffer;
  }

  /// <summary>
  /// Set a cache entry value.
  /// </summary>
  /// <param name="key">The key of cache entry.</param>
  /// <param name="value">The value of cache entry.</param>
  /// <param name="options">Expiration options.</param>
  /// <exception cref="ArgumentNullException">
  /// The key is not specified.
  /// </exception>
  public void Set(string key, byte[] value, DistributedCacheEntryOptions options) => SetAsync(key, value, options).GetAwaiter().GetResult();

  /// <summary>
  /// Set a cache entry value.
  /// </summary>
  /// <param name="key">The key of cache entry.</param>
  /// <param name="value">The value of cache entry.</param>
  /// <param name="options">Expiration options.</param>
  /// <param name="token">Cancellation token.</param>
  /// <exception cref="ArgumentNullException">
  /// The key is not specified.
  /// </exception>
  public async Task SetAsync(
    string key,
    byte[] value,
    DistributedCacheEntryOptions options,
    CancellationToken token = new()) {
    ValidateKey(key);
    _cacheInvalidation.PurgeEntriesIfRequired(token);

    try {
      var objectMetadata = await _cacheBucket.PutAsync(key, value, token).ConfigureAwait(continueOnCapturedContext: false);
      objectMetadata.Metadata = FillCacheEntryMetadata(options);
      await _cacheBucket.UpdateMetaAsync(key, objectMetadata, token).ConfigureAwait(continueOnCapturedContext: false);
      _logger.LogDebug("An entry with '{Key}' put into cache", key);
    }
    catch (NatsObjException exception) {
      throw new InvalidOperationException($"An entry with key '{key}' could not be found in the cache.", exception);
    }
  }

  /// <summary>
  /// Refresh expiration time of the cache entry with <paramref name="key"/>.
  /// </summary>
  /// <param name="key">The key of refreshing cache entry.</param>
  /// <exception cref="ArgumentNullException">
  /// The key is not specified.
  /// </exception>
  /// <exception cref="InvalidOperationException">
  /// Cache entry with <paramref name="key"/> not found.
  /// </exception>
  public void Refresh(string key) => RefreshAsync(key).GetAwaiter().GetResult();

  /// <summary>
  /// Refresh expiration time of the cache entry with <paramref name="key"/>.
  /// </summary>
  /// <param name="key">The key of refreshing cache entry.</param>
  /// <param name="token">Cancellation token.</param>
  /// <exception cref="ArgumentNullException">
  /// The key is not specified.
  /// </exception>
  /// <exception cref="InvalidOperationException">
  /// Cache entry with <paramref name="key"/> not found.
  /// </exception>
  public async Task RefreshAsync(string key, CancellationToken token = new()) {
    ValidateKey(key);
    _cacheInvalidation.PurgeEntriesIfRequired(token);

    try {
      var objectMetadata = await _cacheBucket.GetInfoAsync(key, showDeleted: false, token).ConfigureAwait(continueOnCapturedContext: false);
      await RefreshExpiresAt(objectMetadata, token).ConfigureAwait(continueOnCapturedContext: false);
    }
    catch (NatsObjException exception) {
      throw new InvalidOperationException($"An entry with key '{key}' could not be found in the cache.", exception);
    }
  }

  /// <summary>
  /// Remove a cache entry.
  /// </summary>
  /// <remarks>
  /// If cache entry doesn't exist or removed no exception will be thrown.
  /// </remarks>
  /// <param name="key">The key of removing cache entry.</param>
  /// <exception cref="ArgumentNullException">
  /// The key is not specified.
  /// </exception>
  /// <exception cref="InvalidOperationException">
  /// Cache entry metadata are corrupted.
  /// </exception>
  public void Remove(string key) => RemoveAsync(key).GetAwaiter().GetResult();

  /// <summary>
  /// Remove a cache entry.
  /// </summary>
  /// <remarks>
  /// If cache entry doesn't exist or removed no exception will be thrown.
  /// </remarks>
  /// <param name="key">The key of removing cache entry.</param>
  /// <param name="token">Cancellation token.</param>
  /// <exception cref="ArgumentNullException">
  /// The key is not specified.
  /// </exception>
  /// <exception cref="InvalidOperationException">
  /// Cache entry metadata are corrupted.
  /// </exception>
  public async Task RemoveAsync(string key, CancellationToken token = new()) {
    ValidateKey(key);
    _cacheInvalidation.PurgeEntriesIfRequired(token);

    try {
      await _cacheBucket.DeleteAsync(key, token).ConfigureAwait(continueOnCapturedContext: false);
    }
    catch (NatsObjException exception) {
      throw new InvalidOperationException($"An occurred on removing entry with key '{key}'.", exception);
    }
  }

  /// <summary>
  /// Try to get a cache entry.
  /// </summary>
  /// <param name="key">Cache entry key.</param>
  /// <param name="destination">Buffer writer to write cache entry value into.</param>
  /// <exception cref="ArgumentNullException">
  /// The key is not specified.
  /// </exception>
  /// <returns>
  /// <c>true</c> - value successfully read, <c>false</c> - entry not found in the cache.
  /// </returns>
  /// <exception cref="ArgumentNullException">
  /// The key is not specified.
  /// </exception>
  /// <exception cref="InvalidOperationException">
  /// Failed to read cache key value.
  /// </exception>
  public bool TryGet(string key, IBufferWriter<byte> destination) => TryGetAsync(key, destination).AsTask().GetAwaiter().GetResult();

  /// <summary>
  /// Try to get a cache entry.
  /// </summary>
  /// <param name="key">Cache entry key.</param>
  /// <param name="destination">Buffer writer to write cache entry value into.</param>
  /// <param name="token">Cancellation token.</param>
  /// <returns>
  /// <c>true</c> - value successfully read, <c>false</c> - entry not found in the cache.
  /// </returns>
  /// <exception cref="ArgumentNullException">
  /// The key is not specified.
  /// </exception>
  /// <exception cref="InvalidOperationException">
  /// Failed to read cache key value.
  /// </exception>
  public async ValueTask<bool> TryGetAsync(string key, IBufferWriter<byte> destination, CancellationToken token = new()) {
    ValidateKey(key);
    _cacheInvalidation.PurgeEntriesIfRequired(token);

    try {
      var objectMetadata = await _cacheBucket.GetAsync(
          key,
          destination.AsStream(),
          leaveOpen: true,
          token)
        .ConfigureAwait(continueOnCapturedContext: false);

      _logger.LogDebug(
        "An object with the key '{Key}' has been read. Object meta-data: @{ObjectMetadata}",
        key,
        objectMetadata);

      await RefreshExpiresAt(objectMetadata, token).ConfigureAwait(continueOnCapturedContext: false);

      return true;
    }
    catch (NatsObjNotFoundException) {
      return false;
    }
    catch (NatsObjException exception) {
      throw new InvalidOperationException($"Failed to read cache key '{key}' value.", exception);
    }
  }

  /// <summary>
  /// Set cache entry with <paramref name="key"/> with <paramref name="value"/>.
  /// </summary>
  /// <param name="key">Cache entry key.</param>
  /// <param name="value">Cache entry value.</param>
  /// <param name="options">Expiration options.</param>
  /// <exception cref="ArgumentNullException">
  /// The key is not specified.
  /// </exception>
  /// <exception cref="InvalidOperationException">
  /// Failed to set cache key value.
  /// </exception>
  public void Set(string key, ReadOnlySequence<byte> value, DistributedCacheEntryOptions options) =>
    SetAsync(key, value, options).AsTask().GetAwaiter().GetResult();

  /// <summary>
  /// Set cache entry with <paramref name="key"/> with <paramref name="value"/>.
  /// </summary>
  /// <param name="key">Cache entry key.</param>
  /// <param name="value">Cache entry value.</param>
  /// <param name="options">Expiration options.</param>
  /// <param name="token">Cancellation token.</param>
  /// <exception cref="ArgumentNullException">
  /// The key is not specified.
  /// </exception>
  /// <exception cref="InvalidOperationException">
  /// Failed to set cache key value.
  /// </exception>
  public async ValueTask SetAsync(
    string key,
    ReadOnlySequence<byte> value,
    DistributedCacheEntryOptions options,
    CancellationToken token = default) {
    ValidateKey(key);
    _cacheInvalidation.PurgeEntriesIfRequired(token);

    try {
      var objectMetadata = await _cacheBucket.PutAsync(
          new ObjectMetadata { Name = key, Metadata = FillCacheEntryMetadata(options) },
          value.AsStream(),
          leaveOpen: true,
          token)
        .ConfigureAwait(continueOnCapturedContext: false);

      _logger.LogDebug(
        "An entry with the key '{Key}' has been put into cache. Cache entry metadata: @{ObjectMetadata}",
        key,
        objectMetadata);
    }
    catch (NatsObjException exception) {
      throw new InvalidOperationException($"Failed to put cache entry with key '{key}'.", exception);
    }
  }

  private Dictionary<string, string> FillCacheEntryMetadata(DistributedCacheEntryOptions options) {
    var absoluteExpirationUtc = _cacheInvalidation.ExpiryCalculator.CalculateAbsoluteExpiration(
      options.AbsoluteExpiration,
      options.AbsoluteExpirationRelativeToNow);
    return new CacheEntryMetadata {
      SlidingExpiration = options.SlidingExpiration,
      AbsoluteExpirationUtc = absoluteExpirationUtc,
      ExpiresAtUtc = _cacheInvalidation.ExpiryCalculator.CalculateExpiration(absoluteExpirationUtc, options.SlidingExpiration)
    };
  }

  private async Task RefreshExpiresAt(ObjectMetadata objectMetadata, CancellationToken token) {
    objectMetadata.Metadata ??= new Dictionary<string, string>();
    var entryMetadata = new CacheEntryMetadata(objectMetadata.Metadata);
    entryMetadata.ExpiresAtUtc = _cacheInvalidation.ExpiryCalculator.CalculateExpiration(
      entryMetadata.AbsoluteExpirationUtc,
      entryMetadata.SlidingExpiration);
    await _cacheBucket.UpdateMetaAsync(objectMetadata.Name, objectMetadata, token).ConfigureAwait(continueOnCapturedContext: false);
  }

  private static void ValidateKey(string key) {
    if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(key));
  }

  private readonly INatsObjStore _cacheBucket;
  private readonly TimeBasedCacheInvalidation _cacheInvalidation;
  private readonly ILogger<NatsObjectStoreBasedCache> _logger;
}
