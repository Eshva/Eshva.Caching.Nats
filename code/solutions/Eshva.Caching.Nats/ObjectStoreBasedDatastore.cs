using System.Buffers;
using CommunityToolkit.HighPerformance;
using Eshva.Caching.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NATS.Client.ObjectStore;
using NATS.Client.ObjectStore.Models;

namespace Eshva.Caching.Nats;

/// <summary>
/// NATS object store based cache datastore.
/// </summary>
public sealed class ObjectStoreBasedDatastore : ICacheDatastore {
  /// <summary>
  ///
  /// </summary>
  /// <param name="cacheBucket"></param>
  /// <param name="expiryCalculator"></param>
  /// <param name="logger"></param>
  /// <exception cref="ArgumentNullException"></exception>
  public ObjectStoreBasedDatastore(
    INatsObjStore cacheBucket,
    CacheEntryExpiryCalculator expiryCalculator,
    ILogger<ObjectStoreBasedDatastore>? logger = null) {
    _cacheBucket = cacheBucket ?? throw new ArgumentNullException(nameof(cacheBucket));
    _expiryCalculator = expiryCalculator;
    _logger = logger ?? new NullLogger<ObjectStoreBasedDatastore>();
  }

  /// <inheritdoc/>
  public async Task<CacheEntryExpiry> GetEntryExpiry(string key, CancellationToken cancellation) {
    try {
      var objectMetadata = await _cacheBucket.GetInfoAsync(key, showDeleted: false, cancellation)
        .ConfigureAwait(continueOnCapturedContext: false);
      var cacheEntryMetadata = new CacheEntryMetadata(objectMetadata.Metadata);
      return new CacheEntryExpiry(
        cacheEntryMetadata.ExpiresAtUtc,
        cacheEntryMetadata.AbsoluteExpirationUtc,
        cacheEntryMetadata.SlidingExpiration);
    }
    catch (NatsObjException exception) {
      throw new InvalidOperationException($"An entry with key '{key}' could not be found in the cache.", exception);
    }
  }

  /// <inheritdoc/>
  public async Task RefreshEntry(string key, CacheEntryExpiry cacheEntryExpiry, CancellationToken cancellation) {
    try {
      var objectMetadata = await _cacheBucket.GetInfoAsync(key, showDeleted: false, cancellation)
        .ConfigureAwait(continueOnCapturedContext: false);
      var metadata = new CacheEntryMetadata(objectMetadata.Metadata);
      metadata.ExpiresAtUtc = _expiryCalculator.CalculateExpiration(metadata.AbsoluteExpirationUtc, metadata.SlidingExpiration);

      await _cacheBucket.UpdateMetaAsync(key, objectMetadata, cancellation).ConfigureAwait(continueOnCapturedContext: false);
    }
    catch (NatsObjException exception) {
      throw new InvalidOperationException($"An entry with key '{key}' could not be found in the cache.", exception);
    }
  }

  /// <inheritdoc/>
  public async Task RemoveEntry(string key, CancellationToken cancellation) {
    try {
      await _cacheBucket.DeleteAsync(key, cancellation).ConfigureAwait(continueOnCapturedContext: false);
    }
    catch (NatsObjException exception) {
      throw new InvalidOperationException($"An entry with key '{key}' could not be found in the cache.", exception);
    }
  }

  /// <inheritdoc/>
  public async Task<(bool isEntryGotten, CacheEntryExpiry cacheEntryExpiry)> TryGetEntry(
    string key,
    IBufferWriter<byte> destination,
    CancellationToken cancellation) {
    try {
      var objectMetadata = await _cacheBucket.GetAsync(
          key,
          destination.AsStream(),
          leaveOpen: true,
          cancellation)
        .ConfigureAwait(continueOnCapturedContext: false);
      var metadata = new CacheEntryMetadata(objectMetadata.Metadata);
      var cacheEntryExpiry = new CacheEntryExpiry(metadata.ExpiresAtUtc, metadata.AbsoluteExpirationUtc, metadata.SlidingExpiration);

      _logger.LogDebug(
        "An object with the key '{Key}' has been read. Object meta-data: @{ObjectMetadata}",
        key,
        objectMetadata);

      return (true, cacheEntryExpiry);
    }
    catch (NatsObjNotFoundException) {
      return (false, new CacheEntryExpiry(DateTimeOffset.MinValue, AbsoluteExpirationUtc: null, SlidingExpiration: null));
    }
    catch (NatsObjException exception) {
      throw new InvalidOperationException($"Failed to read cache key '{key}' value.", exception);
    }
  }

  /// <inheritdoc/>
  public async Task SetEntry(
    string key,
    ReadOnlySequence<byte> value,
    CacheEntryExpiry cacheEntryExpiry,
    CancellationToken cancellation) {
    try {
      var metadata = FillCacheEntryMetadata(cacheEntryExpiry);
      var objectMetadata = await _cacheBucket.PutAsync(
          new ObjectMetadata { Name = key, Metadata = metadata },
          value.AsStream(),
          leaveOpen: true,
          cancellation)
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

  /// <inheritdoc/>
  public void ValidateKey(string key) {
    if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Cache entry key can't be null or whitespace.", nameof(key));
  }

  private Dictionary<string, string> FillCacheEntryMetadata(CacheEntryExpiry cacheEntryExpiry) =>
    new CacheEntryMetadata {
      SlidingExpiration = cacheEntryExpiry.SlidingExpiration,
      AbsoluteExpirationUtc = cacheEntryExpiry.AbsoluteExpirationUtc,
      ExpiresAtUtc = _expiryCalculator.CalculateExpiration(
        cacheEntryExpiry.AbsoluteExpirationUtc,
        cacheEntryExpiry.SlidingExpiration)
    };

  private readonly INatsObjStore _cacheBucket;
  private readonly CacheEntryExpiryCalculator _expiryCalculator;
  private readonly ILogger<ObjectStoreBasedDatastore> _logger;
}
