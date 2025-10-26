using Eshva.Caching.Abstractions;
using Eshva.Caching.Nats.Tests.OutOfProcess.Common;
using Meziantou.Extensions.Logging.Xunit;
using Reqnroll;

namespace Eshva.Caching.Nats.Tests.OutOfProcess.Features.CacheInvalidation;

[Binding]
public class ObjectStoreBasedCacheInvalidationSteps {
  public ObjectStoreBasedCacheInvalidationSteps(CachesContext cachesContext) {
    _cachesContext = cachesContext;
  }

  [Given("NATS object-store based cache invalidation with purging interval {int} minutes with synchronous purge")]
  public void GivenNatsObjectStoreBasedCacheInvalidationWithPurgingIntervalIntMinutesWithSynchronousPurge(int minutes) {
    var purgingInterval = TimeSpan.FromMinutes(minutes);
    _sut = new ObjectStoreBasedCacheInvalidation(
      _cachesContext.Bucket,
      new TimeBasedCacheInvalidationSettings {
        ExpiredEntriesPurgingInterval = purgingInterval, DefaultSlidingExpirationInterval = TimeSpan.FromMinutes(minutes: 1)
      },
      _cachesContext.TimeProvider,
      XUnitLogger.CreateLogger<ObjectStoreBasedCacheInvalidation>(_cachesContext.XUnitLogger)) { ShouldPurgeSynchronously = true };
  }

  [When("I request scan for expired entries if required")]
  public async Task WhenIRequestScanForExpiredEntriesIfRequired() => await _sut.PurgeEntriesIfRequired();

  private readonly CachesContext _cachesContext;
  private ObjectStoreBasedCacheInvalidation _sut = null!;
}
