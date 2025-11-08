using System.Text;
using Eshva.Caching.Abstractions;
using Eshva.Caching.Nats.Tests.Tools;
using FluentAssertions;
using Reqnroll;

namespace Eshva.Caching.Nats.Tests.OutOfProcess.Common;

[Binding]
public class CommonCacheSteps {
  public CommonCacheSteps(CachesContext cachesContext) {
    _cachesContext = cachesContext;
  }

  [Given("entry with key '(.*)' and value '(.*)' which expires in (.*) minutes put into cache")]
  public async Task GivenEntryWithKeyAndValueWhichExpiresInMinutesPutIntoCache(
    string key,
    string value,
    double expiresInMinutes) =>
    await _cachesContext.Driver.PutEntry(
        key,
        Encoding.UTF8.GetBytes(value),
        new CacheEntryExpiry(
          _cachesContext.TimeProvider.GetUtcNow().AddMinutes(expiresInMinutes),
          AbsoluteExpiryAtUtc: null,
          TimeSpan.FromMinutes(expiresInMinutes)))
      .ConfigureAwait(continueOnCapturedContext: false);

  [Given("entry with key '(.*)' and random byte array as value which expires in (.*) minutes put into cache")]
  public async Task GivenEntryWithKeyAndRandomByteArrayAsValueWhichExpiresInMinutesPutIntoCache(
    string key,
    double expiresInMinutes) {
    _originalValue = new byte[1 * 1000 * 1024 + 5];
    Random.Shared.NextBytes(_originalValue);
    await _cachesContext.Driver.PutEntry(
        key,
        _originalValue,
        new CacheEntryExpiry(
          _cachesContext.TimeProvider.GetUtcNow().AddMinutes(expiresInMinutes),
          AbsoluteExpiryAtUtc: null,
          TimeSpan.FromMinutes(expiresInMinutes)))
      .ConfigureAwait(continueOnCapturedContext: false);
  }

  [Then("I should get value '(.*)' as the requested entry")]
  public void ThenIShouldGetValueAsTheRequestedEntry(string value) =>
    Encoding.UTF8.GetBytes(value).Should().BeEquivalentTo(_cachesContext.GottenCacheEntryValue);

  [Then("I should get same value as the requested entry")]
  public void ThenIShouldGetSameValueAsTheRequestedEntry() =>
    _cachesContext.GottenCacheEntryValue.Should().BeEquivalentTo(_originalValue);

  [Then("I should get a null value as the requested entry")]
  public void ThenIShouldGetANullValueAsTheRequestedEntry() =>
    _cachesContext.GottenCacheEntryValue.Should().BeNull();

  [Given("{double} minutes passed")]
  public void GivenDoubleMinutesPassed(double minutes) =>
    _cachesContext.TimeProvider.Advance(TimeSpan.FromMinutes(minutes));

  [Then("'(.*)' entry is not present in the object store bucket")]
  public async Task ThenEntryIsNotPresentInTheObjectStoreBucket(string key) {
    var doesExist = await _cachesContext.Driver.DoesExist(key).ConfigureAwait(continueOnCapturedContext: false);
    doesExist.Should().BeFalse();
  }

  [Then("'(.*)' entry is present in the object store bucket")]
  public async Task ThenEntryIsPresentInTheObjectStoreBucket(string key) {
    var doesExist = await _cachesContext.Driver.DoesExist(key).ConfigureAwait(continueOnCapturedContext: false);
    doesExist.Should().BeTrue();
  }

  [Given("expired entries purging interval (.*) minutes")]
  public void GivenExpiredEntriesPurgingIntervalMinutes(int minutes) =>
    _cachesContext.ExpiredEntriesPurgingInterval = TimeSpan.FromMinutes(minutes);

  [Given("default sliding expiration interval (.*) minutes")]
  public void GivenDefaultSlidingExpirationIntervalMinutes(int minutes) =>
    _cachesContext.DefaultSlidingExpirationInterval = TimeSpan.FromMinutes(minutes);

  [Given("object store based cache")]
  public void GivenObjectStoreBasedCache() =>
    _cachesContext.CreateObjectStoreDriver();

  [Given("clock set at today (.*)")]
  public void GivenClockSetAtToday(TimeSpan timeOfDay) =>
    _cachesContext.TimeProvider.AdjustTime(_cachesContext.Today + timeOfDay);

  [Given("time passed by (.*) minutes")]
  public void GivenTimePassedByMinutes(double minutes) =>
    _cachesContext.TimeProvider.Advance(TimeSpan.FromMinutes(minutes));

  [Then("'(.*)' entry should be expired today at (.*)")]
  public async Task ThenEntryShouldBeExpiredTodayAt(string key, TimeSpan timeOfDay) {
    var entryExpiry = await _cachesContext.Driver.GetMetadata(key).ConfigureAwait(continueOnCapturedContext: false);
    entryExpiry.ExpiresAtUtc.Should().Be(_cachesContext.Today.Add(timeOfDay));
  }

  [Given("object with key '(.*)' removed from object store bucket")]
  public async Task GivenObjectWithKeyRemovedFromObjectStoreBucket(string key) =>
    await _cachesContext.Driver.Remove(key).ConfigureAwait(continueOnCapturedContext: false);

  [Given("metadata of cache entry with key '(.*)' corrupted")]
  public async Task GivenMetadataOfCacheEntryWithKeyCorrupted(string key) {
    var metadataCorrupter = new ObjectEntryMetadataCorrupter();
    await metadataCorrupter.CorruptEntryMetadata(_cachesContext.Bucket, key).ConfigureAwait(continueOnCapturedContext: false);
  }

  [Then("cache invalidation done")]
  public void ThenCacheInvalidationDone() =>
    _cachesContext.PurgingSignal.Wait(TimeSpan.FromSeconds(seconds: 10)).Should().BeTrue();

  [Then("cache invalidation not started")]
  public void ThenCacheInvalidationNotStarted() =>
    _cachesContext.PurgingSignal.Wait(TimeSpan.FromSeconds(seconds: 10)).Should().BeFalse();

  private readonly CachesContext _cachesContext;
  private byte[] _originalValue = [];
}
