using Eshva.Caching.Abstractions;
using Microsoft.Extensions.Logging;
using NATS.Client.ObjectStore;

namespace Eshva.Caching.Nats.ObjectStore.DataAccessors;

public class RefreshEntryAsyncUsingMetadata : NatsObjectStoreBasedDataAccessor, IRefreshEntryAsync {
  public RefreshEntryAsyncUsingMetadata(
    INatsObjStore cacheBucket,
    ICacheEntryExpirationStrategy expirationStrategy,
    ICacheExpiredEntriesPurger expiredEntriesPurger,
    ILogger? logger = null)
    : base(
      cacheBucket,
      expirationStrategy,
      expiredEntriesPurger,
      logger) { }

  public async Task RefreshAsync(string key, CancellationToken token = default) {
    ValidateKey(key);
    await ExpiredEntriesPurger.ScanForExpiredEntriesIfRequired(token);

    try {
      var objectMetadata = await CacheBucket.GetInfoAsync(key, showDeleted: false, token);
      await RefreshExpiresAt(objectMetadata, token);
    }
    catch (NatsObjException exception) {
      throw new InvalidOperationException($"An entry with key '{key}' could not be found in the cache.", exception);
    }
  }
}
