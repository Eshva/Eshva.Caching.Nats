using Eshva.Caching.Abstractions;
using Eshva.Caching.Nats.Tests.OutOfProcess.Common;
using Meziantou.Extensions.Logging.Xunit;
using Reqnroll;

namespace Eshva.Caching.Nats.Tests.OutOfProcess.Features.CacheEntriesPurger;

[Binding]
public class ObjectStoreBasedCacheExpiredEntriesPurgerSteps {
  public ObjectStoreBasedCacheExpiredEntriesPurgerSteps(CachesContext cachesContext) {
    _cachesContext = cachesContext;
  }

  [Given("purger for NATS object-store based cache with purging interval {int} minutes with synchronous purge")]
  public void GivenPurgerForNatsObjectStoreBasedCacheWithPurgingIntervalMinutes(int minutes) {
    var purgingInterval = TimeSpan.FromMinutes(minutes);
    _sut = new ObjectStoreBasedCacheExpiredEntriesPurger(
      _cachesContext.Bucket,
      new StandardCacheEntryExpirationStrategy(
        new ExpirationStrategySettings {
          DefaultSlidingExpirationInterval = TimeSpan.FromMinutes(minutes: 1)
        },
        _cachesContext.TimeProvider),
      new PurgerSettings {
        ExpiredEntriesPurgingInterval = purgingInterval
      },
      _cachesContext.TimeProvider,
      XUnitLogger.CreateLogger<ObjectStoreBasedCacheExpiredEntriesPurger>(_cachesContext.XUnitLogger)) {
      ShouldPurgeSynchronously = true
    };
  }

  [When("I request scan for expired entries if required")]
  public async Task WhenIRequestScanForExpiredEntriesIfRequired() => await _sut.PurgeExpiredEntriesIfRequired();

  private readonly CachesContext _cachesContext;
  private ObjectStoreBasedCacheExpiredEntriesPurger _sut = null!;
}
