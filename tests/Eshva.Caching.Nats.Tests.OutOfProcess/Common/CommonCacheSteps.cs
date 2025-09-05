using System;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using NATS.Client.ObjectStore;
using Reqnroll;
using Xunit;

namespace Eshva.Caching.Nats.Tests.OutOfProcess.Common;

[Binding]
public class CommonCacheSteps {
  public CommonCacheSteps(CachesContext cachesContext) {
    _cachesContext = cachesContext;
  }

  [Given("entry with key {string} and value {string} which expires in {double} minutes put into cache")]
  public async Task GivenEntryWithKeyStringAndValueStringWhichExpiresInMinutesPutIntoCache(
    string key,
    string value,
    double expiresInMinutes) {
    await _cachesContext.Bucket.PutAsync(key, Encoding.UTF8.GetBytes(value));

    var objectMetadata = await _cachesContext.Bucket.GetInfoAsync(key);
    objectMetadata.Metadata = new CacheEntryMetadata(objectMetadata.Metadata!) {
      ExpiresOnUtc = DateTimeOffset.UtcNow.AddMinutes(expiresInMinutes)
    };
    await _cachesContext.Bucket.UpdateMetaAsync(key, objectMetadata);
  }

  [Then("I should get value {string} as the requested entry")]
  public void ThenIShouldGetValueAsTheRequestedEntry(string value) =>
    Encoding.UTF8.GetBytes(value).Should().BeEquivalentTo(_cachesContext.GottenCacheEntryValue);

  [Then("I should get a null value as the requested entry")]
  public void ThenIShouldGetANullValueAsTheRequestedEntry() => _cachesContext.GottenCacheEntryValue.Should().BeNull();

  [Given("{double} minutes passed")]
  public void GivenMinutesPassed(double minutes) =>
    _cachesContext.Clock.AdjustTime(DateTimeOffset.UtcNow.AddMinutes(minutes));

  [Then("{string} entry is not present in the object-store bucket")]
  public async Task ThenEntryIsNotPresentInTheObjectStoreBucket(string key) {
    try {
      await _cachesContext.Bucket.GetInfoAsync(key);
    }
    catch (NatsObjNotFoundException) {
      // Expected exception if object already deleted from the bucket.
      return;
    }

    Assert.Fail();
  }

  [Given("passed a bit more than purging expired entries interval")]
  public void GivenPassedABitMoreThanPurgingExpiredEntriesInterval() =>
    _cachesContext.Clock.AdjustTime(
      DateTimeOffset.UtcNow.Add(_cachesContext.Settings.ExpiredEntriesPurgingInterval).AddSeconds(seconds: 1));

  [Given("passed a bit less than purging expired entries interval")]
  public void GivenPassedABitLessThanPurgingExpiredEntriesInterval() => _cachesContext.Clock.AdjustTime(
    DateTimeOffset.UtcNow.Add(_cachesContext.Settings.ExpiredEntriesPurgingInterval).AddSeconds(seconds: -1));

  private readonly CachesContext _cachesContext;
}
