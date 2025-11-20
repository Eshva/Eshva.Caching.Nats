using Eshva.Caching.Abstractions.Distributed;
using Eshva.Caching.Nats.Distributed;
using Eshva.Caching.Nats.Tests.OutOfProcess.Common;
using FluentAssertions;
using Meziantou.Extensions.Logging.Xunit.v3;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using NATS.Net;
using Reqnroll;

namespace Eshva.Caching.Nats.Tests.OutOfProcess.InDiContainerRegistration;

[Binding]
internal sealed class InDiContainerRegistrationSteps {
  public InDiContainerRegistrationSteps(CachesContext cachesContext) {
    _cachesContext = cachesContext;
  }

  [Given("service collection")]
  public void GivenServiceCollection() =>
    _serviceCollection = new ServiceCollection();

  [Given("object store based cache settings registered in DI-container with key '(.*)'")]
  public void GivenObjectStoreBasedCacheSettingsRegisteredInDiContainerWithKey(string serviceKey) =>
    _serviceCollection.AddKeyedSingleton(serviceKey, _objectStoreSettings);

  [Given("key-value store based cache settings registered in DI-container with key '(.*)'")]
  public void GivenKeyValueStoreBasedCacheSettingsRegisteredInDiContainerWithKey(string serviceKey) =>
    _serviceCollection.AddKeyedSingleton(serviceKey, _keyValueStoreSettings);

  [Given("object store based cache settings registered in DI-container without key")]
  public void GivenObjectStoreBasedCacheSettingsRegisteredInDiContainerWithoutKey() =>
    _serviceCollection.AddSingleton(_objectStoreSettings);

  [Given("key-value store based cache settings registered in DI-container without key")]
  public void GivenKeyValueStoreBasedCacheSettingsRegisteredInDiContainerWithoutKey() =>
    _serviceCollection.AddSingleton(_keyValueStoreSettings);

  [Given("NATS client registered in DI-container with key '(.*)'")]
  public void GivenNatsClientRegisteredInDiContainerWithKey(string natsClientKey) =>
    _serviceCollection.AddKeyedSingleton(natsClientKey, _cachesContext.Connection);

  [Given("NATS client registered in DI-container")]
  public void GivenNatsClientRegisteredInDiContainer() =>
    _serviceCollection.AddSingleton(_cachesContext.Connection);

  [Given("time provider registered in DI-container")]
  public void GivenTimeProviderRegisteredInDiContainer() =>
    _serviceCollection.AddSingleton(TimeProvider.System);

  [Given("cache entry expiry calculator registered in DI-container")]
  public void GivenCacheEntryExpiryCalculatorRegisteredInDiContainer() =>
    _serviceCollection.AddSingleton(
      new CacheEntryExpiryCalculator(_objectStoreSettings.DefaultSlidingExpirationInterval, TimeProvider.System));

  [Given("object store bucket created")]
  public async Task GivenObjectStoreBucketCreated() =>
    await _cachesContext.Connection.CreateJetStreamContext()
      .CreateObjectStoreContext()
      .CreateObjectStoreAsync(_objectStoreSettings.BucketName);

  [Given("key-value store bucket created")]
  public async Task GivenKeyValueStoreBucketCreated() =>
    await _cachesContext.Connection.CreateJetStreamContext()
      .CreateKeyValueStoreContext()
      .CreateStoreAsync(_keyValueStoreSettings.BucketName);

  [Given("cache logger is registered in DI-container")]
  public void GivenCacheLoggerIsRegisteredInDiContainer() {
    _serviceCollection.AddSingleton(XUnitLogger.CreateLogger<NatsObjectStoreBasedCache>(_cachesContext.Logger));
    _serviceCollection.AddSingleton(XUnitLogger.CreateLogger<NatsKeyValueStoreBasedCache>(_cachesContext.Logger));
  }

  [Given("cache invalidation logger is registered in DI-container")]
  public void GivenCacheInvalidationLoggerIsRegisteredInDiContainer() {
    _serviceCollection.AddSingleton(XUnitLogger.CreateLogger<ObjectStoreBasedCacheInvalidation>(_cachesContext.Logger));
    _serviceCollection.AddSingleton(XUnitLogger.CreateLogger<KeyValueBasedCacheInvalidation>(_cachesContext.Logger));
  }

