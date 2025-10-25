using Eshva.Caching.Abstractions;
using Eshva.Caching.Nats.Tests.OutOfProcess.Common;
using Meziantou.Extensions.Logging.Xunit;
using Reqnroll;

namespace Eshva.Caching.Nats.Tests.OutOfProcess.Features.CacheInvalidator;

[Binding]
public class ObjectStoreBasedCacheInvalidatorSteps {
  public ObjectStoreBasedCacheInvalidatorSteps(CachesContext cachesContext) {
    _cachesContext = cachesContext;
  }

  [Given("purger for NATS object-store based cache with purging interval {int} minutes with synchronous purge")]
  public void GivenPurgerForNatsObjectStoreBasedCacheWithPurgingIntervalMinutes(int minutes) {
    var purgingInterval = TimeSpan.FromMinutes(minutes);
    _sut = new ObjectStoreBasedCacheInvalidator(
      _cachesContext.Bucket,
      new Abstractions.StandardTimeBasedCacheInvalidation(
        new StandardTimeBasedCacheInvalidationSettings { DefaultSlidingExpirationInterval = TimeSpan.FromMinutes(minutes: 1) },
        _cachesContext.TimeProvider),
      new TimeBasedCacheInvalidatorSettings { ExpiredEntriesPurgingInterval = purgingInterval },
      _cachesContext.TimeProvider,
      XUnitLogger.CreateLogger<ObjectStoreBasedCacheInvalidator>(_cachesContext.XUnitLogger)) { ShouldPurgeSynchronously = true };
  }

  [When("I request scan for expired entries if required")]
  public async Task WhenIRequestScanForExpiredEntriesIfRequired() => await _sut.PurgeEntriesIfRequired();

  private readonly CachesContext _cachesContext;
  private ObjectStoreBasedCacheInvalidator _sut = null!;
}
