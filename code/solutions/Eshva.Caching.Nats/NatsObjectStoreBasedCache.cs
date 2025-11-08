using Eshva.Caching.Abstractions;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Eshva.Caching.Nats;

/// <summary>
/// NATS object store based distributed cache.
/// </summary>
[PublicAPI]
public sealed class NatsObjectStoreBasedCache : BufferDistributedCache {
  /// <summary>
  /// Initializes a new instance of a NATS object store based distributed cache.
  /// </summary>
  /// <param name="cacheDatastore">Cache datastore.</param>
  /// <param name="cacheInvalidation">Cache invalidation.</param>
  /// <param name="logger">Logger.</param>
  /// <exception cref="ArgumentNullException">
  /// Value of a required argument isn't specified.
  /// </exception>
  public NatsObjectStoreBasedCache(
    ObjectStoreBasedDatastore cacheDatastore,
    ObjectStoreBasedCacheInvalidation cacheInvalidation,
    ILogger<NatsObjectStoreBasedCache>? logger = null)
    : base(cacheInvalidation, cacheDatastore, logger) { }
}
