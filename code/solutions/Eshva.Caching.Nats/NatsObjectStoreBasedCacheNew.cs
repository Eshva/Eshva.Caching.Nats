using Eshva.Caching.Abstractions;
using JetBrains.Annotations;

namespace Eshva.Caching.Nats;

/// <summary>
/// NATS object-store based distributed cache.
/// </summary>
[PublicAPI]
public sealed class NatsObjectStoreBasedCacheNew : BufferDistributedCache {
  /// <summary>
  /// Initializes a new instance of a NATS object-store based distributed cache.
  /// </summary>
  /// <param name="dataAccessStrategy">Data access strategy used by this cache.</param>
  /// <exception cref="ArgumentNullException">
  /// One of required arguments isn't specified.
  /// </exception>
  public NatsObjectStoreBasedCacheNew(IBufferDistributedCacheDataAccessStrategy dataAccessStrategy)
    : base(dataAccessStrategy) { }
}
