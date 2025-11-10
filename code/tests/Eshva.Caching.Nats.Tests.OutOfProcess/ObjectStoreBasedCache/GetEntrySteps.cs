using Eshva.Caching.Nats.Tests.OutOfProcess.Common;
using Eshva.Testing.Reqnroll.Contexts;
using Reqnroll;

namespace Eshva.Caching.Nats.Tests.OutOfProcess.ObjectStoreBasedCache;

[Binding]
public class GetEntrySteps {
  public GetEntrySteps(CachesContext cachesContext, ErrorHandlingContext errorHandlingContext) {
    _cachesContext = cachesContext;
    _errorHandlingContext = errorHandlingContext;
  }

  [When("I get {string} cache entry asynchronously")]
  public async Task WhenIGetCacheEntry(string key) {
    try {
      _cachesContext.GottenCacheEntryValue = await _cachesContext.Cache.GetAsync(key).ConfigureAwait(continueOnCapturedContext: false);
    }
    catch (Exception exception) {
      _errorHandlingContext.LastException = exception;
    }
  }

  [When("I get {string} cache entry synchronously")]
  public void WhenIGetCacheEntrySynchronously(string key) {
    try {
      _cachesContext.GottenCacheEntryValue = _cachesContext.Cache.Get(key);
    }
    catch (Exception exception) {
      _errorHandlingContext.LastException = exception;
    }
  }

  private readonly CachesContext _cachesContext;
  private readonly ErrorHandlingContext _errorHandlingContext;
}
