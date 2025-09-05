using Microsoft.Extensions.Internal;
using NATS.Client.ObjectStore;

namespace Eshva.Caching.Nats.Tests.OutOfProcess.Common;

public class CachesContext {
  public CachesContext(INatsObjStore objectStore) {
    CacheBucket = objectStore;
    Cache = new NatsObjectStoreBaseCache(objectStore, new SystemClock());
  }

  public INatsObjStore CacheBucket { get; }

  public NatsObjectStoreBaseCache Cache { get; }

  public byte[]? GottenCacheEntryValue { get; set; } = null;
}
