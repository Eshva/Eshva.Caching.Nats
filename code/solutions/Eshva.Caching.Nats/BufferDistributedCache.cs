using System.Buffers;
using Eshva.Caching.Abstractions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NATS.Client.Core;

#pragma warning disable VSTHRD002
#pragma warning disable VSTHRD200

namespace Eshva.Caching.Nats;

/// <summary>
/// 
/// </summary>
public abstract class BufferDistributedCache : IBufferDistributedCache {
  /// <summary>
  /// Initializes a new instance of a buffer distributed cache.
  /// </summary>
  /// <param name="cacheInvalidation">Cache invalidation.</param>
  /// <param name="logger">Logger.</param>
  /// <exception cref="ArgumentNullException">
  /// Value of a required argument isn't specified.
  /// </exception>
  protected BufferDistributedCache(
    TimeBasedCacheInvalidation cacheInvalidation,
    ILogger? logger = null) {
    CacheInvalidation = cacheInvalidation ?? throw new ArgumentNullException(nameof(cacheInvalidation));
    Logger = logger ?? new NullLogger<BufferDistributedCache>();
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
  public byte[]? Get(string key) =>
    GetAsync(key).GetAwaiter().GetResult();

  /// <summary>
  /// Get value of a cache key <paramref name="key"/>.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Read the key <paramref name="key"/> value from the object-store bucket and returns it as a byte array if it's
  /// found, otherwise returns <c>null</c>. If entry has a sliding expiration its expiration time could be refreshed.
  /// </para>
  /// <para>
  /// If it's the time purge all expired entries in the cache.
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
    CacheInvalidation.PurgeEntriesIfRequired(token);

    using var destination = new NatsBufferWriter<byte>();
    var (isEntryGotten, cacheEntryExpiry) = await TryGetEntry(key, destination, token).ConfigureAwait(continueOnCapturedContext: false);
    if (!isEntryGotten) return null;

    await RefreshEntry(key, UpdateCacheEntryExpiry(cacheEntryExpiry), token).ConfigureAwait(continueOnCapturedContext: false);

    return destination.WrittenMemory.ToArray();
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
  public void Set(string key, byte[] value, DistributedCacheEntryOptions options) =>
    SetAsync(key, value, options).GetAwaiter().GetResult();

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
    CacheInvalidation.PurgeEntriesIfRequired(token);
    await SetEntry(
      key,
      new ReadOnlySequence<byte>(value),
      MakeCacheEntryExpiry(options),
      token);
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
  public void Refresh(string key) =>
    RefreshAsync(key).GetAwaiter().GetResult();

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
    CacheInvalidation.PurgeEntriesIfRequired(token);
    var cacheEntryExpiry = await GetEntryExpiry(key, token);
    await RefreshEntry(key, UpdateCacheEntryExpiry(cacheEntryExpiry), token).ConfigureAwait(continueOnCapturedContext: false);
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
  public void Remove(string key) =>
    RemoveAsync(key).GetAwaiter().GetResult();

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
    CacheInvalidation.PurgeEntriesIfRequired(token);
    await RemoveEntry(key, token);
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
  public bool TryGet(string key, IBufferWriter<byte> destination) =>
    TryGetAsync(key, destination).AsTask().GetAwaiter().GetResult();

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
    CacheInvalidation.PurgeEntriesIfRequired(token);
    var (isEntryGotten, cacheEntryExpiry) = await TryGetEntry(
      key,
      destination,
      token);
    if (!isEntryGotten) return false;

    await RefreshEntry(key, UpdateCacheEntryExpiry(cacheEntryExpiry), token).ConfigureAwait(continueOnCapturedContext: false);
    return true;
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
    CacheInvalidation.PurgeEntriesIfRequired(token);
    await SetEntry(
      key,
      value,
      MakeCacheEntryExpiry(options),
      token);
  }

  /// <summary>
  /// 
  /// </summary>
  protected TimeBasedCacheInvalidation CacheInvalidation { get; }

  /// <summary>
  /// 
  /// </summary>
  protected ILogger Logger { get; }

  /// <summary>
  /// 
  /// </summary>
  /// <param name="key"></param>
  /// <param name="cancellation"></param>
  /// <returns></returns>
  protected abstract Task<CacheEntryExpiry> GetEntryExpiry(string key, CancellationToken cancellation);

  /// <summary>
  /// 
  /// </summary>
  /// <param name="key"></param>
  /// <param name="cacheEntryExpiry"></param>
  /// <param name="cancellation"></param>
  /// <returns></returns>
  protected abstract Task RefreshEntry(string key, CacheEntryExpiry cacheEntryExpiry, CancellationToken cancellation);

  /// <summary>
  /// 
  /// </summary>
  /// <param name="key"></param>
  /// <param name="cancellation"></param>
  /// <returns></returns>
  protected abstract Task RemoveEntry(string key, CancellationToken cancellation);

  /// <summary>
  /// 
  /// </summary>
  /// <param name="key"></param>
  /// <param name="destination"></param>
  /// <param name="cancellation"></param>
  /// <returns></returns>
  protected abstract Task<(bool, CacheEntryExpiry)> TryGetEntry(
    string key,
    IBufferWriter<byte> destination,
    CancellationToken cancellation);

  /// <summary>
  /// 
  /// </summary>
  /// <param name="key"></param>
  /// <param name="value"></param>
  /// <param name="cacheEntryExpiry"></param>
  /// <param name="cancellation"></param>
  /// <returns></returns>
  protected abstract Task SetEntry(
    string key,
    ReadOnlySequence<byte> value,
    CacheEntryExpiry cacheEntryExpiry,
    CancellationToken cancellation);

  /// <summary>
  /// 
  /// </summary>
  /// <param name="key"></param>
  /// <exception cref="ArgumentException"></exception>
  protected virtual void ValidateKey(string key) {
    if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(key));
  }

  private CacheEntryExpiry UpdateCacheEntryExpiry(CacheEntryExpiry cacheEntryExpiry) =>
    cacheEntryExpiry with {
      ExpiresAtUtc = CacheInvalidation.ExpiryCalculator.CalculateExpiration(
        cacheEntryExpiry.AbsoluteExpirationUtc,
        cacheEntryExpiry.SlidingExpiration)
    };

  private CacheEntryExpiry MakeCacheEntryExpiry(DistributedCacheEntryOptions options) {
    var absoluteExpirationUtc = CacheInvalidation.ExpiryCalculator.CalculateAbsoluteExpiration(
      options.AbsoluteExpiration,
      options.AbsoluteExpirationRelativeToNow);
    var expiresAtUtc = CacheInvalidation.ExpiryCalculator.CalculateExpiration(absoluteExpirationUtc, options.SlidingExpiration);
    var cacheEntryExpiry = new CacheEntryExpiry(expiresAtUtc, absoluteExpirationUtc, options.SlidingExpiration);
    return cacheEntryExpiry;
  }
}
