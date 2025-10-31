using Eshva.Caching.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NATS.Client.ObjectStore;
using Xunit.Abstractions;

namespace Eshva.Caching.Nats.Tests.OutOfProcess.Common;

public class CachesContext {
  public CachesContext(INatsObjStore objectStore, ITestOutputHelper xUnitLogger) {
    _xUnitLogger = xUnitLogger;
    Bucket = objectStore;
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
    XUnitLogger = xUnitLogger;
  }

  public ITestOutputHelper XUnitLogger { get; }

  public DateTimeOffset Today { get; }

  public TimeSpan ExpiredEntriesPurgingInterval { get; set; }

  public TimeSpan DefaultSlidingExpirationInterval { get; set; }

  public FakeTimeProvider TimeProvider { get; }

  public INatsObjStore Bucket { get; }

  public NatsObjectStoreBasedCache NatsObjectStoreBasedCache { get; private set; } = null!;

  public byte[]? GottenCacheEntryValue { get; set; } = [];

  public void CreateAndAssignCacheServices() {
    var cacheInvalidation = new ObjectStoreBasedCacheInvalidation(
      Bucket,
      new TimeBasedCacheInvalidationSettings {
        ExpiredEntriesPurgingInterval = ExpiredEntriesPurgingInterval, DefaultSlidingExpirationInterval = DefaultSlidingExpirationInterval
      },
      TimeProvider,
      Meziantou.Extensions.Logging.Xunit.XUnitLogger.CreateLogger<ObjectStoreBasedCacheInvalidation>(_xUnitLogger));

    NatsObjectStoreBasedCache = new NatsObjectStoreBasedCache(
      Bucket,
      cacheInvalidation,
      Meziantou.Extensions.Logging.Xunit.XUnitLogger.CreateLogger<NatsObjectStoreBasedCache>(_xUnitLogger));
  }

  private readonly ITestOutputHelper _xUnitLogger;
}
