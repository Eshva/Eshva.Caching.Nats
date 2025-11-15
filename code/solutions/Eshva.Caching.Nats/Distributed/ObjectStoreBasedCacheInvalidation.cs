using Eshva.Caching.Abstractions.Distributed;
using Microsoft.Extensions.Logging;
using NATS.Client.ObjectStore;

namespace Eshva.Caching.Nats.Distributed;

/// <summary>
/// Expired entries purger for NATS object store based cache.
/// </summary>
public sealed class ObjectStoreBasedCacheInvalidation : TimeBasedCacheInvalidation {
  /// <summary>
  /// Initializes a new instance of expired entries purger for NATS object store based cache.
  /// </summary>
  /// <remarks>
  /// Cache invalidation could be a resource intensive operation. It shouldn't run very often. The interval between cache
  /// invalidation runs defined using <paramref name="expiredEntriesPurgingInterval"/>. Cache invalidation could freeze (for
  /// instance I noticed it with NATS .NET client), it's wise to limit its duration. You can do it with
  /// <paramref name="maximalCacheInvalidationDuration"/>.
  /// If we'd don't limit this duration, cache invalidation can be disabled until application restart.
  /// </remarks>
  /// <param name="cacheBucket">NATS object store cache bucket.</param>
  /// <param name="expiredEntriesPurgingInterval">Expired entries purging interval.</param>
  /// <param name="maximalCacheInvalidationDuration">Maximal cache invalidation duration.</param>
  /// <param name="expiryCalculator">Cache entry expiry calculator.</param>
  /// <param name="timeProvider">Time provider.</param>
  /// <param name="logger">Logger.</param>
  /// <exception cref="ArgumentNullException">
  /// Value of a required parameter not specified.
  /// </exception>
  /// <exception cref="ArgumentNullException">
  /// Value of a required parameter isn't specified.
  /// </exception>
  /// <exception cref="ArgumentOutOfRangeException">
  /// One of:
  /// <list type="bullet">
  /// <item>Expired entries purging interval is less than minimal allowed of 1 minute.</item>
  /// <item>Maximal cache invalidation duration is greater or equals to expired entries purging interval.</item>
  /// </list>
  /// </exception>
  public ObjectStoreBasedCacheInvalidation(
    INatsObjStore cacheBucket,
    TimeSpan expiredEntriesPurgingInterval,
    TimeSpan maximalCacheInvalidationDuration,
    CacheEntryExpiryCalculator expiryCalculator,
    TimeProvider timeProvider,
    ILogger<ObjectStoreBasedCacheInvalidation>? logger = null)
    : base(
      expiredEntriesPurgingInterval,
      maximalCacheInvalidationDuration,
      expiryCalculator,
      timeProvider,
      logger) {
    _cacheBucket = cacheBucket ?? throw new ArgumentNullException(nameof(cacheBucket));
    _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
  }

  /// <inheritdoc/>
  protected override async Task<CacheInvalidationStatistics> DeleteExpiredCacheEntries(CancellationToken cancellation) {
    Logger.LogDebug("Purging expired entries started at {CurrentTime}", _timeProvider.GetUtcNow());

    var expiredEntries = await _cacheBucket.ListAsync(cancellationToken: cancellation)
      .Where(entry => ExpiryCalculator.IsCacheEntryExpired(new ObjectMetadataAccessor(entry).ExpiresAtUtc))
      .ToArrayAsync(cancellation)
      .ConfigureAwait(continueOnCapturedContext: false);

    foreach (var expiredEntry in expiredEntries) {
      var expiresAtUtc = new ObjectMetadataAccessor(expiredEntry).ExpiresAtUtc;
      Logger.LogDebug("Entry '{Key}' expires at {ExpiresAtUtc} - purge it", expiredEntry.Name, expiresAtUtc);
      await _cacheBucket.DeleteAsync(expiredEntry.Name, cancellation).ConfigureAwait(continueOnCapturedContext: false);
    }

    var expiredCount = expiredEntries.Length;
    Logger.LogDebug(
      "Purging expired entries completed at {CurrentTime}. Purged {PurgedCount} entries",
      _timeProvider.GetUtcNow(),
      expiredCount);

    return new CacheInvalidationStatistics((uint)expiredCount);
  }

  private readonly INatsObjStore _cacheBucket;
  private readonly TimeProvider _timeProvider;
}
