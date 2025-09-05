using System;
using Eshva.Caching.Nats.Tests.Tools;
using Meziantou.Extensions.Logging.Xunit;
using NATS.Client.ObjectStore;
using Xunit.Abstractions;

namespace Eshva.Caching.Nats.Tests.OutOfProcess.Common;

public class CachesContext : IDisposable {
  public CachesContext(INatsObjStore objectStore, ITestOutputHelper logger) {
    Bucket = objectStore;
    Clock = new GovernedSystemClock();
    Settings = new NatsCacheSettings {
      ExpiredEntriesPurgingInterval = TimeSpan.FromMinutes(minutes: 2)
    };
    Cache = new NatsObjectStoreBaseCache(
      objectStore,
      Settings,
      Clock,
      XUnitLogger.CreateLogger<NatsObjectStoreBaseCache>(logger));
  }

  public NatsCacheSettings Settings { get; }

  public GovernedSystemClock Clock { get; }

  public INatsObjStore Bucket { get; }

  public NatsObjectStoreBaseCache Cache { get; }

  public byte[]? GottenCacheEntryValue { get; set; }

  public void Dispose() => Cache.Dispose();
}
