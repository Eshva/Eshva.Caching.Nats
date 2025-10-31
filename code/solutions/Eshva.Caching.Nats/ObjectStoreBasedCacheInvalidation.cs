using Eshva.Caching.Abstractions;
using Microsoft.Extensions.Logging;
using NATS.Client.ObjectStore;
using NATS.Client.ObjectStore.Models;

namespace Eshva.Caching.Nats;

/// <summary>
/// Expired entries purger for NATS object-store based cache.
/// </summary>
public sealed class ObjectStoreBasedCacheInvalidation : TimeBasedCacheInvalidation {
  /// <summary>
  /// Initializes a new instance of expired entries purger for NATS object-store based cache.
  /// </summary>
  /// <param name="cacheBucket">NATS object-store cache bucket.</param>
  /// <param name="settings">Time-based cache invalidation settings.</param>
  /// <param name="timeProvider">Time provider.</param>
  /// <param name="logger">Logger.</param>
  /// <exception cref="ArgumentOutOfRangeException">
  /// <paramref name="settings"/>.ExpiredEntriesPurgingInterval value is less than 1 minute.
  /// </exception>
  public ObjectStoreBasedCacheInvalidation(
    INatsObjStore cacheBucket,
    TimeBasedCacheInvalidationSettings settings,
    TimeProvider timeProvider,
    ILogger<ObjectStoreBasedCacheInvalidation>? logger = null) : base(
    settings,
    TimeSpan.FromMinutes(value: 1D),
    timeProvider,
    logger) {
    _cacheBucket = cacheBucket ?? throw new ArgumentNullException(nameof(cacheBucket));
    _timeProvider = timeProvider;
  }

  /// <inheritdoc/>
  protected override async Task<CacheInvalidationStatistics> DeleteExpiredCacheEntries(CancellationToken cancellation) {
    Logger.LogDebug("Deleting expired entries started at {CurrentTime}", _timeProvider.GetUtcNow());

    uint totalCount = 0;
    uint purgedCount = 0;
    var entries = _cacheBucket.ListAsync(cancellationToken: cancellation).ConfigureAwait(continueOnCapturedContext: false);
    await foreach (var entry in entries) {
      totalCount++;
      Logger.LogDebug("Entry '{Key}' expires at {ExpiresAt}", entry.Name, EntryMetadata(entry).ExpiresAtUtc);
      if (!ExpiryCalculator.IsCacheEntryExpired(EntryMetadata(entry).ExpiresAtUtc)) continue;

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

  private static CacheEntryMetadata EntryMetadata(ObjectMetadata objectMetadata) => objectMetadata.Metadata is not null
    ? new CacheEntryMetadata(objectMetadata.Metadata)
    : throw new InvalidOperationException($"A cache entry '{objectMetadata.Name}' has no metadata.");

  private readonly INatsObjStore _cacheBucket;
  private readonly TimeProvider _timeProvider;
}
