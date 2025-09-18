using Eshva.Caching.Abstractions;
using Microsoft.Extensions.Logging;
using NATS.Client.ObjectStore;

namespace Eshva.Caching.Nats.ObjectStore.DataAccessors;

public class RemoveEntryAsync : NatsObjectStoreBasedDataAccessor, IRemoveEntryAsync {
  public RemoveEntryAsync(
    INatsObjStore cacheBucket,
    ICacheEntryExpirationStrategy expirationStrategy,
    ICacheExpiredEntriesPurger expiredEntriesPurger,
    ILogger? logger = null)
    : base(
      cacheBucket,
      expirationStrategy,
      expiredEntriesPurger,
      logger) { }

  public async Task RemoveAsync(string key, CancellationToken token = default) {
    ValidateKey(key);
    await ExpiredEntriesPurger.ScanForExpiredEntriesIfRequired(token);

    try {
      await CacheBucket.DeleteAsync(key, token);
    }
    catch (NatsObjException exception) {
      throw new InvalidOperationException($"An occurred on removing entry with key '{key}'.", exception);
    }
  }
}