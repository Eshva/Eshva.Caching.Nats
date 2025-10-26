using System.Text;
using Eshva.Caching.Nats.Tests.OutOfProcess.Common;
using Microsoft.Extensions.Caching.Distributed;
using Reqnroll;

namespace Eshva.Caching.Nats.Tests.OutOfProcess.Features.ObjectStoreBasedCache;

[Binding]
public class SetEntryUsingByteArraySteps {
  public SetEntryUsingByteArraySteps(CachesContext cachesContext, ErrorHandlingContext errorHandlingContext) {
    _cachesContext = cachesContext;
    _errorHandlingContext = errorHandlingContext;
  }

  [When("I set using byte array asynchronously '(.*)' cache entry with value '(.*)' and sliding expiration in (.*) minutes")]
  public async Task WhenISetUsingByteArrayAsynchronouslyCacheEntryWithValueAndSlidingExpirationInMinutes(
    string key,
    string value,
    int minutes) {
    try {
      await _cachesContext.NatsObjectStoreBasedCache.SetAsync(
        key,
        Encoding.UTF8.GetBytes(value),
        new DistributedCacheEntryOptions { SlidingExpiration = TimeSpan.FromMinutes(minutes) });
    }
    catch (Exception exception) {
      _errorHandlingContext.LastException = exception;
    }
  }

  [When("I set using byte array synchronously '(.*)' cache entry with value '(.*)' and sliding expiration in (.*) minutes")]
  public void WhenISetUsingByteArraySynchronouslyCacheEntryWithValueAndSlidingExpirationInMinutes(string key, string value, int minutes) {
    try {
      _cachesContext.NatsObjectStoreBasedCache.Set(
        key,
        Encoding.UTF8.GetBytes(value),
        new DistributedCacheEntryOptions { SlidingExpiration = TimeSpan.FromMinutes(minutes) });
    }
    catch (Exception exception) {
      _errorHandlingContext.LastException = exception;
    }
  }

  [When(@"I set using byte array asynchronously '(.*)' cache entry with value '(.*)' and absolute expiration at today at (\d+:\d+)")]
  public async Task WhenISetUsingByteArrayAsynchronouslyCacheEntryWithValueAndAbsoluteExpirationAtTodayAtDd(
    string key,
    string value,
    TimeSpan timeOfDay) {
    try {
      await _cachesContext.NatsObjectStoreBasedCache.SetAsync(
        key,
        Encoding.UTF8.GetBytes(value),
        new DistributedCacheEntryOptions { AbsoluteExpiration = _cachesContext.Today.Add(timeOfDay) });
    }
    catch (Exception exception) {
      _errorHandlingContext.LastException = exception;
    }
  }

  [When(
    @"I set using byte array asynchronously '(.*)' cache entry with value '(.*)' and absolute expiration at today at (\d+:\d+) and sliding expiration in (.*) minutes")]
  public async Task WhenISetUsingByteArrayAsynchronouslyCacheEntryWithValueAndAbsoluteExpirationAtTodayAtDdAndSlidingExpirationInMinutes(
    string key,
    string value,
    TimeSpan timeOfDay,
    int minutes) {
    try {
      await _cachesContext.NatsObjectStoreBasedCache.SetAsync(
        key,
        Encoding.UTF8.GetBytes(value),
        new DistributedCacheEntryOptions {
          AbsoluteExpiration = _cachesContext.Today.Add(timeOfDay), SlidingExpiration = TimeSpan.FromMinutes(minutes)
        });
    }
    catch (Exception exception) {
      _errorHandlingContext.LastException = exception;
    }
  }

  [When(
    @"I set using byte array synchronously '(.*)' cache entry with value '(.*)' and absolute expiration at today at (\d+:\d+) and sliding expiration in (.*) minutes")]
  public async Task WhenISetUsingByteArraySynchronouslyCacheEntryWithValueAndAbsoluteExpirationAtTodayAtDdAndSlidingExpirationInMinutes(
    string key,
    string value,
    TimeSpan timeOfDay,
    int minutes) {
    try {
      await _cachesContext.NatsObjectStoreBasedCache.SetAsync(
        key,
        Encoding.UTF8.GetBytes(value),
        new DistributedCacheEntryOptions {
          AbsoluteExpiration = _cachesContext.Today.Add(timeOfDay), SlidingExpiration = TimeSpan.FromMinutes(minutes)
        });
    }
    catch (Exception exception) {
      _errorHandlingContext.LastException = exception;
    }
  }

  [When(@"I set using byte array synchronously '(.*)' cache entry with value '(.*)' and absolute expiration at today at (\d+:\d+)")]
  public void WhenISetUsingByteArraySynchronouslyCacheEntryWithValueAndAbsoluteExpirationAtTodayAtDd(
    string key,
    string value,
    TimeSpan timeOfDay) {
    try {
      _cachesContext.NatsObjectStoreBasedCache.Set(
        key,
        Encoding.UTF8.GetBytes(value),
        new DistributedCacheEntryOptions { AbsoluteExpiration = _cachesContext.Today.Add(timeOfDay) });
    }
    catch (Exception exception) {
      _errorHandlingContext.LastException = exception;
    }
  }

  [When("I set using byte array asynchronously '(.*)' cache entry with value '(.*)' and absolute expiration (.*) relative to now")]
  public async Task
    WhenISetUsingByteArrayAsynchronouslyCacheEntryWithValueAndAbsoluteExpirationRelativeToNow(
      string key,
      string value,
      TimeSpan timeOfDay) {
    try {
      await _cachesContext.NatsObjectStoreBasedCache.SetAsync(
        key,
        Encoding.UTF8.GetBytes(value),
        new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = timeOfDay });
    }
    catch (Exception exception) {
      _errorHandlingContext.LastException = exception;
    }
  }

  [When("I set using byte array synchronously '(.*)' cache entry with value '(.*)' and absolute expiration (.*) relative to now")]
  public void WhenISetUsingByteArraySynchronouslyCacheEntryWithValueAndAbsoluteExpirationRelativeToNow(
    string key,
    string value,
    TimeSpan timeOfDay) {
    try {
      _cachesContext.NatsObjectStoreBasedCache.Set(
        key,
        Encoding.UTF8.GetBytes(value),
        new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = timeOfDay });
    }
    catch (Exception exception) {
      _errorHandlingContext.LastException = exception;
    }
  }

  private readonly CachesContext _cachesContext;
  private readonly ErrorHandlingContext _errorHandlingContext;
}
