using Eshva.Caching.Abstractions;
using Microsoft.Extensions.Logging;

namespace Eshva.Caching.Nats;

/// <summary>
/// NATS key-value store based distributed cache.
/// </summary>
public sealed class NatsKeyValueStoreBasedCache : BufferDistributedCache {
  /// <summary>
  /// Initializes a new instance of a NATS key-value store based distributed cache.
  /// </summary>
  /// <param name="cacheDatastore">Cache datastore.</param>
  /// <param name="cacheInvalidation">Cache invalidation.</param>
  /// <param name="logger">Logger.</param>
  /// <exception cref="ArgumentNullException">
  /// Value of a required argument isn't specified.
  /// </exception>
  public NatsKeyValueStoreBasedCache(
    KeyValueBasedDatastore cacheDatastore,
    KeyValueBasedCacheInvalidation cacheInvalidation,
    ILogger? logger = null)
    : base(cacheInvalidation, cacheDatastore, logger) { }
}
