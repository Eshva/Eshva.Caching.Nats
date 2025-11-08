using Eshva.Caching.Nats.Tests.OutOfProcess.Common;
using Reqnroll;

namespace Eshva.Caching.Nats.Tests.OutOfProcess.KeyValueBasedCache;

[Binding]
public class KeyValueBasedCacheSteps {
  public KeyValueBasedCacheSteps(CachesContext cachesContext) {
    _cachesContext = cachesContext;
  }

  [Given("key-value store based cache")]
  public void GivenKeyValueStoreBasedCache() => _cachesContext.CreateKeyValueStoreDriver();

  private readonly CachesContext _cachesContext;
}