  [When("I register object store based cache in DI-container with key '(.*)' and NATS client key '(.*)'")]
  public void WhenIRegisterObjectStoreBasedCacheInDiContainerWithKeyAndNatsClientKey(string serviceKey, string natsClientKey) =>
    _serviceCollection.AddKeyedNatsObjectStoreBasedCache(serviceKey, natsClientKey);

  [When("I register key-value store based cache in DI-container with key '(.*)' and NATS client key '(.*)'")]
  public void WhenIRegisterKeyValueStoreBasedCacheInDiContainerWithKeyAndNatsClientKey(string serviceKey, string natsClientKey) =>
    _serviceCollection.AddKeyedNatsKeyValueBasedCache(serviceKey, natsClientKey);

  [When("I register object store based cache in DI-container with key '(.*)' and NATS client without key")]
  public void WhenIRegisterObjectStoreBasedCacheInDiContainerWithKeyAndNatsClientWithoutKey(string serviceKey) =>
    _serviceCollection.AddKeyedNatsObjectStoreBasedCache(serviceKey);

  [When("I register key-value store based cache in DI-container with key '(.*)' and NATS client without key")]
  public void WhenIRegisterKeyValueStoreBasedCacheInDiContainerWithKeyAndNatsClientWithoutKey(string serviceKey) =>
    _serviceCollection.AddKeyedNatsKeyValueBasedCache(serviceKey);

  [When("I register object store based cache in DI-container without key and NATS client without key")]
  public void WhenIRegisterObjectStoreBasedCacheInDiContainerWithoutKeyAndNatsClientWithoutKey() =>
    _serviceCollection.AddNatsObjectStoreBasedCache();

  [When("I register key-value store based cache in DI-container without key and NATS client without key")]
  public void WhenIRegisterKeyValueStoreBasedCacheInDiContainerWithoutKeyAndNatsClientWithoutKey() =>
    _serviceCollection.AddNatsKeyValueBasedCache();

  [When("I register object store based cache in DI-container without key and NATS client key '(.*)'")]
  public void WhenIRegisterObjectStoreBasedCacheInDiContainerWithoutKeyAndNatsClientKey(string natsClientKey) =>
    _serviceCollection.AddNatsObjectStoreBasedCache(natsClientKey);

  [When("I register key-value store based cache in DI-container without key and NATS client key '(.*)'")]
  public void WhenIRegisterKeyValueStoreBasedCacheInDiContainerWithoutKeyAndNatsClientKey(string natsClientKey) =>
    _serviceCollection.AddNatsKeyValueBasedCache(natsClientKey);

  [Then("service provided created from service collection")]
  public void ThenServiceProvidedCreatedFromServiceCollection() =>
    _serviceProvider = _serviceCollection.BuildServiceProvider();

  [Then("it should be possible to get cache instance with key '(.*)'")]
  public void ThenItShouldBePossibleToGetCacheInstanceWithKey(string serviceKey) {
    _serviceProvider.GetKeyedService<IBufferDistributedCache>(serviceKey).Should().NotBeNull();
    _serviceProvider.GetKeyedService<IDistributedCache>(serviceKey).Should().NotBeNull();
  }

  [Then("it should be possible to get cache instance without key")]
  public void ThenItShouldBePossibleToGetCacheInstanceWithoutKey() {
    _serviceProvider.GetService<IBufferDistributedCache>().Should().NotBeNull();
    _serviceProvider.GetService<IDistributedCache>().Should().NotBeNull();
  }

  private readonly ObjectStoreBasedCacheSettings _objectStoreSettings = new() {
    BucketName = "cache",
    DefaultSlidingExpirationInterval = TimeSpan.FromMinutes(minutes: 1),
    ExpiredEntriesPurgingInterval = TimeSpan.FromMinutes(minutes: 4),
    MaximalCacheInvalidationDuration = TimeSpan.FromMinutes(minutes: 2)
  };

  private readonly KeyValueBasedCacheSettings _keyValueStoreSettings = new() {
    BucketName = "cache",
    DefaultSlidingExpirationInterval = TimeSpan.FromMinutes(minutes: 1),
    ExpiredEntriesPurgingInterval = TimeSpan.FromMinutes(minutes: 4),
    MaximalCacheInvalidationDuration = TimeSpan.FromMinutes(minutes: 2)
  };

  private readonly CachesContext _cachesContext;
  private ServiceCollection _serviceCollection = null!;
  private ServiceProvider _serviceProvider = null!;
}
