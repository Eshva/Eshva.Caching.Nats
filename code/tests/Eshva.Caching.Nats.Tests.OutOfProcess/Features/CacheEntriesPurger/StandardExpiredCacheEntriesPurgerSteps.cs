using Eshva.Caching.Abstractions;
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

  [When("I construct standard purger with purging interval of (.*) minutes and clock set at today (.*)")]
  public void WhenIConstructStandardPurgerWithPurgingIntervalOfIntMinutesAndClockSetAtToday(
    int purgingIntervalMinutes,
    TimeSpan currentTime) {
    var purgingInterval = TimeSpan.FromMinutes(purgingIntervalMinutes);
    _cachesContext.Clock.AdjustTime(currentTime);
    try {
      _sut = new TestPurger(purgingInterval, _minimalExpiredEntriesPurgingInterval, _cachesContext.Clock);
    }
    catch (Exception exception) {
      _errorHandlingContext.LastException = exception;
    }
  }

  [When("I construct standard purger with purging interval of (.*) minutes")]
  public void WhenIConstructStandardPurgerWithPurgingIntervalOf(int purgingIntervalMinutes) {
    var purgingInterval = TimeSpan.FromMinutes(purgingIntervalMinutes);
    try {
      _sut = new TestPurger(purgingInterval, _minimalExpiredEntriesPurgingInterval, _cachesContext.Clock);
    }
    catch (Exception exception) {
      _errorHandlingContext.LastException = exception;
    }
  }

  [Then("purging should start after {int} minutes")]
  public async Task ThenPurgingShouldStartAfterMinutes(int minutes) {
    _cachesContext.Clock.AdjustTime(TimeSpan.FromMinutes(minutes));
    await _sut.ScanForExpiredEntriesIfRequired();
    for (var turn = 0; turn < 10; turn++) {
      if (_sut.IsPurgeStarted) return;
      await Task.Delay(millisecondsDelay: 10);
    }

    Assert.Fail("Purging not started on time.");
  }

  private readonly CachesContext _cachesContext;
  private readonly ErrorHandlingContext _errorHandlingContext;
  private TimeSpan _minimalExpiredEntriesPurgingInterval;
  private TestPurger _sut = null!;

  private sealed class TestPurger : StandardExpiredCacheEntriesPurger {
    public TestPurger(
      TimeSpan expiredEntriesPurgingInterval,
      TimeSpan minimalExpiredEntriesPurgingInterval,
      ISystemClock clock)
      : base(
        new PurgerSettings {
          ExpiredEntriesPurgingInterval = expiredEntriesPurgingInterval
        },
        minimalExpiredEntriesPurgingInterval,
        clock) { }

    public bool IsPurgeStarted { get; private set; }

    protected override Task DeleteExpiredCacheEntries(CancellationToken token) {
      IsPurgeStarted = true;
      return Task.CompletedTask;
    }
  }
}
