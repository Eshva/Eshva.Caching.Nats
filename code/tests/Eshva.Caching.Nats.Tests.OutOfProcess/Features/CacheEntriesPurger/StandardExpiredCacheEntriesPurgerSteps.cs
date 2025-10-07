using Eshva.Caching.Abstractions;
using Eshva.Caching.Nats.Tests.OutOfProcess.Common;
using FluentAssertions;
using Meziantou.Extensions.Logging.Xunit;
using Microsoft.Extensions.Logging;
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

  [Given("standard purger with purging interval of {int} minutes")]
  public void GivenStandardPurgerWithPurgingIntervalOfMinutes(int minutes) =>
    _sut = new TestPurger(
      TimeSpan.FromMinutes(minutes),
      TimeSpan.FromMinutes(minutes: 2),
      _cachesContext.TimeProvider,
      XUnitLogger.CreateLogger<TestPurger>(_cachesContext.XUnitLogger));

  [When("I construct standard purger with purging interval of (.*) minutes and clock set at today (.*)")]
  public void WhenIConstructStandardPurgerWithPurgingIntervalOfIntMinutesAndClockSetAtToday(
    int purgingIntervalMinutes,
    TimeSpan currentTime) {
    var purgingInterval = TimeSpan.FromMinutes(purgingIntervalMinutes);
    _cachesContext.TimeProvider.AdjustTime(_cachesContext.Today + currentTime);
    try {
      _sut = new TestPurger(
        purgingInterval,
        _minimalExpiredEntriesPurgingInterval,
        _cachesContext.TimeProvider,
        XUnitLogger.CreateLogger<TestPurger>(_cachesContext.XUnitLogger));
    }
    catch (Exception exception) {
      _errorHandlingContext.LastException = exception;
    }
  }

  [When("I construct standard purger with purging interval of (.*) minutes")]
  public void WhenIConstructStandardPurgerWithPurgingIntervalOfMinutes(int purgingIntervalMinutes) {
    var purgingInterval = TimeSpan.FromMinutes(purgingIntervalMinutes);
    try {
      _sut = new TestPurger(
        purgingInterval,
        _minimalExpiredEntriesPurgingInterval,
        _cachesContext.TimeProvider,
        XUnitLogger.CreateLogger<TestPurger>(_cachesContext.XUnitLogger));
    }
    catch (Exception exception) {
      _errorHandlingContext.LastException = exception;
    }
  }

  [When("purging expired cache entries requested")]
  public async Task WhenPurgingExpiredCacheEntriesRequested() {
    try {
      await _sut.PurgeExpiredEntriesIfRequired();
    }
    catch (Exception exception) {
      _errorHandlingContext.LastException = exception;
    }
  }

  [When("a few concurrent purging expired cache entries requested")]
  public async Task WhenAFewConcurrentPurgingExpiredCacheEntriesRequested() {
    try {
      await Task.WhenAll(
        _sut.PurgeExpiredEntriesIfRequired(),
        _sut.PurgeExpiredEntriesIfRequired(),
        _sut.PurgeExpiredEntriesIfRequired(),
        _sut.PurgeExpiredEntriesIfRequired(),
        _sut.PurgeExpiredEntriesIfRequired());
    }
    catch (Exception exception) {
      _errorHandlingContext.LastException = exception;
    }
  }

  [Then("purging should be done")]
  public void ThenPurgingShouldBeDone() {
    _sut.IsPurgeStarted.Should().BeTrue();
    // for (var turn = 0; turn < 10; turn++) {
    //   if (_sut.IsPurgeStarted) return;
    //   await Task.Delay(millisecondsDelay: 10);
    // }
    //
    // Assert.Fail("Purging not started on time.");
  }

  [Then("purging should not start")] public void ThenPurgingShouldNotStart() => _sut.IsPurgeStarted.Should().BeFalse();

  [Then("only one purging should be done")]
  public void ThenOnlyOnePurgingShouldBeDone() {
    _sut.IsPurgeStarted.Should().BeTrue();
    _sut.NumberOfPurgeStarted.Should().Be(expected: 1);
  }

  private readonly CachesContext _cachesContext;
  private readonly ErrorHandlingContext _errorHandlingContext;
  private TimeSpan _minimalExpiredEntriesPurgingInterval;
  private TestPurger _sut = null!;

  private sealed class TestPurger : StandardExpiredCacheEntriesPurger {
    public TestPurger(
      TimeSpan expiredEntriesPurgingInterval,
      TimeSpan minimalExpiredEntriesPurgingInterval,
      TimeProvider timeProvider,
      ILogger logger)
      : base(
        new PurgerSettings { ExpiredEntriesPurgingInterval = expiredEntriesPurgingInterval },
        minimalExpiredEntriesPurgingInterval,
        timeProvider,
        logger) { }

    public bool IsPurgeStarted { get; private set; }

    public int NumberOfPurgeStarted { get; private set; }

    protected override Task DeleteExpiredCacheEntries(CancellationToken token) {
      IsPurgeStarted = true;
      NumberOfPurgeStarted++;
      return Task.CompletedTask;
    }
  }
}
