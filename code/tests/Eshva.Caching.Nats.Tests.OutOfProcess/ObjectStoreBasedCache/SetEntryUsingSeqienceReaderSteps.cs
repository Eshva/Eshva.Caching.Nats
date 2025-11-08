using System.Buffers;
using System.Text;
using Eshva.Caching.Nats.Tests.OutOfProcess.Common;
using Microsoft.Extensions.Caching.Distributed;
using Reqnroll;

namespace Eshva.Caching.Nats.Tests.OutOfProcess.ObjectStoreBasedCache;

[Binding]
public class SetEntryUsingSequenceReaderSteps {
  public SetEntryUsingSequenceReaderSteps(CachesContext cachesContext, ErrorHandlingContext errorHandlingContext) {
    _cachesContext = cachesContext;
    _errorHandlingContext = errorHandlingContext;
  }

  [When("I set using sequence reader asynchronously '(.*)' cache entry with value '(.*)' and sliding expiration in (.*) minutes")]
  public async Task WhenISetUsingSequenceReaderAsynchronouslyCacheEntryWithValueAndSlidingExpirationInMinutes(
    string key,
    string value,
    int minutes) {
    try {
      await _cachesContext.Cache.SetAsync(
          key,
          new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(value)),
          new DistributedCacheEntryOptions { SlidingExpiration = TimeSpan.FromMinutes(minutes) })
        .ConfigureAwait(continueOnCapturedContext: false);
    }
    catch (Exception exception) {
      _errorHandlingContext.LastException = exception;
    }
  }

  [When("I set using sequence reader synchronously '(.*)' cache entry with value '(.*)' and sliding expiration in (.*) minutes")]
  public void WhenISetUsingSequenceReaderSynchronouslyCacheEntryWithValueAndSlidingExpirationInMinutes(
    string key,
    string value,
    int minutes) {
    try {
      _cachesContext.Cache.Set(
        key,
        new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(value)),
        new DistributedCacheEntryOptions { SlidingExpiration = TimeSpan.FromMinutes(minutes) });
    }
    catch (Exception exception) {
      _errorHandlingContext.LastException = exception;
    }
  }

  [When(@"I set using sequence reader asynchronously '(.*)' cache entry with value '(.*)' and absolute expiration at today at (\d+:\d+)")]
  public async Task WhenISetUsingSequenceReaderAsynchronouslyCacheEntryWithValueAndAbsoluteExpirationAtTodayAtDd(
    string key,
    string value,
    TimeSpan timeOfDay) {
    try {
      await _cachesContext.Cache.SetAsync(
          key,
          new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(value)),
          new DistributedCacheEntryOptions { AbsoluteExpiration = _cachesContext.Today.Add(timeOfDay) })
        .ConfigureAwait(continueOnCapturedContext: false);
    }
    catch (Exception exception) {
      _errorHandlingContext.LastException = exception;
    }
  }

  [When(
    @"I set using sequence reader asynchronously '(.*)' cache entry with value '(.*)' and absolute expiration at today at (\d+:\d+) and sliding expiration in (.*) minutes")]
  public async Task
    WhenISetUsingSequenceReaderAsynchronouslyCacheEntryWithValueAndAbsoluteExpirationAtTodayAtDdAndSlidingExpirationInMinutes(
      string key,
      string value,
      TimeSpan timeOfDay,
      int minutes) {
    try {
      await _cachesContext.Cache.SetAsync(
          key,
          new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(value)),
          new DistributedCacheEntryOptions {
            AbsoluteExpiration = _cachesContext.Today.Add(timeOfDay), SlidingExpiration = TimeSpan.FromMinutes(minutes)
          })
        .ConfigureAwait(continueOnCapturedContext: false);
    }
    catch (Exception exception) {
      _errorHandlingContext.LastException = exception;
    }
  }

  [When(
    @"I set using sequence reader synchronously '(.*)' cache entry with value '(.*)' and absolute expiration at today at (\d+:\d+) and sliding expiration in (.*) minutes")]
  public async Task
    WhenISetUsingSequenceReaderSynchronouslyCacheEntryWithValueAndAbsoluteExpirationAtTodayAtDdAndSlidingExpirationInMinutes(
      string key,
      string value,
      TimeSpan timeOfDay,
      int minutes) {
    try {
      await _cachesContext.Cache.SetAsync(
          key,
          new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(value)),
          new DistributedCacheEntryOptions {
            AbsoluteExpiration = _cachesContext.Today.Add(timeOfDay), SlidingExpiration = TimeSpan.FromMinutes(minutes)
          })
        .ConfigureAwait(continueOnCapturedContext: false);
    }
    catch (Exception exception) {
      _errorHandlingContext.LastException = exception;
    }
  }

  [When(@"I set using sequence reader synchronously '(.*)' cache entry with value '(.*)' and absolute expiration at today at (\d+:\d+)")]
  public void WhenISetUsingSequenceReaderSynchronouslyCacheEntryWithValueAndAbsoluteExpirationAtTodayAtDd(
    string key,
    string value,
    TimeSpan timeOfDay) {
    try {
      _cachesContext.Cache.Set(
        key,
        new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(value)),
        new DistributedCacheEntryOptions { AbsoluteExpiration = _cachesContext.Today.Add(timeOfDay) });
    }
    catch (Exception exception) {
      _errorHandlingContext.LastException = exception;
    }
  }

  [When("I set using sequence reader asynchronously '(.*)' cache entry with value '(.*)' and absolute expiration (.*) relative to now")]
  public async Task
    WhenISetUsingSequenceReaderAsynchronouslyCacheEntryWithValueAndAbsoluteExpirationRelativeToNow(
      string key,
      string value,
      TimeSpan timeOfDay) {
    try {
      await _cachesContext.Cache.SetAsync(
          key,
          new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(value)),
          new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = timeOfDay })
        .ConfigureAwait(continueOnCapturedContext: false);
    }
    catch (Exception exception) {
      _errorHandlingContext.LastException = exception;
    }
  }

  [When("I set using sequence reader synchronously '(.*)' cache entry with value '(.*)' and absolute expiration (.*) relative to now")]
  public void WhenISetUsingSequenceReaderSynchronouslyCacheEntryWithValueAndAbsoluteExpirationRelativeToNow(
    string key,
    string value,
    TimeSpan timeOfDay) {
    try {
      _cachesContext.Cache.Set(
        key,
        new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(value)),
        new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = timeOfDay });
    }
    catch (Exception exception) {
      _errorHandlingContext.LastException = exception;
    }
  }

  private readonly CachesContext _cachesContext;
  private readonly ErrorHandlingContext _errorHandlingContext;
}
