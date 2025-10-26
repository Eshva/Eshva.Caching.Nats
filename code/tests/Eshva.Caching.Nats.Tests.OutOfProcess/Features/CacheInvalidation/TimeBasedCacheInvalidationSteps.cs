using Eshva.Caching.Abstractions;
using Eshva.Caching.Nats.Tests.OutOfProcess.Common;
using FluentAssertions;
using Meziantou.Extensions.Logging.Xunit;
using Microsoft.Extensions.Logging;
using Reqnroll;

namespace Eshva.Caching.Nats.Tests.OutOfProcess.Features.CacheInvalidation;

[Binding]
public class TimeBasedCacheInvalidationSteps {
  public TimeBasedCacheInvalidationSteps(CachesContext cachesContext, ErrorHandlingContext errorHandlingContext) {
    _cachesContext = cachesContext;
    _errorHandlingContext = errorHandlingContext;
  }

  [Given("time-based cache invalidation with default sliding expiration time (.*) minutes")]
  public void GivenTimeBasedCacheInvalidationWithDefaultSlidingExpirationTimeMinutes(double defaultSlidingExpirationTime) =>
    _sut = new TestCacheInvalidation(
      TimeSpan.FromMinutes(minutes: 2),
      TimeSpan.FromMinutes(defaultSlidingExpirationTime),
      TimeSpan.FromMinutes(minutes: 2),
      _cachesContext.TimeProvider,
      XUnitLogger.CreateLogger<TestCacheInvalidation>(_cachesContext.XUnitLogger));

  [Given("minimal expired entries purging interval is {int} minutes")]
  public void GivenMinimalExpiredEntriesPurgingIntervalIsIntMinutes(int minutes) =>
    _minimalExpiredEntriesPurgingInterval = TimeSpan.FromMinutes(minutes);

  [Given("time-based cache invalidation with purging interval of {int} minutes")]
  public void GivenTimeBasedCacheInvalidationWithPurgingIntervalOfIntMinutes(int minutes) =>
    _sut = new TestCacheInvalidation(
      TimeSpan.FromMinutes(minutes),
      TimeSpan.FromMinutes(minutes: 1),
      TimeSpan.FromMinutes(minutes: 2),
      _cachesContext.TimeProvider,
      XUnitLogger.CreateLogger<TestCacheInvalidation>(_cachesContext.XUnitLogger));

  [Given("absolute expiration today at (.*)")]
  public void GivenAbsoluteExpirationTodayAt(TimeSpan absoluteExpirationTime) =>
    _absoluteExpiration = _cachesContext.Today.Add(absoluteExpirationTime);

  [Given("sliding expiration in {int} minutes")]
  public void GivenSlidingExpirationInIntMinutes(int minutes) => _slidingExpiration = TimeSpan.FromMinutes(minutes);

  [Given("time passed by {double} minutes")]
  public void GivenTimePassedByDoubleMinutes(double minutes) =>
    _cachesContext.TimeProvider.Advance(TimeSpan.FromMinutes(minutes));

  [Given("no absolute expiration")] public void GivenNoAbsoluteExpiration() => _absoluteExpiration = null;

  [Given("no sliding expiration")] public void GivenNoSlidingExpiration() => _slidingExpiration = null;

  [Given("cache entry that expires today at (.*)")]
  public void GivenCacheEntryThatExpiresTodayAt(TimeSpan expiresAt) => _expiresAt = _cachesContext.Today.Add(expiresAt);

  [When("I construct time-based cache invalidation with purging interval of (.*) minutes")]
  public void WhenIConstructTimeBasedCacheInvalidationWithPurgingIntervalOfMinutes(int purgingIntervalMinutes) {
    var purgingInterval = TimeSpan.FromMinutes(purgingIntervalMinutes);
    try {
      _sut = new TestCacheInvalidation(
        purgingInterval,
        TimeSpan.FromMinutes(minutes: 2),
        _minimalExpiredEntriesPurgingInterval,
        _cachesContext.TimeProvider,
        XUnitLogger.CreateLogger<TestCacheInvalidation>(_cachesContext.XUnitLogger));
    }
    catch (Exception exception) {
      _errorHandlingContext.LastException = exception;
    }
  }

  [When("purging expired cache entries requested")]
  public async Task WhenPurgingExpiredCacheEntriesRequested() {
    try {
      await _sut.PurgeEntriesIfRequired();
    }
    catch (Exception exception) {
      _errorHandlingContext.LastException = exception;
    }
  }

  [When("a few concurrent purging expired cache entries requested")]
  public async Task WhenAFewConcurrentPurgingExpiredCacheEntriesRequested() {
    try {
      await Task.WhenAll(
        _sut.PurgeEntriesIfRequired(),
        _sut.PurgeEntriesIfRequired(),
        _sut.PurgeEntriesIfRequired(),
        _sut.PurgeEntriesIfRequired(),
        _sut.PurgeEntriesIfRequired());
    }
    catch (Exception exception) {
      _errorHandlingContext.LastException = exception;
    }
  }

  [Then("purging should be done")] public void ThenPurgingShouldBeDone() => _sut.IsPurgeStarted.Should().BeTrue();

  [Then("purging should not start")] public void ThenPurgingShouldNotStart() => _sut.IsPurgeStarted.Should().BeFalse();

  [Then("only one purging should be done")]
  public void ThenOnlyOnePurgingShouldBeDone() {
    _sut.IsPurgeStarted.Should().BeTrue();
    _sut.NumberOfPurgeStarted.Should().Be(expected: 1);
  }

  [When("I check is cache entry expired")]
  public void WhenICheckIsCacheEntryExpired() => _isExpired = _sut.IsCacheEntryExpired(_expiresAt);

  [Then("it should be not expired")] public void ThenItShouldBeNotExpired() => _isExpired.Should().BeFalse();

  [Then("it should be expired")] public void ThenItShouldBeExpired() => _isExpired.Should().BeTrue();

  [When("I calculate expiration time")]
  public void WhenICalculateExpirationTime() => _calculatedExpiration = _sut.CalculateExpiration(_absoluteExpiration, _slidingExpiration);

  [Then("it should be today at (.*)")]
  public void ThenItShouldBeTodayAt(TimeSpan expirationTime) => _calculatedExpiration.Should().Be(_cachesContext.Today.Add(expirationTime));

  private readonly CachesContext _cachesContext;
  private readonly ErrorHandlingContext _errorHandlingContext;
  private DateTimeOffset? _absoluteExpiration;
  private DateTimeOffset _calculatedExpiration;
  private DateTimeOffset _expiresAt;
  private bool _isExpired;
  private TimeSpan _minimalExpiredEntriesPurgingInterval;
  private TimeSpan? _slidingExpiration;
  private TestCacheInvalidation _sut = null!;

  private sealed class TestCacheInvalidation : TimeBasedCacheInvalidation {
    public TestCacheInvalidation(
      TimeSpan expiredEntriesPurgingInterval,
      TimeSpan defaultSlidingExpirationInterval,
      TimeSpan minimalExpiredEntriesPurgingInterval,
      TimeProvider timeProvider,
      ILogger logger)
      : base(
        new TimeBasedCacheInvalidationSettings {
          ExpiredEntriesPurgingInterval = expiredEntriesPurgingInterval, DefaultSlidingExpirationInterval = defaultSlidingExpirationInterval
        },
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
