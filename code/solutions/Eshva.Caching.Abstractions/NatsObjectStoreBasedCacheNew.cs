using System.Buffers;
using JetBrains.Annotations;
using Microsoft.Extensions.Caching.Distributed;

namespace Eshva.Caching.Abstractions;

/// <summary>
/// NATS object-store based distributed cache.
/// </summary>
[PublicAPI]
public abstract class BufferDistributedCache : IBufferDistributedCache {
  /// <summary>
  /// Initializes a new instance of a NATS object-store based distributed cache.
  /// </summary>
  /// <param name="dataAccessStrategy">Data access strategy used by this cache.</param>
  /// <exception cref="ArgumentNullException">
  /// One of required arguments isn't specified.
  /// </exception>
  protected BufferDistributedCache(IBufferDistributedCacheDataAccessStrategy dataAccessStrategy) {
    _dataAccessStrategy = dataAccessStrategy ?? throw new ArgumentNullException(nameof(dataAccessStrategy));
  }

  /// <summary>
  /// Get value of a cache key <paramref name="key"/>.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Read the key <paramref name="key"/> value from the object-store bucket and returns it as a byte array if it's
  /// found. If entry has a sliding expiration its expiration time could be refreshed (depends on the purger used).
  /// </para>
  /// <para>
  /// If it's time purge all expired entries in the cache.
  /// </para>
  /// </remarks>
  /// <param name="key">Cache entry key.</param>
  /// <returns>
  /// Depending on different circumstances returns:
  /// <list type="bullet">
  /// <item>byte array - read key value,</item>
  /// <item>null - cache key <paramref name="key"/> isn't found.</item>
  /// </list>
  /// </returns>
  /// <exception cref="ArgumentException">
  /// The key is not specified.
  /// </exception>
  /// <exception cref="InvalidOperationException">
  /// One of:
  /// <list type="bullet">
  /// <item>Failed to read cache key value.</item>
  /// <item>Length of the read value less than length of the cache value.</item>
  /// </list>
  /// </exception>
  public byte[]? Get(string key) => _dataAccessStrategy.Get(key);

  /// <summary>
  /// Get value of a cache key <paramref name="key"/>.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Read the key <paramref name="key"/> value from the object-store bucket and returns it as a byte array if it's
  /// found. If entry has a sliding expiration its expiration time could be refreshed (depends on the purger used).
  /// </para>
  /// <para>
  /// If it's time purge all expired entries in the cache.
  /// </para>
  /// </remarks>
  /// <param name="key">Cache entry key.</param>
  /// <param name="token">Cancellation token.</param>
  /// <returns>
  /// Depending on different circumstances returns:
  /// <list type="bullet">
  /// <item>byte array - read key value,</item>
  /// <item>null - cache key <paramref name="key"/> isn't found.</item>
  /// </list>
  /// </returns>
  /// <exception cref="ArgumentException">
  /// The key is not specified.
  /// </exception>
  /// <exception cref="InvalidOperationException">
  /// One of:
  /// <list type="bullet">
  /// <item>Failed to read cache key value.</item>
  /// <item>Length of the read value less than length of the cache value.</item>
  /// </list>
  /// </exception>
  public Task<byte[]?> GetAsync(string key, CancellationToken token = default) => _dataAccessStrategy.GetAsync(key, token);

  /// <summary>
  /// Set a cache entry value.
  /// </summary>
  /// <param name="key">The key of cache entry.</param>
  /// <param name="value">The value of cache entry.</param>
  /// <param name="options">Expiration options.</param>
  /// <exception cref="ArgumentNullException">
  /// The key is not specified.
  /// </exception>
  public void Set(string key, byte[] value, DistributedCacheEntryOptions options) =>
    _dataAccessStrategy.Set(key, value, options);

  /// <summary>
  /// Set a cache entry value.
  /// </summary>
  /// <param name="key">The key of cache entry.</param>
  /// <param name="value">The value of cache entry.</param>
  /// <param name="options">Expiration options.</param>
  /// <param name="token">Cancellation token.</param>
  /// <exception cref="ArgumentNullException">
  /// The key is not specified.
  /// </exception>
  public Task SetAsync(
    string key,
    byte[] value,
    DistributedCacheEntryOptions options,
    CancellationToken token = default) =>
    _dataAccessStrategy.SetAsync(
      key,
      value,
      options,
      token);

