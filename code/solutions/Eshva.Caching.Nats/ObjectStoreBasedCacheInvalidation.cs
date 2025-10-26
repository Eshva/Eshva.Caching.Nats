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
    TimeSpan.FromMinutes(minutes: 1),
    timeProvider,
    logger) {
    _cacheBucket = cacheBucket ?? throw new ArgumentNullException(nameof(cacheBucket));
  }

  /// <inheritdoc/>
  protected override async Task DeleteExpiredCacheEntries(CancellationToken token) {
    Logger.LogDebug("Deleting expired entries started");
    NotifyPurgeStarted();
    var entries = _cacheBucket.ListAsync(cancellationToken: token);

    uint totalCount = 0;
    uint purgedCount = 0;
    await foreach (var entry in entries.ConfigureAwait(continueOnCapturedContext: false)) {
      totalCount++;
      Logger.LogDebug("Entry '{Key}' expires at {ExpiresAt}", entry.Name, EntryMetadata(entry).ExpiresAtUtc);
      if (!IsCacheEntryExpired(EntryMetadata(entry).ExpiresAtUtc)) continue;

      purgedCount++;
      await _cacheBucket.DeleteAsync(entry.Name, token).ConfigureAwait(continueOnCapturedContext: false);
      Logger.LogDebug("Deleted expired entry '{Key}'", entry.Name);
    }

    Logger.LogDebug(
      "Deleting expired entries completed: total {TotalCount} entries, purged {PurgedCount} entries",
      totalCount,
      purgedCount);
    NotifyPurgeCompleted(totalCount, purgedCount);
  }

  private static CacheEntryMetadata EntryMetadata(ObjectMetadata objectMetadata) => objectMetadata.Metadata is not null
    ? new CacheEntryMetadata(objectMetadata.Metadata)
    : throw new InvalidOperationException($"A cache entry '{objectMetadata.Name}' has no metadata.");

  private readonly INatsObjStore _cacheBucket;
}
