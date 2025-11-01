using System.Buffers;
using CommunityToolkit.HighPerformance;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using NATS.Client.ObjectStore;
using NATS.Client.ObjectStore.Models;

#pragma warning disable VSTHRD002
#pragma warning disable VSTHRD200

namespace Eshva.Caching.Nats;

/// <summary>
/// NATS object-store based distributed cache.
/// </summary>
[PublicAPI]
public sealed class NatsObjectStoreBasedCache1 : BufferDistributedCache {
  /// <summary>
  /// Initializes a new instance of a NATS object-store based distributed cache.
  /// </summary>
  /// <param name="cacheBucket">NATS object-store cache bucket.</param>
  /// <param name="cacheInvalidation">Cache invalidation.</param>
  /// <param name="logger">Logger.</param>
  /// <exception cref="ArgumentNullException">
  /// Value of a required argument isn't specified.
  /// </exception>
  public NatsObjectStoreBasedCache1(
    INatsObjStore cacheBucket,
    ObjectStoreBasedCacheInvalidation cacheInvalidation,
    ILogger<NatsObjectStoreBasedCache1>? logger = null) : base(cacheInvalidation, logger) {
    _cacheBucket = cacheBucket ?? throw new ArgumentNullException(nameof(cacheBucket));
  }

  /// <inheritdoc/>
  protected override async Task<CacheEntryExpiry> GetEntryExpiry(string key, CancellationToken cancellation) {
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
  protected override async Task RefreshEntry(string key, CacheEntryExpiry cacheEntryExpiry, CancellationToken cancellation) {
    try {
      var objectMetadata = await _cacheBucket.GetInfoAsync(key, showDeleted: false, cancellation)
        .ConfigureAwait(continueOnCapturedContext: false);
      var metadata = new CacheEntryMetadata(objectMetadata.Metadata);
      metadata.ExpiresAtUtc =
        CacheInvalidation.ExpiryCalculator.CalculateExpiration(metadata.AbsoluteExpirationUtc, metadata.SlidingExpiration);

      await _cacheBucket.UpdateMetaAsync(key, objectMetadata, cancellation).ConfigureAwait(continueOnCapturedContext: false);
    }
    catch (NatsObjException exception) {
      throw new InvalidOperationException($"An entry with key '{key}' could not be found in the cache.", exception);
    }
  }

  /// <inheritdoc/>
  protected override async Task RemoveEntry(string key, CancellationToken cancellation) {
    try {
      await _cacheBucket.DeleteAsync(key, cancellation).ConfigureAwait(continueOnCapturedContext: false);
    }
    catch (NatsObjException exception) {
      throw new InvalidOperationException($"An entry with key '{key}' could not be found in the cache.", exception);
    }
  }

  /// <inheritdoc/>
  protected override async Task<(bool, CacheEntryExpiry)> TryGetEntry(
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

      Logger.LogDebug(
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
  protected override async Task SetEntry(
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

      Logger.LogDebug(
        "An entry with the key '{Key}' has been put into cache. Cache entry metadata: @{ObjectMetadata}",
        key,
        objectMetadata);
    }
    catch (NatsObjException exception) {
      throw new InvalidOperationException($"Failed to put cache entry with key '{key}'.", exception);
    }
  }

  private Dictionary<string, string> FillCacheEntryMetadata(CacheEntryExpiry cacheEntryExpiry) =>
    new CacheEntryMetadata {
      SlidingExpiration = cacheEntryExpiry.SlidingExpiration,
      AbsoluteExpirationUtc = cacheEntryExpiry.AbsoluteExpirationUtc,
      ExpiresAtUtc = CacheInvalidation.ExpiryCalculator.CalculateExpiration(
        cacheEntryExpiry.AbsoluteExpirationUtc,
        cacheEntryExpiry.SlidingExpiration)
    };

  private readonly INatsObjStore _cacheBucket;
}
