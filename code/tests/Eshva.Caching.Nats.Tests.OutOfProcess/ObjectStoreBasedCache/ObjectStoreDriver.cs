using Eshva.Caching.Abstractions.Distributed;
using Eshva.Caching.Nats.Distributed;
using Eshva.Caching.Nats.Tests.OutOfProcess.Common;
using NATS.Client.ObjectStore;
using NATS.Client.ObjectStore.Models;
using Xunit;

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

    await _bucket.PutAsync(metadataAccessor.ObjectMetadata, new MemoryStream(value)).ConfigureAwait(continueOnCapturedContext: false);
    _logger.WriteLine($"Put entry '{key}' that expires at {entryExpiry.ExpiresAtUtc}");
  }

  public async Task<bool> DoesExist(string key) {
    try {
      await _bucket.GetInfoAsync(key).ConfigureAwait(continueOnCapturedContext: false);
      return true;
    }
    catch (NatsObjNotFoundException) {
      return false;
    }
  }

  public async Task<CacheEntryExpiry> GetMetadata(string key) {
    var objectMetadata = await _bucket.GetInfoAsync(key).ConfigureAwait(continueOnCapturedContext: false);
    var metadataAccessor = new ObjectMetadataAccessor(objectMetadata);
    return new CacheEntryExpiry(
      metadataAccessor.ExpiresAtUtc,
      metadataAccessor.AbsoluteExpiryAtUtc,
      metadataAccessor.SlidingExpiryInterval);
  }

  public async Task Remove(string key) =>
    await _bucket.DeleteAsync(key).ConfigureAwait(continueOnCapturedContext: false);

  private readonly INatsObjStore _bucket;
  private readonly ITestOutputHelper _logger;
}
