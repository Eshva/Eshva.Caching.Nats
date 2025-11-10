using Eshva.Caching.Nats.Tests.OutOfProcess.Common;
using Eshva.Testing.Reqnroll.Contexts;
using FluentAssertions;
using NATS.Client.Core;
using Reqnroll;

namespace Eshva.Caching.Nats.Tests.OutOfProcess.ObjectStoreBasedCache;

[Binding]
public class TryGetEntrySteps {
  public TryGetEntrySteps(CachesContext cachesContext, ErrorHandlingContext errorHandlingContext) {
    _cachesContext = cachesContext;
    _errorHandlingContext = errorHandlingContext;
  }

  [When("I try get {string} cache entry asynchronously")]
  public async Task WhenITryGetStringCacheEntryAsynchronously(string key) {
    try {
      var destination = new NatsBufferWriter<byte>();
      _isSuccessfullyRead = await _cachesContext.Cache.TryGetAsync(key, destination).ConfigureAwait(continueOnCapturedContext: false);
      _cachesContext.GottenCacheEntryValue = destination.WrittenMemory.ToArray();
    }
    catch (Exception exception) {
      _errorHandlingContext.LastException = exception;
    }
  }

  [When("I try get {string} cache entry synchronously")]
  public void WhenITryGetStringCacheEntrySynchronously(string key) {
    try {
      var destination = new NatsBufferWriter<byte>();
      _isSuccessfullyRead = _cachesContext.Cache.TryGet(key, destination);
      _cachesContext.GottenCacheEntryValue = destination.WrittenMemory.ToArray();
    }
    catch (Exception exception) {
      _errorHandlingContext.LastException = exception;
    }
  }

  [Then("cache entry successfully read")]
  public void ThenCacheEntrySuccessfullyRead() =>
    _isSuccessfullyRead.Should().BeTrue();

  [Then("cache entry did not read")]
  public void ThenCacheEntryDidNotRead() =>
    _isSuccessfullyRead.Should().BeFalse();

  private readonly CachesContext _cachesContext;
  private readonly ErrorHandlingContext _errorHandlingContext;
  private bool _isSuccessfullyRead;
}
