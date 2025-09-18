using Eshva.Caching.Abstractions;
using Microsoft.Extensions.Logging;
using NATS.Client.ObjectStore;

namespace Eshva.Caching.Nats.ObjectStore.DataAccessors;

public class GetEntryAsByteArrayAsyncUsingNewArray
  : NatsObjectStoreBasedDataAccessor,
    IGetEntryAsByteArrayAsync {
  public GetEntryAsByteArrayAsyncUsingNewArray(
    INatsObjStore cacheBucket,
    ICacheEntryExpirationStrategy expirationStrategy,
    ICacheExpiredEntriesPurger expiredEntriesPurger,
    ILogger logger)
    : base(
      cacheBucket,
      expirationStrategy,
      expiredEntriesPurger,
      logger) { }

  async Task<byte[]?> IGetEntryAsByteArrayAsync.GetAsync(string key, CancellationToken token = default) {
    ValidateKey(key);
    await ExpiredEntriesPurger.ScanForExpiredEntriesIfRequired(token);

    var valueStream = new MemoryStream();

    try {
      var objectMetadata = await CacheBucket.GetAsync(
        key,
        valueStream,
        leaveOpen: true,
        token);
      Logger.LogDebug(
        "An object with the key '{Key}' has been read. Object meta-data: @{ObjectMetadata}",
        key,
        objectMetadata);

      await RefreshExpiresAt(objectMetadata, token);
    }
    catch (NatsObjNotFoundException) {
      return null;
    }
    catch (NatsObjException exception) {
      throw new InvalidOperationException($"Failed to read cache key '{key}' value.", exception);
    }

    if (valueStream.Length == 0) {
      Logger.LogDebug("No key '{Key}' has been found in the object-store", key);
      return null;
    }

    valueStream.Seek(offset: 0, SeekOrigin.Begin);
    var buffer = new byte[valueStream.Length];
    var bytesRead = await valueStream.ReadAsync(buffer, token);

    if (bytesRead != valueStream.Length) {
      throw new InvalidOperationException(
        $"Should be read {valueStream.Length} bytes but read {bytesRead} bytes for a cache entry wih ID '{key}'.");
    }

    return buffer;
  }
}