  /// <summary>
  /// Refresh expiration time of the cache entry with <paramref name="key"/>.
  /// </summary>
  /// <param name="key">The key of refreshing cache entry.</param>
  /// <exception cref="ArgumentNullException">
  /// The key is not specified.
  /// </exception>
  /// <exception cref="InvalidOperationException">
  /// Cache entry with <paramref name="key"/> not found.
  /// </exception>
  public void Refresh(string key) => _dataAccessStrategy.Refresh(key);

  /// <summary>
  /// Refresh expiration time of the cache entry with <paramref name="key"/>.
  /// </summary>
  /// <param name="key">The key of refreshing cache entry.</param>
  /// <param name="token">Cancellation token.</param>
  /// <exception cref="ArgumentNullException">
  /// The key is not specified.
  /// </exception>
  /// <exception cref="InvalidOperationException">
  /// Cache entry with <paramref name="key"/> not found.
  /// </exception>
  public Task RefreshAsync(string key, CancellationToken token = default) => _dataAccessStrategy.RefreshAsync(key, token);

  /// <summary>
  /// Remove a cache entry.
  /// </summary>
  /// <remarks>
  /// If cache entry doesn't exist or removed no exception will be thrown.
  /// </remarks>
  /// <param name="key">The key of removing cache entry.</param>
  /// <exception cref="ArgumentNullException">
  /// The key is not specified.
  /// </exception>
  /// <exception cref="InvalidOperationException">
  /// Cache entry metadata are corrupted.
  /// </exception>
  public void Remove(string key) => RemoveAsync(key).GetAwaiter().GetResult();

  /// <summary>
  /// Remove a cache entry.
  /// </summary>
  /// <remarks>
  /// If cache entry doesn't exist or removed no exception will be thrown.
  /// </remarks>
  /// <param name="key">The key of removing cache entry.</param>
  /// <param name="token">Cancellation token.</param>
  /// <exception cref="ArgumentNullException">
  /// The key is not specified.
  /// </exception>
  /// <exception cref="InvalidOperationException">
  /// Cache entry metadata are corrupted.
  /// </exception>
  public Task RemoveAsync(string key, CancellationToken token = default) => _dataAccessStrategy.RemoveAsync(key, token);

  /// <summary>
  /// Try to get a cache entry.
  /// </summary>
  /// <param name="key">Cache entry key.</param>
  /// <param name="destination">Buffer writer to write cache entry value into.</param>
  /// <exception cref="ArgumentNullException">
  /// The key is not specified.
  /// </exception>
  /// <returns>
  /// <c>true</c> - value successfully read, <c>false</c> - entry not found in the cache.
  /// </returns>
  /// <exception cref="ArgumentNullException">
  /// The key is not specified.
  /// </exception>
  /// <exception cref="InvalidOperationException">
  /// Failed to read cache key value.
  /// </exception>
  public bool TryGet(string key, IBufferWriter<byte> destination) =>
    _dataAccessStrategy.TryGet(key, destination);

  /// <summary>
  /// Try to get a cache entry.
  /// </summary>
  /// <param name="key">Cache entry key.</param>
  /// <param name="destination">Buffer writer to write cache entry value into.</param>
  /// <param name="token">Cancellation token.</param>
  /// <exception cref="ArgumentNullException">
  /// The key is not specified.
  /// </exception>
  /// <returns>
  /// <c>true</c> - value successfully read, <c>false</c> - entry not found in the cache.
  /// </returns>
  /// <exception cref="ArgumentNullException">
  /// The key is not specified.
  /// </exception>
  /// <exception cref="InvalidOperationException">
  /// Failed to read cache key value.
  /// </exception>
  public ValueTask<bool> TryGetAsync(string key, IBufferWriter<byte> destination, CancellationToken token = new()) =>
    _dataAccessStrategy.TryGetAsync(key, destination, token);

  public void Set(string key, ReadOnlySequence<byte> value, DistributedCacheEntryOptions options) =>
    _dataAccessStrategy.Set(key, value, options);

  public ValueTask SetAsync(
    string key,
    ReadOnlySequence<byte> value,
    DistributedCacheEntryOptions options,
    CancellationToken token = default) =>
    _dataAccessStrategy.SetAsync(
      key,
      value,
      options,
      token);

  private readonly IBufferDistributedCacheDataAccessStrategy _dataAccessStrategy;
}
