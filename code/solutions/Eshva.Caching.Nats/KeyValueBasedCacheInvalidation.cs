using System.Runtime.CompilerServices;
using Eshva.Caching.Abstractions;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using NATS.Client.KeyValueStore;

namespace Eshva.Caching.Nats;

#pragma warning disable VSTHRD200

/// <summary>
/// Expired entries purger for NATS key-value store based cache.
/// </summary>
public sealed class KeyValueBasedCacheInvalidation : TimeBasedCacheInvalidation {
  /// <summary>
  /// Initializes a new instance of expired entries purger for NATS key-value store based cache.
  /// </summary>
  /// <param name="entriesStore">Entries key-value store.</param>
  /// <param name="expiredEntriesPurgingInterval">Expired entries purging interval.</param>
  /// <param name="expirySerializer">Cache entry expiry serializer.</param>
  /// <param name="expiryCalculator">Cache entry expiry calculator.</param>
  /// <param name="timeProvider">Time provider.</param>
  /// <param name="logger">Logger.</param>
  /// <exception cref="ArgumentNullException">
  /// Value of a required parameter not specified.
  /// </exception>
  public KeyValueBasedCacheInvalidation(
    INatsKVStore entriesStore,
    TimeSpan expiredEntriesPurgingInterval,
    INatsSerializer<CacheEntryExpiry> expirySerializer,
    CacheEntryExpiryCalculator expiryCalculator,
    TimeProvider timeProvider,
    ILogger? logger = null)
    : base(
      expiredEntriesPurgingInterval,
      expiryCalculator,
      timeProvider,
      logger) {
    _entriesStore = entriesStore ?? throw new ArgumentNullException(nameof(entriesStore));
    _expirySerializer = expirySerializer ?? throw new ArgumentNullException(nameof(expirySerializer));
    _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
  }

  /// <inheritdoc/>
  protected override async Task<CacheInvalidationStatistics> DeleteExpiredCacheEntries(CancellationToken cancellation) {
    Logger.LogDebug("Deleting expired entries started at {CurrentTime}", _timeProvider.GetUtcNow());

    uint totalCount = 0;
    uint purgedCount = 0;
    await foreach (var key in _entriesStore.GetKeysAsync(cancellationToken: cancellation)
                     .ConfigureAwait(continueOnCapturedContext: false)) {
      if (IsMetadataKey(key)) continue;

      totalCount++;

      var metadataKey = MakeMetadataKey(key);
      var entryMetadata = await _entriesStore
        .GetEntryAsync(metadataKey, serializer: _expirySerializer, cancellationToken: cancellation)
        .ConfigureAwait(continueOnCapturedContext: false);
      var expiresAtUtc = entryMetadata.Value.ExpiresAtUtc;

      if (!ExpiryCalculator.IsCacheEntryExpired(expiresAtUtc)) {
        Logger.LogDebug("Entry '{Key}' expires at {ExpiresAtUtc} - keep it", key, expiresAtUtc);
        continue;
      }

      Logger.LogDebug("Entry '{Key}' expires at {ExpiresAtUtc} - purge it", key, expiresAtUtc);
      var valuePurgeStatus = await _entriesStore.TryPurgeAsync(key, cancellationToken: cancellation)
        .ConfigureAwait(continueOnCapturedContext: false);
      var metadataPurgeStatus = await _entriesStore.TryPurgeAsync(metadataKey, cancellationToken: cancellation)
        .ConfigureAwait(continueOnCapturedContext: false);
      if (valuePurgeStatus.Success && metadataPurgeStatus.Success) {
        purgedCount++;
      }
      else {
        Logger.LogError(valuePurgeStatus.Error, "Can't deleted expired entry '{Key}'", key);
      }
    }

    Logger.LogDebug(
      "Deleting expired entries completed: total {TotalCount} entries scanned, purged {PurgedCount} entries",
      totalCount,
      purgedCount);

    return new CacheInvalidationStatistics(totalCount, purgedCount);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private static bool IsMetadataKey(string key) => key.EndsWith(MetadataSuffix);

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private static string MakeMetadataKey(string key) => $"{key}{MetadataSuffix}";

  private readonly INatsKVStore _entriesStore;
  private readonly TimeProvider _timeProvider;
  private readonly INatsSerializer<CacheEntryExpiry> _expirySerializer;
  private const string MetadataSuffix = "-metadata";
}
