using System;
using Eshva.Caching.Nats.Tests.Tools;
using Meziantou.Extensions.Logging.Xunit;
using Microsoft.Extensions.Logging;
using NATS.Client.ObjectStore;
using Xunit.Abstractions;

namespace Eshva.Caching.Nats.Tests.OutOfProcess.Common;

public class CachesContext : IDisposable {
  public CachesContext(INatsObjStore objectStore, ITestOutputHelper logger) {
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
    ExpiredEntriesPurgingInterval = TimeSpan.FromMinutes(minutes: 2);
    ExpirationStrategy = new StandardCacheEntryExpirationStrategy(TimeSpan.FromMinutes(minutes: 1), Clock);
    Logger = XUnitLogger.CreateLogger<ObjectStoreBasedCacheExpiredEntriesPurger>(logger);
    ExpiredEntriesPurger = new ObjectStoreBasedCacheExpiredEntriesPurger(
      objectStore,
      ExpirationStrategy,
      ExpiredEntriesPurgingInterval,
      Clock,
      Logger);
    Cache = new NatsObjectStoreBasedCache(
      objectStore,
      ExpirationStrategy,
      ExpiredEntriesPurger,
      XUnitLogger.CreateLogger<NatsObjectStoreBasedCache>(logger));
  }

  public ILogger<ObjectStoreBasedCacheExpiredEntriesPurger> Logger { get; }

  public DateTimeOffset Today { get; }

  public ICacheEntryExpirationStrategy ExpirationStrategy { get; }

  public ICacheExpiredEntriesPurger ExpiredEntriesPurger { get; }

  public TimeSpan ExpiredEntriesPurgingInterval { get; }

  public GovernedSystemClock Clock { get; }

  public INatsObjStore Bucket { get; }

  public NatsObjectStoreBasedCache Cache { get; }

  public byte[]? GottenCacheEntryValue { get; set; }

  public void Dispose() => Cache.Dispose();
}
