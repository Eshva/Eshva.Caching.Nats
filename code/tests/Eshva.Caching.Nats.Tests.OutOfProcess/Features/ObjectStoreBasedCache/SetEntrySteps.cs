using System;
using System.Text;
using System.Threading.Tasks;
using Eshva.Caching.Nats.Tests.OutOfProcess.Common;
using Microsoft.Extensions.Caching.Distributed;
using Reqnroll;

namespace Eshva.Caching.Nats.Tests.OutOfProcess.Features.ObjectStoreBasedCache;

[Binding]
public class SetEntrySteps {
  public SetEntrySteps(CachesContext cachesContext, ErrorHandlingContext errorHandlingContext) {
    _cachesContext = cachesContext;
    _errorHandlingContext = errorHandlingContext;
  }

  [When("I set asynchronously '(.*)' cache entry with value '(.*)' and sliding expiration in (.*) minutes")]
  public async Task WhenISetCacheEntryAsynchronouslyWithSlidingExpirationInMinutes(string key, string value, int minutes) {
    try {
      await _cachesContext.Cache.SetAsync(
        key,
        Encoding.UTF8.GetBytes(value),
        new DistributedCacheEntryOptions {
          SlidingExpiration = TimeSpan.FromMinutes(minutes)
        });
    }
    catch (Exception exception) {
      _errorHandlingContext.LastException = exception;
    }
  }

  [When("I set synchronously '(.*)' cache entry with value '(.*)' and sliding expiration in (.*) minutes")]
  public void WhenISetSynchronouslyCacheEntryWithValueAndSlidingExpirationInMinutes(string key, string value, int minutes) {
    try {
      _cachesContext.Cache.Set(
        key,
        Encoding.UTF8.GetBytes(value),
        new DistributedCacheEntryOptions {
          SlidingExpiration = TimeSpan.FromMinutes(minutes)
        });
    }
    catch (Exception exception) {
      _errorHandlingContext.LastException = exception;
    }
  }

  [When(@"I set asynchronously '(.*)' cache entry with value '(.*)' and absolute expiration at today at (\d+:\d+)")]
  public async Task WhenISetAsynchronouslyCacheEntryWithValueAndAbsoluteExpirationAtTodayAt(string key, string value, TimeSpan timeOfDay) {
    try {
      await _cachesContext.Cache.SetAsync(
        key,
        Encoding.UTF8.GetBytes(value),
        new DistributedCacheEntryOptions {
          AbsoluteExpiration = _cachesContext.Today.Add(timeOfDay)
        });
    }
    catch (Exception exception) {
      _errorHandlingContext.LastException = exception;
    }
  }

  [When(
    @"I set asynchronously '(.*)' cache entry with value '(.*)' and absolute expiration at today at (\d+:\d+) and sliding expiration in (.*) minutes")]
  public async Task WhenISetAsynchronouslyCacheEntryWithValueAndAbsoluteExpirationAtTodayAtAndSlidingExpirationInMinutes(
    string key,
    string value,
    TimeSpan timeOfDay,
    int minutes) {
    try {
      await _cachesContext.Cache.SetAsync(
        key,
        Encoding.UTF8.GetBytes(value),
        new DistributedCacheEntryOptions {
          AbsoluteExpiration = _cachesContext.Today.Add(timeOfDay),
          SlidingExpiration = TimeSpan.FromMinutes(minutes)
        });
    }
    catch (Exception exception) {
      _errorHandlingContext.LastException = exception;
    }
  }

  [When(
    @"I set synchronously '(.*)' cache entry with value '(.*)' and absolute expiration at today at (\d+:\d+) and sliding expiration in (.*) minutes")]
  public async Task WhenISetSynchronouslyCacheEntryWithValueAndAbsoluteExpirationAtTodayAtDdAndSlidingExpirationInMinutes(
    string key,
    string value,
    TimeSpan timeOfDay,
    int minutes) {
    try {
      await _cachesContext.Cache.SetAsync(
        key,
        Encoding.UTF8.GetBytes(value),
        new DistributedCacheEntryOptions {
          AbsoluteExpiration = _cachesContext.Today.Add(timeOfDay),
          SlidingExpiration = TimeSpan.FromMinutes(minutes)
        });
    }
    catch (Exception exception) {
      _errorHandlingContext.LastException = exception;
    }
  }

  [When(@"I set synchronously '(.*)' cache entry with value '(.*)' and absolute expiration at today at (\d+:\d+)")]
  public void WhenISetSynchronouslyCacheEntryWithValueAndAbsoluteExpirationAtTodayAt(string key, string value, TimeSpan timeOfDay) {
    try {
      _cachesContext.Cache.Set(
        key,
        Encoding.UTF8.GetBytes(value),
        new DistributedCacheEntryOptions {
          AbsoluteExpiration = _cachesContext.Today.Add(timeOfDay)
        });
    }
    catch (Exception exception) {
      _errorHandlingContext.LastException = exception;
    }
  }

  [When("I set asynchronously '(.*)' cache entry with value '(.*)' and absolute expiration (.*) relative to now")]
  public async Task
    WhenISetAsynchronouslyCacheEntryWithValueAndAbsoluteExpirationRelativeToNow(string key, string value, TimeSpan timeOfDay) {
    try {
      await _cachesContext.Cache.SetAsync(
        key,
        Encoding.UTF8.GetBytes(value),
        new DistributedCacheEntryOptions {
          AbsoluteExpirationRelativeToNow = timeOfDay
        });
    }
    catch (Exception exception) {
      _errorHandlingContext.LastException = exception;
    }
  }

  [When("I set synchronously '(.*)' cache entry with value '(.*)' and absolute expiration (.*) relative to now")]
  public void WhenISetSynchronouslyCacheEntryWithValueAndAbsoluteExpirationRelativeToNow(string key, string value, TimeSpan timeOfDay) {
    try {
      _cachesContext.Cache.Set(
        key,
        Encoding.UTF8.GetBytes(value),
        new DistributedCacheEntryOptions {
          AbsoluteExpirationRelativeToNow = timeOfDay
        });
    }
    catch (Exception exception) {
      _errorHandlingContext.LastException = exception;
    }
  }

  private readonly CachesContext _cachesContext;
  private readonly ErrorHandlingContext _errorHandlingContext;
}
