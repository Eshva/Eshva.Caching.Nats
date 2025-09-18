using Eshva.Caching.Abstractions;
using Microsoft.Extensions.Logging;
using NATS.Client.ObjectStore;
using NATS.Client.ObjectStore.Models;

namespace Eshva.Caching.Nats;

/// <summary>
/// Expired entries purger for NATS object-store based cache.
/// </summary>
public sealed class ObjectStoreBasedCacheExpiredEntriesPurger : StandardExpiredCacheEntriesPurger {
  /// <summary>
  /// Initializes a new instance of expired entries purger for NATS object-store based cache.
  /// </summary>
  /// <param name="cacheBucket">NATS object-store cache bucket.</param>
  /// <param name="cacheEntryExpirationStrategy">Cache entry expiration strategy.</param>
  /// <param name="purgerSettings">Purger settings.</param>
  /// <param name="timeProvider">Time provider.</param>
  /// <param name="logger">Logger.</param>
  /// <exception cref="ArgumentOutOfRangeException">
  /// <paramref name="purgerSettings"/>.ExpiredEntriesPurgingInterval value is less than 1 minute.
  /// </exception>
  public ObjectStoreBasedCacheExpiredEntriesPurger(
    INatsObjStore cacheBucket,
    ICacheEntryExpirationStrategy cacheEntryExpirationStrategy,
    PurgerSettings purgerSettings,
    TimeProvider timeProvider,
    ILogger<ObjectStoreBasedCacheExpiredEntriesPurger>? logger = null) : base(
    purgerSettings,
    TimeSpan.FromMinutes(minutes: 1),
    timeProvider,
    logger) {
    _cacheBucket = cacheBucket ?? throw new ArgumentNullException(nameof(cacheBucket));
    _cacheEntryExpirationStrategy = cacheEntryExpirationStrategy ?? throw new ArgumentNullException(nameof(cacheEntryExpirationStrategy));
  }

  /// <inheritdoc/>
  protected override async Task DeleteExpiredCacheEntries(CancellationToken token) {
    Logger.LogDebug("Deleting expired entries started");
    NotifyPurgeStarted();
    var entries = _cacheBucket.ListAsync(cancellationToken: token);

    uint totalCount = 0;
    uint purgedCount = 0;
    await foreach (var entry in entries) {
      totalCount++;
      Logger.LogDebug("Entry '{Key}' expires at {ExpiresAt}", entry.Name, EntryMetadata(entry).ExpiresAtUtc);
      if (!_cacheEntryExpirationStrategy.IsCacheEntryExpired(EntryMetadata(entry).ExpiresAtUtc)) continue;

      purgedCount++;
      await _cacheBucket.DeleteAsync(entry.Name, token);
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
  private readonly ICacheEntryExpirationStrategy _cacheEntryExpirationStrategy;
}
