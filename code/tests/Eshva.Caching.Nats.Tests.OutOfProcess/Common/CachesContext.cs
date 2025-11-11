using Eshva.Caching.Abstractions;
using Eshva.Caching.Nats.Tests.OutOfProcess.KeyValueBasedCache;
using Eshva.Caching.Nats.Tests.OutOfProcess.ObjectStoreBasedCache;
using Meziantou.Extensions.Logging.Xunit;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Time.Testing;
using NATS.Client.KeyValueStore;
using NATS.Client.ObjectStore;
using Xunit.Abstractions;

namespace Eshva.Caching.Nats.Tests.OutOfProcess.Common;

public class CachesContext {
  public CachesContext(
    INatsObjStore objectStore,
    INatsKVStore entriesStore,
    ITestOutputHelper xUnitLogger) {
    Bucket = objectStore;
    EntriesStore = entriesStore;
    var now = DateTimeOffset.UtcNow;
    Today = new DateTimeOffset(
      now.Year,
      now.Month,
      now.Day,
      hour: 0,
      minute: 0,
      second: 0,
      TimeSpan.Zero);
    TimeProvider = new FakeTimeProvider(Today);
    _xUnitLogger = xUnitLogger;
  }

  public DateTimeOffset Today { get; }

  public TimeSpan ExpiredEntriesPurgingInterval { get; set; }

  public TimeSpan DefaultSlidingExpirationInterval { get; set; }

  public FakeTimeProvider TimeProvider { get; }

  public INatsObjStore Bucket { get; }

  public IBufferDistributedCache Cache { get; private set; } = null!;

  public byte[]? GottenCacheEntryValue { get; set; } = [];

  public ManualResetEventSlim PurgingSignal { get; } = new(initialState: false);

  public ICacheStorageDriver Driver { get; private set; } = null!;

  public INatsKVStore EntriesStore { get; }

  public void CreateObjectStoreDriver() {
    var expiryCalculator = new CacheEntryExpiryCalculator(DefaultSlidingExpirationInterval, TimeProvider);

    var cacheInvalidation = new ObjectStoreBasedCacheInvalidation(
      Bucket,
      ExpiredEntriesPurgingInterval,
      expiryCalculator,
      TimeProvider,
      XUnitLogger.CreateLogger<ObjectStoreBasedCacheInvalidation>(_xUnitLogger));

    var cacheDatastore = new ObjectStoreBasedDatastore(Bucket, expiryCalculator);
    cacheInvalidation.CacheInvalidationCompleted += (_, _) => { PurgingSignal.Set(); };

    Cache = new NatsObjectStoreBasedCache(
      cacheDatastore,
      cacheInvalidation,
      XUnitLogger.CreateLogger<NatsObjectStoreBasedCache>(_xUnitLogger));
    Driver = new ObjectStoreDriver(Bucket, _xUnitLogger);
  }

  public void CreateKeyValueStoreDriver() {
    var expiryCalculator = new CacheEntryExpiryCalculator(DefaultSlidingExpirationInterval, TimeProvider);
    var expirySerializer = new CacheEntryExpiryBinarySerializer();
    var cacheInvalidation = new KeyValueBasedCacheInvalidation(
      EntriesStore,
      ExpiredEntriesPurgingInterval,
      expirySerializer,
      expiryCalculator,
      TimeProvider,
      XUnitLogger.CreateLogger<KeyValueBasedCacheInvalidation>(_xUnitLogger));

    var cacheDatastore = new KeyValueBasedDatastore(
      EntriesStore,
      expirySerializer,
      expiryCalculator);
    cacheInvalidation.CacheInvalidationCompleted += (_, _) => { PurgingSignal.Set(); };

    Cache = new NatsKeyValueStoreBasedCache(
      cacheDatastore,
      cacheInvalidation,
      XUnitLogger.CreateLogger<NatsKeyValueStoreBasedCache>(_xUnitLogger));
    Driver = new KeyValueStoreDriver(
      EntriesStore,
      expirySerializer,
      _xUnitLogger);
  }

  private readonly ITestOutputHelper _xUnitLogger;
}
