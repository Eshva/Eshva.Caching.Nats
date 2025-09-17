using System;
using Eshva.Caching.Nats.Tests.Tools;
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
    Clock = new GovernedSystemClock(Today);
    XUnitLogger = xUnitLogger;
  }

  public ITestOutputHelper XUnitLogger { get; }

  public DateTimeOffset Today { get; }

  public TimeSpan ExpiredEntriesPurgingInterval { get; set; }

  public TimeSpan DefaultSlidingExpirationInterval { get; set; }

  public GovernedSystemClock Clock { get; }

  public INatsObjStore Bucket { get; }

  public NatsObjectStoreBasedCache Cache { get; private set; } = null!;

  public byte[]? GottenCacheEntryValue { get; set; }

  public void CreateAndAssignCacheServices() {
    var expirationStrategy = new StandardCacheEntryExpirationStrategy(
      new ExpirationStrategySettings {
        DefaultSlidingExpirationInterval = DefaultSlidingExpirationInterval
      },
      Clock);

    var expiredEntriesPurger = new ObjectStoreBasedCacheExpiredEntriesPurger(
      Bucket,
      expirationStrategy,
      new PurgerSettings {
        ExpiredEntriesPurgingInterval = ExpiredEntriesPurgingInterval
      },
      Clock,
      Meziantou.Extensions.Logging.Xunit.XUnitLogger.CreateLogger<ObjectStoreBasedCacheExpiredEntriesPurger>(_xUnitLogger)) {
      ShouldPurgeSynchronously = true
    };

    Cache = new NatsObjectStoreBasedCache(
      Bucket,
      expirationStrategy,
      expiredEntriesPurger,
      Meziantou.Extensions.Logging.Xunit.XUnitLogger.CreateLogger<NatsObjectStoreBasedCache>(_xUnitLogger));
  }

  private readonly ITestOutputHelper _xUnitLogger;
}
