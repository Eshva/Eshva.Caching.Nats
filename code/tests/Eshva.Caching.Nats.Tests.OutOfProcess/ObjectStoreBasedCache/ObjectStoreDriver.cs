using Eshva.Caching.Abstractions;
using Eshva.Caching.Nats.Tests.OutOfProcess.Common;
using NATS.Client.ObjectStore;
using NATS.Client.ObjectStore.Models;
using Xunit.Abstractions;

namespace Eshva.Caching.Nats.Tests.OutOfProcess.ObjectStoreBasedCache;

public sealed class ObjectStoreDriver : ICacheStorageDriver {
  public ObjectStoreDriver(INatsObjStore bucket, ITestOutputHelper logger) {
    _bucket = bucket ?? throw new ArgumentNullException(nameof(bucket));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
  }

  public async Task PutEntry(
    string key,
    byte[] value,
    CacheEntryExpiry entryExpiry) {
    var metadataAccessor = new ObjectMetadataAccessor(new ObjectMetadata { Name = key }) {
      ExpiresAtUtc = entryExpiry.ExpiresAtUtc,
      AbsoluteExpiryAtUtc = entryExpiry.AbsoluteExpiryAtUtc,
      SlidingExpiryInterval = entryExpiry.SlidingExpiryInterval
    };

    await _bucket.PutAsync(metadataAccessor.ObjectMetadata, new MemoryStream(value));
    _logger.WriteLine($"Put entry '{key}' that expires at {entryExpiry.ExpiresAtUtc}");
  }

  public async Task<bool> DoesExist(string key) {
    try {
      await _bucket.GetInfoAsync(key);
      return true;
    }
    catch (NatsObjNotFoundException) {
      return false;
    }
  }

  public async Task<CacheEntryExpiry> GetMetadata(string key) {
    var objectMetadata = await _bucket.GetInfoAsync(key);
    var metadataAccessor = new ObjectMetadataAccessor(objectMetadata);
    return new CacheEntryExpiry(
      metadataAccessor.ExpiresAtUtc,
      metadataAccessor.AbsoluteExpiryAtUtc,
      metadataAccessor.SlidingExpiryInterval);
  }

  public async Task Remove(string key) =>
    await _bucket.DeleteAsync(key);

  private readonly INatsObjStore _bucket;
  private readonly ITestOutputHelper _logger;
}
