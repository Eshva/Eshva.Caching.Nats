using Eshva.Caching.Nats.Tests.OutOfProcess.Common;
using Reqnroll;

namespace Eshva.Caching.Nats.Tests.OutOfProcess.ObjectStoreBasedCache;

[Binding]
public class RefreshEntrySteps {
  public RefreshEntrySteps(CachesContext cachesContext, ErrorHandlingContext errorHandlingContext) {
    _cachesContext = cachesContext;
    _errorHandlingContext = errorHandlingContext;
  }

  [When("I refresh {string} cache entry asynchronously")]
  public async Task WhenIRefreshCacheEntryAsynchronously(string key) {
    try {
      await _cachesContext.Cache.RefreshAsync(key).ConfigureAwait(continueOnCapturedContext: false);
    }
    catch (Exception exception) {
      _errorHandlingContext.LastException = exception;
    }
  }

  [When("I refresh {string} cache entry synchronously")]
  public void WhenIRefreshCacheEntrySynchronously(string key) {
    try {
      _cachesContext.Cache.Refresh(key);
    }
    catch (Exception exception) {
      _errorHandlingContext.LastException = exception;
    }
  }

  private readonly CachesContext _cachesContext;
  private readonly ErrorHandlingContext _errorHandlingContext;
}
