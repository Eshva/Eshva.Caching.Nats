using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Eshva.Caching.Abstractions.Distributed;
using NATS.Client.Core;
using NATS.Client.KeyValueStore;

namespace Eshva.Caching.Nats.Distributed;

/// <summary>
/// NATS key-value store based cache datastore.
/// </summary>
/// <remarks>
/// NATS key-value store doesn't allow to add metadata to keys. To keep cache entry expiry metadata additional key is
/// created. In theory there could be inconsistency between value and metadata keys that can produce garbage. As a
/// solution for this problem in the future we can add a long TTL to each key, but currently it isn't implemented.
/// </remarks>
public sealed class KeyValueBasedDatastore : ICacheDatastore {
  /// <summary>
  /// Initializes a new instance of NATS key-value store based cache datastore.
  /// </summary>
  /// <param name="entriesStore">Entries key-value store.</param>
  /// <param name="expirySerializer">Cache entry expiry serializer.</param>
  /// <param name="expiryCalculator">Cache entry expiry calculator.</param>
  /// <exception cref="ArgumentNullException">
  /// Value of a required parameter not specified.
  /// </exception>
  public KeyValueBasedDatastore(
    INatsKVStore entriesStore,
    INatsSerializer<CacheEntryExpiry> expirySerializer,
    CacheEntryExpiryCalculator expiryCalculator) {
    _entriesStore = entriesStore ?? throw new ArgumentNullException(nameof(entriesStore));
    _expirySerializer = expirySerializer ?? throw new ArgumentNullException(nameof(expirySerializer));
    _expiryCalculator = expiryCalculator ?? throw new ArgumentNullException(nameof(expiryCalculator));
  }

  /// <inheritdoc/>
  public async Task<CacheEntryExpiry> GetEntryExpiry(string key, CancellationToken cancellation) {
    var metadataStatus = await _entriesStore
      .TryGetEntryAsync(MakeMetadataKey(key), serializer: _expirySerializer, cancellationToken: cancellation)
      .ConfigureAwait(continueOnCapturedContext: false);
    return metadataStatus.Success
      ? metadataStatus.Value.Value
      : throw new InvalidOperationException($"An entry with key '{key}' could not be found in the cache.");
  }

  /// <inheritdoc/>
  public async Task RefreshEntry(string key, CacheEntryExpiry cacheEntryExpiry, CancellationToken cancellation) {
    var metadataStatus = await _entriesStore
      .TryGetEntryAsync(MakeMetadataKey(key), serializer: _expirySerializer, cancellationToken: cancellation)
      .ConfigureAwait(continueOnCapturedContext: false);
    if (!metadataStatus.Success) {
      throw new InvalidOperationException($"An entry with key '{key}' could not be found in the cache.");
    }

    var metadata = metadataStatus.Value.Value with {
      ExpiresAtUtc = _expiryCalculator.CalculateExpiration(
        metadataStatus.Value.Value.AbsoluteExpiryAtUtc,
        metadataStatus.Value.Value.SlidingExpiryInterval)
    };

    var valueStatus = await _entriesStore.TryUpdateAsync(
        MakeMetadataKey(key),
        metadata,
        metadataStatus.Value.Revision,
        _expirySerializer,
        cancellation)
      .ConfigureAwait(continueOnCapturedContext: false);
    if (!valueStatus.Success) {
      throw new InvalidOperationException($"An entry with key '{key}' could not be found in the cache.");
    }
  }

  /// <inheritdoc/>
  public async Task RemoveEntry(string key, CancellationToken cancellation) {
    var metadataStatus = await _entriesStore.TryPurgeAsync(MakeMetadataKey(key), cancellationToken: cancellation)
      .ConfigureAwait(continueOnCapturedContext: false);
    var valueStatus = await _entriesStore.TryPurgeAsync(key, cancellationToken: cancellation)
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
    var metadataStatus = await _entriesStore
      .TryGetEntryAsync(MakeMetadataKey(key), serializer: _expirySerializer, cancellationToken: cancellation)
      .ConfigureAwait(continueOnCapturedContext: false);
    var valueStatus = await _entriesStore.TryGetEntryAsync<byte[]>(key, cancellationToken: cancellation)
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
    var valueStatus = await _entriesStore.TryPutAsync(key, value, cancellationToken: cancellation)
      .ConfigureAwait(continueOnCapturedContext: false);
    var metadataStatus = await _entriesStore.TryPutAsync(
        MakeMetadataKey(key),
        cacheEntryExpiry,
        _expirySerializer,
        cancellation)
      .ConfigureAwait(continueOnCapturedContext: false);
    if (!valueStatus.Success || !metadataStatus.Success) {
      throw new InvalidOperationException($"Failed to put cache entry with key '{key}'.");
    }
  }

  /// <inheritdoc/>
  public void ValidateKey(string key) {
    if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Cache entry key can't be null or whitespace.", nameof(key));
    if (key[index: 0] == '.' || key[^1] == '.') throw new ArgumentException("Key cannot start or end with a period", nameof(key));
    if (!ValidKeyRegex.IsMatch(key)) throw new ArgumentException("Key contains invalid characters", nameof(key));
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private static string MakeMetadataKey(string key) => $"{key}-metadata";

  private readonly INatsKVStore _entriesStore;
  private readonly INatsSerializer<CacheEntryExpiry> _expirySerializer;
  private readonly CacheEntryExpiryCalculator _expiryCalculator;
  private static readonly Regex ValidKeyRegex = new(@"\A[-/_=\.a-zA-Z0-9]+\z", RegexOptions.Compiled);
}
