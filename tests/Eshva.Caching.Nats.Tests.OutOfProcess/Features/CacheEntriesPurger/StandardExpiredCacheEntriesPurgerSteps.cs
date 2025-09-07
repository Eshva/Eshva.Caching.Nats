using System;
using System.Threading;
using System.Threading.Tasks;
using Eshva.Caching.Nats.Tests.OutOfProcess.Common;
using Microsoft.Extensions.Internal;
using Reqnroll;
using Xunit;

namespace Eshva.Caching.Nats.Tests.OutOfProcess.Features.CacheEntriesPurger;

[Binding]
public class StandardExpiredCacheEntriesPurgerSteps {
  public StandardExpiredCacheEntriesPurgerSteps(CachesContext cachesContext, ErrorHandlingContext errorHandlingContext) {
    _cachesContext = cachesContext;
    _errorHandlingContext = errorHandlingContext;
  }

  [Given("minimal expired entries purging interval is {int} minutes")]
  public void GivenMinimalExpiredEntriesPurgingIntervalIsMinutes(int minutes) =>
    _minimalExpiredEntriesPurgingInterval = TimeSpan.FromMinutes(minutes);

  [Given("default expired entries purging interval is {int} minutes")]
  public void GivenDefaultExpiredEntriesPurgingIntervalIsMinutes(int minutes) =>
    _defaultExpiredEntriesPurgingInterval = TimeSpan.FromMinutes(minutes);

  [When("I construct standard purger with purging interval of (.*) minutes and clock set at today (.*)")]
  public void WhenIConstructStandardPurgerWithPurgingIntervalOfIntMinutesAndClockSetAtToday(
    int purgingIntervalMinutes,
    TimeSpan currentTime) {
    var purgingInterval = TimeSpan.FromMinutes(purgingIntervalMinutes);
    _cachesContext.Clock.AdjustTime(currentTime);
    try {
      _sut = new TestPurger(
        _defaultExpiredEntriesPurgingInterval,
        _minimalExpiredEntriesPurgingInterval,
        purgingInterval,
        _cachesContext.Clock);
    }
    catch (Exception exception) {
      _errorHandlingContext.LastException = exception;
    }
  }

  [When("I construct standard purger without purging interval")]
  public void WhenIConstructStandardPurgerWithoutPurgingInterval() {
    try {
      _sut = new TestPurger(
        _defaultExpiredEntriesPurgingInterval,
        _minimalExpiredEntriesPurgingInterval,
        clock: _cachesContext.Clock);
    }
    catch (Exception exception) {
      _errorHandlingContext.LastException = exception;
    }
  }

  [Then("purging interval should be set to default purging interval")]
  public async Task ThenPurgingIntervalShouldBeSetToDefaultPurgingInterval() {
    _cachesContext.Clock.AdjustTime(_defaultExpiredEntriesPurgingInterval.Add(TimeSpan.FromSeconds(seconds: 1)));
    _sut.ScanForExpiredEntriesIfRequired();
    for (var turn = 0; turn < 10; turn++) {
      if (_sut.IsPurgeStarted) return;
      await Task.Delay(millisecondsDelay: 10);
    }

    Assert.Fail("Purging not started on time.");
  }

  private readonly CachesContext _cachesContext;
  private readonly ErrorHandlingContext _errorHandlingContext;
  private TestPurger _sut = null!;
  private static TimeSpan _defaultExpiredEntriesPurgingInterval;
  private static TimeSpan _minimalExpiredEntriesPurgingInterval;

  private sealed class TestPurger : StandardExpiredCacheEntriesPurger {
    public TestPurger(
      TimeSpan defaultExpiredEntriesPurgingInterval,
      TimeSpan minimalExpiredEntriesPurgingInterval,
      TimeSpan? expiredEntriesPurgingInterval = null,
      ISystemClock? clock = null)
      : base(expiredEntriesPurgingInterval, clock) {
      _defaultExpiredEntriesPurgingInterval = defaultExpiredEntriesPurgingInterval;
      _minimalExpiredEntriesPurgingInterval = minimalExpiredEntriesPurgingInterval;
    }

    public bool IsPurgeStarted { get; private set; }

    protected override TimeSpan DefaultExpiredEntriesPurgingInterval => _defaultExpiredEntriesPurgingInterval;

    protected override TimeSpan MinimalExpiredEntriesPurgingInterval => _minimalExpiredEntriesPurgingInterval;

    protected override Task DeleteExpiredCacheEntries(CancellationToken token) {
      IsPurgeStarted = true;
      return Task.CompletedTask;
    }
  }
}
