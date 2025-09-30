using Eshva.Caching.Abstractions;
using Eshva.Caching.Nats.Tests.OutOfProcess.Common;
using FluentAssertions;
using Reqnroll;

namespace Eshva.Caching.Nats.Tests.OutOfProcess.Features.EntryExpirationStrategy;

[Binding]
public class StandardCacheEntryExpirationStrategySteps {
  public StandardCacheEntryExpirationStrategySteps(CachesContext cachesContext) {
    _cachesContext = cachesContext;
  }

  [Given("standard cache entry expiration strategy with clock set at today (.*) and default sliding expiration time (.*) minutes")]
  public void GivenStandardCacheEntryExpirationStrategyWithClockSetAtTodayAndDefaultSlidingExpirationTimeMinutes(
    TimeSpan currentTime,
    double defaultSlidingExpirationTime) {
    _cachesContext.TimeProvider.AdjustTime(_cachesContext.Today + currentTime);
    _sut = new StandardCacheEntryExpirationStrategy(
      new ExpirationStrategySettings { DefaultSlidingExpirationInterval = TimeSpan.FromMinutes(defaultSlidingExpirationTime) },
      _cachesContext.TimeProvider);
  }

  [Given("cache entry that expires today at (.*)")]
  public void GivenCacheEntryThatExpiresAtToday(TimeSpan expiresAt) => _expiresAt = _cachesContext.Today.Add(expiresAt);

  [When("I check is cache entry expired")]
  public void WhenICheckIsCacheEntryExpired() => _isExpired = _sut.IsCacheEntryExpired(_expiresAt);

  [Then("it should be not expired")] public void ThenItShouldBeNotExpired() => _isExpired.Should().BeFalse();

  [Then("it should be expired")] public void ThenItShouldBeExpired() => _isExpired.Should().BeTrue();

  [Given("absolute expiration today at (.*)")]
  public void GivenAbsoluteExpirationTodayAt(TimeSpan absoluteExpirationTime) =>
    _absoluteExpiration = _cachesContext.Today.Add(absoluteExpirationTime);

  [Given("sliding expiration in {int} minutes")]
  public void GivenSlidingExpirationInMinutes(int minutes) => _slidingExpiration = TimeSpan.FromMinutes(minutes);

  [Given("no absolute expiration")] public void GivenNoAbsoluteExpiration() => _absoluteExpiration = null;

  [Given("no sliding expiration")] public void GivenNoSlidingExpiration() => _slidingExpiration = null;

  [When("I calculate expiration time")]
  public void WhenICalculateExpirationTime() => _calculatedExpiration = _sut.CalculateExpiration(_absoluteExpiration, _slidingExpiration);

  [Then("it should be today at (.*)")]
  public void ThenItShouldBeTodayAt(TimeSpan expirationTime) => _calculatedExpiration.Should().Be(_cachesContext.Today.Add(expirationTime));

  [Given("time passed by {double} minutes")]
  public void GivenTimePassedByMinutes(double minutes) =>
    _cachesContext.TimeProvider.Advance(TimeSpan.FromMinutes(minutes));

  private readonly CachesContext _cachesContext;
  private DateTimeOffset? _absoluteExpiration;
  private DateTimeOffset _calculatedExpiration;
  private DateTimeOffset _expiresAt;
  private bool _isExpired;
  private TimeSpan? _slidingExpiration;
  private StandardCacheEntryExpirationStrategy _sut = null!;
}
