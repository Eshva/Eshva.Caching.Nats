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

    return new CacheInvalidationStatistics(TotalEntriesCount: 0, (uint)expiredCount);
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
