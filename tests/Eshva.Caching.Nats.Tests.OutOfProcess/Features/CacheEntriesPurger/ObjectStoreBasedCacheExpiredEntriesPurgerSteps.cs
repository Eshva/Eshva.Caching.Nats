using System;
using Eshva.Caching.Nats.Tests.OutOfProcess.Common;
using Reqnroll;

namespace Eshva.Caching.Nats.Tests.OutOfProcess.Features.CacheEntriesPurger;

[Binding]
public class ObjectStoreBasedCacheExpiredEntriesPurgerSteps {
  public ObjectStoreBasedCacheExpiredEntriesPurgerSteps(CachesContext cachesContext) {
    _cachesContext = cachesContext;
  }

  [Given("purger for NATS object-store based cache with purging interval {int} minutes")]
  public void GivenPurgerForNatsObjectStoreBasedCacheWithPurgingIntervalMinutes(int minutes) {
    var purgingInterval = TimeSpan.FromMinutes(minutes);
    _sut = new ObjectStoreBasedCacheExpiredEntriesPurger(
      _cachesContext.Bucket,
      _cachesContext.ExpirationStrategy,
      purgingInterval,
      _cachesContext.Clock,
      _cachesContext.Logger);
  }

  [When("I request scan for expired entries if required")]
  public void WhenIRequestScanForExpiredEntriesIfRequired() => _sut.ScanForExpiredEntriesIfRequired();

  private readonly CachesContext _cachesContext;
  private ObjectStoreBasedCacheExpiredEntriesPurger _sut = null!;
}
