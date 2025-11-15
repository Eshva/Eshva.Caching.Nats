using System.Runtime.CompilerServices;
using Eshva.Caching.Abstractions.Distributed;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using NATS.Client.KeyValueStore;

namespace Eshva.Caching.Nats.Distributed;

#pragma warning disable VSTHRD200

/// <summary>
/// Expired entries purger for NATS key-value store based cache.
/// </summary>
public sealed class KeyValueBasedCacheInvalidation : TimeBasedCacheInvalidation {
  /// <summary>
  /// Initializes a new instance of expired entries purger for NATS key-value store based cache.
  /// </summary>
  /// <remarks>
  /// Cache invalidation could be a resource intensive operation. It shouldn't run very often. The interval between cache
  /// invalidation runs defined using <paramref name="expiredEntriesPurgingInterval"/>. Cache invalidation could freeze (for
  /// instance I noticed it with NATS .NET client), it's wise to limit its duration. You can do it with
  /// <paramref name="maximalCacheInvalidationDuration"/>.
  /// If we'd don't limit this duration, cache invalidation can be disabled until application restart.
  /// </remarks>
  /// <param name="entriesStore">Entries key-value store.</param>
  /// <param name="expiredEntriesPurgingInterval">Expired entries purging interval.</param>
  /// <param name="maximalCacheInvalidationDuration">Maximal cache invalidation duration.</param>
  /// <param name="expirySerializer">Cache entry expiry serializer.</param>
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
  public KeyValueBasedCacheInvalidation(
    INatsKVStore entriesStore,
    TimeSpan expiredEntriesPurgingInterval,
    TimeSpan maximalCacheInvalidationDuration,
    INatsSerializer<CacheEntryExpiry> expirySerializer,
    CacheEntryExpiryCalculator expiryCalculator,
    TimeProvider timeProvider,
    ILogger? logger = null)
    : base(
      expiredEntriesPurgingInterval,
      maximalCacheInvalidationDuration,
      expiryCalculator,
      timeProvider,
      logger) {
    _entriesStore = entriesStore ?? throw new ArgumentNullException(nameof(entriesStore));
    _expirySerializer = expirySerializer ?? throw new ArgumentNullException(nameof(expirySerializer));
    _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
  }

  /// <inheritdoc/>
  protected override async Task<CacheInvalidationStatistics> DeleteExpiredCacheEntries(CancellationToken cancellation) {
    Logger.LogDebug("Purging expired entries started at {CurrentTime}", _timeProvider.GetUtcNow());

    var expiredEntries = await _entriesStore.GetKeysAsync(cancellationToken: cancellation)
      .Where(IsMetadataKey)
      .SelectAwaitWithCancellation(async (key, token) =>
        new EntryExpiry(key, await GetEntryExpiry(token, key).ConfigureAwait(continueOnCapturedContext: false)))
      .Where(entryExpiry => ExpiryCalculator.IsCacheEntryExpired(entryExpiry.Expiry.ExpiresAtUtc))
      .ToArrayAsync(cancellation)
      .ConfigureAwait(continueOnCapturedContext: false);

    foreach (var expiredEntry in expiredEntries) {
      var expiresAtUtc = expiredEntry.Expiry.ExpiresAtUtc;

      var valueKey = expiredEntry.Key[..^MetadataSuffix.Length];
      Logger.LogDebug("Entry '{Key}' expires at {ExpiresAtUtc} - purge it", valueKey, expiresAtUtc);

      var valuePurgeStatus = await _entriesStore.TryPurgeAsync(valueKey, cancellationToken: cancellation)
        .ConfigureAwait(continueOnCapturedContext: false);
      var metadataPurgeStatus = await _entriesStore.TryPurgeAsync(expiredEntry.Key, cancellationToken: cancellation)
        .ConfigureAwait(continueOnCapturedContext: false);

      if (!valuePurgeStatus.Success && metadataPurgeStatus.Success) {
        Logger.LogError(valuePurgeStatus.Error, "Can't purge expired entry '{Key}'", valueKey);
      }
    }

    var expiredCount = expiredEntries.Length;
    Logger.LogDebug(
      "Purging expired entries completed at {CurrentTime}. Purged {PurgedCount} entries",
      _timeProvider.GetUtcNow(),
      expiredCount);

    return new CacheInvalidationStatistics((uint)expiredCount);
  }

  private async Task<CacheEntryExpiry> GetEntryExpiry(CancellationToken cancellation, string key) =>
    (await _entriesStore
      .GetEntryAsync(key, serializer: _expirySerializer, cancellationToken: cancellation)
      .ConfigureAwait(continueOnCapturedContext: false)).Value;

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private static bool IsMetadataKey(string key) => key.EndsWith(MetadataSuffix);

  private readonly INatsKVStore _entriesStore;
  private readonly TimeProvider _timeProvider;
  private readonly INatsSerializer<CacheEntryExpiry> _expirySerializer;
  private const string MetadataSuffix = "-metadata";

  private readonly record struct EntryExpiry(string Key, CacheEntryExpiry Expiry);
}
