using Eshva.Caching.Abstractions;
using Microsoft.Extensions.Logging;
using NATS.Client.ObjectStore;

namespace Eshva.Caching.Nats;

/// <summary>
/// Expired entries purger for NATS object store based cache.
/// </summary>
public sealed class ObjectStoreBasedCacheInvalidation : TimeBasedCacheInvalidation {
  /// <summary>
  /// Initializes a new instance of expired entries purger for NATS object-store based cache.
  /// </summary>
  /// <param name="cacheBucket">NATS object-store cache bucket.</param>
  /// <param name="expiredEntriesPurgingInterval">Expired entries purging interval.</param>
  /// <param name="expiryCalculator">Cache entry expiry calculator.</param>
  /// <param name="timeProvider">Time provider.</param>
  /// <param name="logger">Logger.</param>
  /// <exception cref="ArgumentNullException">
  /// Value of a required parameter not specified.
  /// </exception>
  public ObjectStoreBasedCacheInvalidation(
    INatsObjStore cacheBucket,
    TimeSpan expiredEntriesPurgingInterval,
    CacheEntryExpiryCalculator expiryCalculator,
    TimeProvider timeProvider,
    ILogger<ObjectStoreBasedCacheInvalidation>? logger = null)
    : base(
      expiredEntriesPurgingInterval,
      expiryCalculator,
      timeProvider,
      logger) {
    _cacheBucket = cacheBucket ?? throw new ArgumentNullException(nameof(cacheBucket));
    _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
  }

  /// <inheritdoc/>
  protected override async Task<CacheInvalidationStatistics> DeleteExpiredCacheEntries(CancellationToken cancellation) {
    Logger.LogDebug("Deleting expired entries started at {CurrentTime}", _timeProvider.GetUtcNow());

    uint totalCount = 0;
    uint purgedCount = 0;
    await foreach (var entry in _cacheBucket.ListAsync(cancellationToken: cancellation).ConfigureAwait(continueOnCapturedContext: false)) {
      totalCount++;
      Logger.LogDebug("Entry '{Key}' expires at {ExpiresAt}", entry.Name, new ObjectMetadataAccessor(entry).ExpiresAtUtc);
      if (!ExpiryCalculator.IsCacheEntryExpired(new ObjectMetadataAccessor(entry).ExpiresAtUtc)) continue;

      purgedCount++;
      await _cacheBucket.DeleteAsync(entry.Name, cancellation).ConfigureAwait(continueOnCapturedContext: false);
      Logger.LogDebug("Deleted expired entry '{Key}'", entry.Name);
    }

    Logger.LogDebug(
      "Deleting expired entries completed: total {TotalCount} entries, purged {PurgedCount} entries",
      totalCount,
      purgedCount);

    return new CacheInvalidationStatistics(totalCount, purgedCount);
  }

  private readonly INatsObjStore _cacheBucket;
  private readonly TimeProvider _timeProvider;
}
