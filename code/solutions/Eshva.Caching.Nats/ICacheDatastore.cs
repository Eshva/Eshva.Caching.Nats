using System.Buffers;

namespace Eshva.Caching.Nats;

#pragma warning disable VSTHRD002
#pragma warning disable VSTHRD200

/// <summary>
/// Cache datastore.
/// </summary>
public interface ICacheDatastore {
  /// <summary>
  /// Get cache entry expiry information.
  /// </summary>
  /// <param name="key">Cache entry key.</param>
  /// <param name="cancellation">Cancellation token.</param>
  /// <returns>Cache entry expiry information.</returns>
  Task<CacheEntryExpiry> GetEntryExpiry(string key, CancellationToken cancellation);

  /// <summary>
  /// Refresh a cache entry expiry information.
  /// </summary>
  /// <param name="key">Cache entry key.</param>
  /// <param name="cacheEntryExpiry"></param>
  /// <param name="cancellation">Cancellation token.</param>
  /// <returns></returns>
  Task RefreshEntry(string key, CacheEntryExpiry cacheEntryExpiry, CancellationToken cancellation);

  /// <summary>
  /// Remove a entry from cache.
  /// </summary>
  /// <param name="key">Cache entry key.</param>
  /// <param name="cancellation">Cancellation token.</param>
  /// <returns></returns>
  Task RemoveEntry(string key, CancellationToken cancellation);

  /// <summary>
  /// Try to get a cache entry.
  /// </summary>
  /// <param name="key">Cache entry key.</param>
  /// <param name="destination">Entry value destination buffer writer.</param>
  /// <param name="cancellation">Cancellation token.</param>
  /// <returns>Tuple: is entry gotten, cache entry expiry information.</returns>
  Task<(bool isEntryGotten, CacheEntryExpiry cacheEntryExpiry)> TryGetEntry(
    string key,
    IBufferWriter<byte> destination,
    CancellationToken cancellation);

  /// <summary>
  /// Set a cache entry.
  /// </summary>
  /// <param name="key">Cache entry key.</param>
  /// <param name="value">Cache entry value.</param>
  /// <param name="cacheEntryExpiry">Cache entry expiry information.</param>
  /// <param name="cancellation">Cancellation token.</param>
  /// <returns></returns>
  Task SetEntry(
    string key,
    ReadOnlySequence<byte> value,
    CacheEntryExpiry cacheEntryExpiry,
    CancellationToken cancellation);

  /// <summary>
  /// Validate cache entry key.
  /// </summary>
  /// <param name="key">Cache entry key.</param>
  /// <exception cref="ArgumentException">
  /// Cache entry key is invalid.
  /// </exception>
  void ValidateKey(string key);
}

// TODO: Document exceptions and remarks.
