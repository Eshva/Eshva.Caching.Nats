using System;
using System.Threading.Tasks;
using Eshva.Caching.Nats.Tests.OutOfProcess.Common;
using Eshva.Caching.Nats.Tests.Tools;
using Reqnroll;

namespace Eshva.Caching.Nats.Tests.OutOfProcess.Features.ObjectStoreBasedCache;

[Binding]
public class GetEntrySteps {
  public GetEntrySteps(CachesContext cachesContext, ErrorHandlingContext errorHandlingContext) {
    _cachesContext = cachesContext;
    _errorHandlingContext = errorHandlingContext;
  }

  [When("I get {string} cache entry asynchronously")]
  public async Task WhenIGetCacheEntry(string key) {
    try {
      await Task.Delay(millisecondsDelay: 1000);
      _cachesContext.GottenCacheEntryValue = await _cachesContext.Cache.GetAsync(key);
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

  [When("I get from cache some entry with corrupted metadata asynchronously")]
  public async Task WhenIGetSomeCacheEntryAsynchronouslyOnCacheWithClosedConnection() {
    const string key = "some-key";
    await _cachesContext.Bucket.PutAsync(key, "some-value"u8.ToArray());
    var metadataCorrupter = new ObjectEntryMetadataCorrupter();
    await metadataCorrupter.CorruptEntryMetadata(_cachesContext.Bucket, key);

    try {
      await _cachesContext.Cache.GetAsync("some-key");
    }
    catch (Exception exception) {
      _errorHandlingContext.LastException = exception;
    }
  }

  private readonly CachesContext _cachesContext;
  private readonly ErrorHandlingContext _errorHandlingContext;
}
