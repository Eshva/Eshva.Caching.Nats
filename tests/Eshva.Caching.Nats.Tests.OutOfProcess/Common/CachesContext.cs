using System;
using Eshva.Caching.Nats.Tests.Tools;
using NATS.Client.ObjectStore;
using Xunit.Abstractions;

namespace Eshva.Caching.Nats.Tests.OutOfProcess.Common;

public class CachesContext : IDisposable {
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

  public ICacheEntryExpirationStrategy ExpirationStrategy { get; set; } = null!;

  public ICacheExpiredEntriesPurger ExpiredEntriesPurger { get; set; } = null!;

  public TimeSpan ExpiredEntriesPurgingInterval { get; set; }

  public TimeSpan DefaultSlidingExpirationInterval { get; set; }

  public GovernedSystemClock Clock { get; }

  public INatsObjStore Bucket { get; }

  public NatsObjectStoreBasedCache Cache { get; private set; } = null!;

  public byte[]? GottenCacheEntryValue { get; set; }

  public void CreateAndAssignCacheServices() {
    ExpirationStrategy = new StandardCacheEntryExpirationStrategy(
      new ExpirationStrategySettings {
        DefaultSlidingExpirationInterval = DefaultSlidingExpirationInterval
      },
      Clock);
    ExpiredEntriesPurger = new ObjectStoreBasedCacheExpiredEntriesPurger(
      Bucket,
      ExpirationStrategy,
      new PurgerSettings {
        ExpiredEntriesPurgingInterval = ExpiredEntriesPurgingInterval
      },
      Clock,
      Meziantou.Extensions.Logging.Xunit.XUnitLogger.CreateLogger<ObjectStoreBasedCacheExpiredEntriesPurger>(_xUnitLogger));
    Cache = new NatsObjectStoreBasedCache(
      Bucket,
      ExpirationStrategy,
      ExpiredEntriesPurger,
      Meziantou.Extensions.Logging.Xunit.XUnitLogger.CreateLogger<NatsObjectStoreBasedCache>(_xUnitLogger));
  }

  public void Dispose() => Cache.Dispose();

  private readonly ITestOutputHelper _xUnitLogger;
}
