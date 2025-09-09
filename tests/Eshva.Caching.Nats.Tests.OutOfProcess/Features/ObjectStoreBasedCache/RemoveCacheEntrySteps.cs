using System;
using System.Threading.Tasks;
using Eshva.Caching.Nats.Tests.OutOfProcess.Common;
using Reqnroll;

namespace Eshva.Caching.Nats.Tests.OutOfProcess.Features.ObjectStoreBasedCache;

[Binding]
public class RemoveCacheEntrySteps {
  public RemoveCacheEntrySteps(CachesContext cachesContext, ErrorHandlingContext errorHandlingContext) {
    _cachesContext = cachesContext;
    _errorHandlingContext = errorHandlingContext;
  }

  [When("I remove {string} cache entry asynchronously")]
  public async Task WhenIRemoveCacheEntryAsynchronously(string key) {
    try {
      await _cachesContext.Cache.RemoveAsync(key);
    }
    catch (Exception exception) {
      _errorHandlingContext.LastException = exception;
    }
  }

  [When("I remove {string} cache entry synchronously")]
  public void WhenIRemoveCacheEntrySynchronously(string key) {
    try {
      _cachesContext.Cache.Remove(key);
    }
    catch (Exception exception) {
      _errorHandlingContext.LastException = exception;
    }
  }

  private readonly CachesContext _cachesContext;
  private readonly ErrorHandlingContext _errorHandlingContext;
}
