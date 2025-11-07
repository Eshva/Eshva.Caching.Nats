using Eshva.Caching.Abstractions;
using Microsoft.Extensions.Logging;
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
  /// <param name="entryValuesStore">Values key-value store.</param>
  /// <param name="entryMetadataStore">Metadata key-value store.</param>
  /// <param name="expiredEntriesPurgingInterval">Expired entries purging interval.</param>
  /// <param name="desynchronizedEntriesPurgingFactor">Desynchronized entries purging factor.</param>
  /// <param name="expiryCalculator">Cache entry expiry calculator.</param>
  /// <param name="timeProvider">Time provider.</param>
  /// <param name="logger">Logger.</param>
  /// <exception cref="ArgumentNullException">
  /// Value of a required parameter not specified.
  /// </exception>
  public KeyValueBasedCacheInvalidation(
    INatsKVStore entryValuesStore,
    INatsKVStore entryMetadataStore,
    TimeSpan expiredEntriesPurgingInterval,
    uint desynchronizedEntriesPurgingFactor,
    CacheEntryExpiryCalculator expiryCalculator,
    TimeProvider timeProvider,
    ILogger? logger = null)
    : base(
      expiredEntriesPurgingInterval,
      expiryCalculator,
      timeProvider,
      logger) {
    _entryValuesStore = entryValuesStore ?? throw new ArgumentNullException(nameof(entryValuesStore));
    _entryMetadataStore = entryMetadataStore ?? throw new ArgumentNullException(nameof(entryMetadataStore));
    _desynchronizedEntriesPurgingFactor = desynchronizedEntriesPurgingFactor;
    _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
  }

  /// <inheritdoc/>
  protected override async Task<CacheInvalidationStatistics> DeleteExpiredCacheEntries(CancellationToken cancellation) {
    Logger.LogDebug("Deleting expired entries started at {CurrentTime}", _timeProvider.GetUtcNow());

    if (IsDesynchronizedEntriesPurgingRequired()) {
      await PurgeDesynchronizedEntries(cancellation).ConfigureAwait(continueOnCapturedContext: false);
    }

    uint totalCount = 0;
    uint purgedCount = 0;
    await foreach (var key in _entryMetadataStore.GetKeysAsync(cancellationToken: cancellation)
                     .ConfigureAwait(continueOnCapturedContext: false)) {
      totalCount++;

      var entryMetadata = await _entryMetadataStore.GetEntryAsync<CacheEntryExpiry>(key, cancellationToken: cancellation)
        .ConfigureAwait(continueOnCapturedContext: false);
      Logger.LogDebug("Entry '{Key}' expires at {ExpiresAtUtc}", key, entryMetadata.Value.ExpiresAtUtc);

      if (!ExpiryCalculator.IsCacheEntryExpired(entryMetadata.Value.ExpiresAtUtc)) continue;

      var valuePurgeStatus = await _entryValuesStore.TryPurgeAsync(key, cancellationToken: cancellation)
        .ConfigureAwait(continueOnCapturedContext: false);
      var metadataPurgeStatus = await _entryMetadataStore.TryPurgeAsync(key, cancellationToken: cancellation)
        .ConfigureAwait(continueOnCapturedContext: false);
      if (valuePurgeStatus.Success && metadataPurgeStatus.Success) {
        purgedCount++;
        Logger.LogDebug("Deleted expired entry '{Key}'", key);
      }
      else {
        Logger.LogError(valuePurgeStatus.Error, "Can't deleted expired entry '{Key}'", key);
      }
    }

    Logger.LogDebug(
      "Deleting expired entries completed: total {TotalCount} entries, purged {PurgedCount} entries",
      totalCount,
      purgedCount);

    _invalidationCount++;

    return new CacheInvalidationStatistics(totalCount, purgedCount);
  }

  private bool IsDesynchronizedEntriesPurgingRequired() =>
    _invalidationCount % _desynchronizedEntriesPurgingFactor == 0;

  private async Task PurgeDesynchronizedEntries(CancellationToken cancellation) {
    var (desynchronizedValueKeys, desynchronizedMetadataKeys) =
      await GetDesynchronizedKeys(cancellation).ConfigureAwait(continueOnCapturedContext: false);
    foreach (var key in desynchronizedValueKeys) {
      await _entryValuesStore.PurgeAsync(key, cancellationToken: cancellation).ConfigureAwait(continueOnCapturedContext: false);
    }

    foreach (var key in desynchronizedMetadataKeys) {
      await _entryMetadataStore.PurgeAsync(key, cancellationToken: cancellation).ConfigureAwait(continueOnCapturedContext: false);
    }
  }

  private async Task<(IReadOnlyList<string> desynchronizedValueKeys, IReadOnlyList<string> desynchronizedMetadataKeys)>
    GetDesynchronizedKeys(CancellationToken cancellation) {
    var entryValueKeys = await _entryValuesStore.GetKeysAsync(cancellationToken: cancellation)
      .ToListAsync(cancellation)
      .ConfigureAwait(continueOnCapturedContext: false);
    var entryMetadataKeys = await _entryMetadataStore.GetKeysAsync(cancellationToken: cancellation)
      .ToListAsync(cancellation)
      .ConfigureAwait(continueOnCapturedContext: false);
    return (desynchronizedValueKeys: entryValueKeys.Except(entryMetadataKeys).ToArray(),
      desynchronizedMetadataKeys: entryMetadataKeys.Except(entryValueKeys).ToArray());
  }

  private readonly INatsKVStore _entryValuesStore;
  private readonly TimeProvider _timeProvider;
  private readonly INatsKVStore _entryMetadataStore;
  private uint _invalidationCount;
  private readonly uint _desynchronizedEntriesPurgingFactor;
}
