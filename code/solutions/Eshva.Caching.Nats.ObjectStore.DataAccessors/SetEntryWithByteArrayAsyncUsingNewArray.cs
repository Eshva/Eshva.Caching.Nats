using Eshva.Caching.Abstractions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using NATS.Client.ObjectStore;

namespace Eshva.Caching.Nats.ObjectStore.DataAccessors;

public class SetEntryWithByteArrayAsyncUsingNewArray : NatsObjectStoreBasedDataAccessor, ISetEntryWithByteArrayAsync {
  public SetEntryWithByteArrayAsyncUsingNewArray(
    INatsObjStore cacheBucket,
    ICacheEntryExpirationStrategy expirationStrategy,
    ICacheExpiredEntriesPurger expiredEntriesPurger,
    ILogger? logger = null) : base(
    cacheBucket,
    expirationStrategy,
    expiredEntriesPurger,
    logger) { }

  public async Task SetAsync(
    string key,
    byte[] value,
    DistributedCacheEntryOptions options,
    CancellationToken token = default) {
    ValidateKey(key);
    await ExpiredEntriesPurger.ScanForExpiredEntriesIfRequired(token);

    try {
      var objectMetadata = await CacheBucket.PutAsync(key, value, token);
      objectMetadata.Metadata = FillCacheEntryMetadata(options);
      await CacheBucket.UpdateMetaAsync(key, objectMetadata, token);
      Logger.LogDebug("An entry with '{Key}' put into cache", key);
    }
    catch (NatsObjException exception) {
      throw new InvalidOperationException($"An entry with key '{key}' could not be found in the cache.", exception);
    }
  }
}
