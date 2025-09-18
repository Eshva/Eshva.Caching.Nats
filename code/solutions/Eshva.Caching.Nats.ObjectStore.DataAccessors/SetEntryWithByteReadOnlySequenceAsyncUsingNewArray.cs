using System.Buffers;
using Eshva.Caching.Abstractions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using NATS.Client.ObjectStore;

namespace Eshva.Caching.Nats.ObjectStore.DataAccessors;

public class SetEntryWithByteReadOnlySequenceAsyncUsingNewArray
  : NatsObjectStoreBasedDataAccessor,
    ISetEntryWithByteReadOnlySequenceAsync {
  public SetEntryWithByteReadOnlySequenceAsyncUsingNewArray(
    INatsObjStore cacheBucket,
    ICacheEntryExpirationStrategy expirationStrategy,
    ICacheExpiredEntriesPurger expiredEntriesPurger,
    ILogger? logger = null)
    : base(
      cacheBucket,
      expirationStrategy,
      expiredEntriesPurger,
      logger) { }

  public async ValueTask SetAsync(
    string key,
    ReadOnlySequence<byte> value,
    DistributedCacheEntryOptions options,
    CancellationToken token = default) =>
    throw new NotImplementedException();
}
