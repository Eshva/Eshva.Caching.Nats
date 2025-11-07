using System.Buffers;
using Eshva.Caching.Abstractions;
using NATS.Client.KeyValueStore;

namespace Eshva.Caching.Nats;

/// <summary>
/// NATS key-value store based cache datastore.
/// </summary>
public sealed class KeyValueBasedDatastore : ICacheDatastore {
  /// <summary>
  /// Initializes a new instance of NATS key-value store based cache datastore.
  /// </summary>
  /// <param name="entryValuesStore">Values key-value store.</param>
  /// <param name="entryMetadataStore">Metadata key-value store.</param>
  /// <param name="expiryCalculator">Cache entry expiry calculator.</param>
  /// <exception cref="ArgumentNullException">
  /// Value of a required parameter not specified.
  /// </exception>
  public KeyValueBasedDatastore(
    INatsKVStore entryValuesStore,
    INatsKVStore entryMetadataStore,
    CacheEntryExpiryCalculator expiryCalculator) {
    _entryValuesStore = entryValuesStore ?? throw new ArgumentNullException(nameof(entryValuesStore));
    _entryMetadataStore = entryMetadataStore ?? throw new ArgumentNullException(nameof(entryMetadataStore));
    _expiryCalculator = expiryCalculator ?? throw new ArgumentNullException(nameof(expiryCalculator));
  }

  /// <inheritdoc/>
  public async Task<CacheEntryExpiry> GetEntryExpiry(string key, CancellationToken cancellation) {
    var metadataStatus = await _entryMetadataStore.TryGetEntryAsync<CacheEntryExpiry>(key, cancellationToken: cancellation)
      .ConfigureAwait(continueOnCapturedContext: false);
    return metadataStatus.Success
      ? metadataStatus.Value.Value
      : throw new InvalidOperationException($"An entry with key '{key}' could not be found in the cache.");
  }

  /// <inheritdoc/>
  public async Task RefreshEntry(string key, CacheEntryExpiry cacheEntryExpiry, CancellationToken cancellation) {
    var metadataStatus = await _entryMetadataStore.TryGetEntryAsync<CacheEntryExpiry>(key, cancellationToken: cancellation)
      .ConfigureAwait(continueOnCapturedContext: false);
    if (!metadataStatus.Success) {
      throw new InvalidOperationException($"An entry with key '{key}' could not be found in the cache.");
    }

    var metadata = metadataStatus.Value.Value with {
      ExpiresAtUtc = _expiryCalculator.CalculateExpiration(
        metadataStatus.Value.Value.AbsoluteExpiryAtUtc,
        metadataStatus.Value.Value.SlidingExpiryInterval)
    };

    var valueStatus = await _entryMetadataStore.TryUpdateAsync(
        key,
        metadata,
        metadataStatus.Value.Revision,
        cancellationToken: cancellation)
      .ConfigureAwait(continueOnCapturedContext: false);
    if (!valueStatus.Success) {
      throw new InvalidOperationException($"An entry with key '{key}' could not be found in the cache.");
    }
  }

  /// <inheritdoc/>
  public async Task RemoveEntry(string key, CancellationToken cancellation) {
    var metadataStatus = await _entryMetadataStore.TryPurgeAsync(key, cancellationToken: cancellation)
      .ConfigureAwait(continueOnCapturedContext: false);
    var valueStatus = await _entryValuesStore.TryPurgeAsync(key, cancellationToken: cancellation)
      .ConfigureAwait(continueOnCapturedContext: false);
    if (!valueStatus.Success || !metadataStatus.Success) {
      throw new InvalidOperationException($"An entry with key '{key}' could not be found in the cache.");
    }
  }

  /// <inheritdoc/>
  public async Task<(bool isEntryGotten, CacheEntryExpiry cacheEntryExpiry)> TryGetEntry(
    string key,
    IBufferWriter<byte> destination,
    CancellationToken cancellation) {
    var metadataStatus = await _entryMetadataStore.TryGetEntryAsync<CacheEntryExpiry>(key, cancellationToken: cancellation)
      .ConfigureAwait(continueOnCapturedContext: false);
    var valueStatus = await _entryMetadataStore.TryGetEntryAsync<byte[]>(key, cancellationToken: cancellation)
      .ConfigureAwait(continueOnCapturedContext: false);
    destination.Write(valueStatus.Value.Value);
    return metadataStatus.Success && valueStatus.Success
      ? (true, metadataStatus.Value.Value)
      : (false, new CacheEntryExpiry(DateTimeOffset.MinValue, AbsoluteExpiryAtUtc: null, SlidingExpiryInterval: null));
  }

  /// <inheritdoc/>
  public async Task SetEntry(
    string key,
    ReadOnlySequence<byte> value,
    CacheEntryExpiry cacheEntryExpiry,
    CancellationToken cancellation) {
    var valueStatus = await _entryValuesStore.TryPutAsync(key, value, cancellationToken: cancellation)
      .ConfigureAwait(continueOnCapturedContext: false);
    var metadataStatus = await _entryMetadataStore.TryPutAsync(key, cacheEntryExpiry, cancellationToken: cancellation)
      .ConfigureAwait(continueOnCapturedContext: false);
    if (!valueStatus.Success || !metadataStatus.Success) {
      throw new InvalidOperationException($"Failed to put cache entry with key '{key}'.");
    }
  }

  /// <inheritdoc/>
  public void ValidateKey(string key) {
    if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Cache entry key can't be null or whitespace.", nameof(key));
  }

  private readonly INatsKVStore _entryValuesStore;
  private readonly INatsKVStore _entryMetadataStore;
  private readonly CacheEntryExpiryCalculator _expiryCalculator;
}
