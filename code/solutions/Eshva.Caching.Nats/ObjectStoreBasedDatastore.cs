using System.Buffers;
using CommunityToolkit.HighPerformance;
using Eshva.Caching.Abstractions.Distributed;
using NATS.Client.ObjectStore;
using NATS.Client.ObjectStore.Models;

namespace Eshva.Caching.Nats;

/// <summary>
/// NATS object store based cache datastore.
/// </summary>
public sealed class ObjectStoreBasedDatastore : ICacheDatastore {
  /// <summary>
  /// Initializes a new instance of NATS object store based datastore.
  /// </summary>
  /// <param name="cacheBucket">Cache bucket.</param>
  /// <param name="expiryCalculator">Cache entry expiry calculator.</param>
  /// <exception cref="ArgumentNullException">
  /// Value of a required parameter not specified.
  /// </exception>
  public ObjectStoreBasedDatastore(
    INatsObjStore cacheBucket,
    CacheEntryExpiryCalculator expiryCalculator) {
    _cacheBucket = cacheBucket ?? throw new ArgumentNullException(nameof(cacheBucket));
    _expiryCalculator = expiryCalculator ?? throw new ArgumentNullException(nameof(expiryCalculator));
  }

  /// <inheritdoc/>
  public async Task<CacheEntryExpiry> GetEntryExpiry(string key, CancellationToken cancellation) {
    try {
      var objectMetadata = await _cacheBucket.GetInfoAsync(key, showDeleted: false, cancellation)
        .ConfigureAwait(continueOnCapturedContext: false);
      var cacheEntryMetadata = new ObjectMetadataAccessor(objectMetadata);
      return new CacheEntryExpiry(
        cacheEntryMetadata.ExpiresAtUtc,
        cacheEntryMetadata.AbsoluteExpiryAtUtc,
        cacheEntryMetadata.SlidingExpiryInterval);
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
      var metadata = new ObjectMetadataAccessor(objectMetadata);
      metadata.ExpiresAtUtc = _expiryCalculator.CalculateExpiration(metadata.AbsoluteExpiryAtUtc, metadata.SlidingExpiryInterval);

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
      var metadata = new ObjectMetadataAccessor(objectMetadata);
      var cacheEntryExpiry = new CacheEntryExpiry(metadata.ExpiresAtUtc, metadata.AbsoluteExpiryAtUtc, metadata.SlidingExpiryInterval);
      return (true, cacheEntryExpiry);
    }
    catch (NatsObjNotFoundException) {
      return (false, new CacheEntryExpiry(DateTimeOffset.MinValue, AbsoluteExpiryAtUtc: null, SlidingExpiryInterval: null));
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
      var objectMetadata = MakeCacheEntryObjectMetadata(key, cacheEntryExpiry);
      await _cacheBucket.PutAsync(
          objectMetadata,
          value.AsStream(),
          leaveOpen: true,
          cancellation)
        .ConfigureAwait(continueOnCapturedContext: false);
    }
    catch (NatsObjException exception) {
      throw new InvalidOperationException($"Failed to put cache entry with key '{key}'.", exception);
    }
  }

  /// <inheritdoc/>
  public void ValidateKey(string key) {
    if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Cache entry key can't be null or whitespace.", nameof(key));
  }

  private ObjectMetadata MakeCacheEntryObjectMetadata(string key, CacheEntryExpiry cacheEntryExpiry) {
    var metadataAccessor = new ObjectMetadataAccessor(new ObjectMetadata { Name = key }) {
      SlidingExpiryInterval = cacheEntryExpiry.SlidingExpiryInterval,
      AbsoluteExpiryAtUtc = cacheEntryExpiry.AbsoluteExpiryAtUtc,
      ExpiresAtUtc = _expiryCalculator.CalculateExpiration(cacheEntryExpiry.AbsoluteExpiryAtUtc, cacheEntryExpiry.SlidingExpiryInterval)
    };
    return metadataAccessor.ObjectMetadata;
  }

  private readonly INatsObjStore _cacheBucket;
  private readonly CacheEntryExpiryCalculator _expiryCalculator;
}
