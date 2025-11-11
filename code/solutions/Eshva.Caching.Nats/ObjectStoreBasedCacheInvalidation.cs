using Eshva.Caching.Abstractions;
using Microsoft.Extensions.Logging;
using NATS.Client.ObjectStore;

namespace Eshva.Caching.Nats;

/// <summary>
/// Expired entries purger for NATS object store based cache.
/// </summary>
public sealed class ObjectStoreBasedCacheInvalidation : TimeBasedCacheInvalidation {
  /// <summary>
  /// Initializes a new instance of expired entries purger for NATS object store based cache.
  /// </summary>
  /// <param name="cacheBucket">NATS object store cache bucket.</param>
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

    return new CacheInvalidationStatistics(TotalEntriesCount: 0, (uint)expiredCount);
  }

  private readonly INatsObjStore _cacheBucket;
  private readonly TimeProvider _timeProvider;
}
