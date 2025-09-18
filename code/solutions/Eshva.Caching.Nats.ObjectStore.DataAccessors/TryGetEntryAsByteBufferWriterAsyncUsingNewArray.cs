using System.Buffers;
using Eshva.Caching.Abstractions;
using Microsoft.Extensions.Logging;
using NATS.Client.ObjectStore;

namespace Eshva.Caching.Nats.ObjectStore.DataAccessors;

public class TryGetEntryAsByteBufferWriterAsyncUsingNewArray
  : NatsObjectStoreBasedDataAccessor,
    ITryGetEntryAsByteBufferWriterAsync {
  public TryGetEntryAsByteBufferWriterAsyncUsingNewArray(
    INatsObjStore cacheBucket,
    ICacheEntryExpirationStrategy expirationStrategy,
    ICacheExpiredEntriesPurger expiredEntriesPurger,
    ILogger? logger = null)
    : base(
      cacheBucket,
      expirationStrategy,
      expiredEntriesPurger,
      logger) { }

  public async ValueTask<bool> TryGetAsync(string key, IBufferWriter<byte> destination, CancellationToken token = default) {
    ValidateKey(key);
    await ExpiredEntriesPurger.ScanForExpiredEntriesIfRequired(token);

    try {
      destination.Write(await CacheBucket.GetBytesAsync(key, token));
      var objectMetadata = await CacheBucket.GetInfoAsync(key, showDeleted: false, token);

      Logger.LogDebug(
        "An object with the key '{Key}' has been read. Object meta-data: @{ObjectMetadata}",
        key,
        objectMetadata);

      await RefreshExpiresAt(objectMetadata, token);

      return true;
    }
    catch (NatsObjNotFoundException) {
      return false;
    }
    catch (NatsObjException exception) {
      throw new InvalidOperationException($"Failed to read cache key '{key}' value.", exception);
    }
  }
}
