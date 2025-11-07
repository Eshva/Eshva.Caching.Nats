using System.Text;
using Eshva.Caching.Nats.Tests.Tools;
using FluentAssertions;
using NATS.Client.ObjectStore;
using NATS.Client.ObjectStore.Models;
using Reqnroll;
using Xunit;

namespace Eshva.Caching.Nats.Tests.OutOfProcess.Common;

[Binding]
public class CommonCacheSteps {
  public CommonCacheSteps(CachesContext cachesContext) {
    _cachesContext = cachesContext;
  }

  [Given("entry with key {string} and value {string} which expires in {double} minutes put into cache")]
  public async Task GivenEntryWithKeyStringAndValueStringWhichExpiresInDoubleMinutesPutIntoCache(
    string key,
    string value,
    double expiresInMinutes) {
    var metadataAccessor = new ObjectMetadataAccessor(new ObjectMetadata { Name = key }) {
      SlidingExpiryInterval = TimeSpan.FromMinutes(expiresInMinutes),
      ExpiresAtUtc = _cachesContext.TimeProvider.GetUtcNow().AddMinutes(expiresInMinutes)
    };

    await _cachesContext.Bucket.PutAsync(metadataAccessor.ObjectMetadata, new MemoryStream(Encoding.UTF8.GetBytes(value)));

    _cachesContext.XUnitLogger.WriteLine($"Put entry '{key}' that expires at {metadataAccessor.ExpiresAtUtc}");
  }

  [Given("entry with key {string} and random byte array as value which expires in {double} minutes put into cache")]
  public async Task GivenEntryWithKeyStringAndRandomByteArrayAsValueWhichExpiresInDoubleMinutesPutIntoCache(
    string key,
    double expiresInMinutes) {
    _originalValue = new byte[1 * 1024 * 1024 + 5];
    Random.Shared.NextBytes(_originalValue);
    await _cachesContext.Bucket.PutAsync(key, _originalValue);

    var objectMetadata = await _cachesContext.Bucket.GetInfoAsync(key);
    var metadataAccessor = new ObjectMetadataAccessor(objectMetadata) {
      ExpiresAtUtc = _cachesContext.TimeProvider.GetUtcNow().AddMinutes(expiresInMinutes),
      SlidingExpiryInterval = TimeSpan.FromMinutes(expiresInMinutes)
    };

    _cachesContext.XUnitLogger.WriteLine($"Put entry '{key}' that expires at {metadataAccessor.ExpiresAtUtc}");
    await _cachesContext.Bucket.UpdateMetaAsync(key, objectMetadata);
  }

  [Then("I should get value {string} as the requested entry")]
  public void ThenIShouldGetValueStringAsTheRequestedEntry(string value) =>
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

  [Then("{string} entry is not present in the object-store bucket")]
  public async Task ThenStringEntryIsNotPresentInTheObjectStoreBucket(string key) {
    try {
      await _cachesContext.Bucket.GetInfoAsync(key);
    }
    catch (NatsObjNotFoundException) {
      // Expected exception if object already deleted from the bucket.
      return;
    }

    Assert.Fail($"'{key}' entry is still present in the cache");
  }

  [Then("{string} entry is present in the object-store bucket")]
  public async Task ThenStringEntryIsPresentInTheObjectStoreBucket(string key) {
    var objectMetadata = await _cachesContext.Bucket.GetInfoAsync(key);
    objectMetadata.Should().NotBeNull();
  }

  [Given("expired entries purging interval {int} minutes")]
  public void GivenExpiredEntriesPurgingIntervalIntMinutes(int minutes) =>
    _cachesContext.ExpiredEntriesPurgingInterval = TimeSpan.FromMinutes(minutes);

  [Given("default sliding expiration interval {int} minutes")]
  public void GivenDefaultSlidingExpirationIntervalIntMinutes(int minutes) =>
    _cachesContext.DefaultSlidingExpirationInterval = TimeSpan.FromMinutes(minutes);

  [Given("object-store based cache with synchronous purge")]
  public void GivenObjectStoreBasedCacheWithSynchronousPurge() =>
    _cachesContext.CreateAndAssignCacheServices();

  [Given("clock set at today (.*)")]
  public void GivenClockSetAtToday(TimeSpan timeOfDay) =>
    _cachesContext.TimeProvider.AdjustTime(_cachesContext.Today + timeOfDay);

  [Given("time passed by {double} minutes")]
  public void GivenTimePassedByDoubleMinutes(double minutes) =>
    _cachesContext.TimeProvider.Advance(TimeSpan.FromMinutes(minutes));

  [Then("'(.*)' entry should be expired today at (.*)")]
  public async Task ThenEntryShouldBeExpiredTodayAt(string key, TimeSpan timeOfDay) {
    var metadataAccessor = new ObjectMetadataAccessor(await _cachesContext.Bucket.GetInfoAsync(key));
    metadataAccessor.ExpiresAtUtc.Should().Be(_cachesContext.Today.Add(timeOfDay));
  }

  [Given("object with key {string} removed from object-store bucket")]
  public async Task GivenObjectWithKeyStringRemovedFromObjectStoreBucket(string key) =>
    await _cachesContext.Bucket.DeleteAsync(key);

  [Given("metadata of cache entry with key {string} corrupted")]
  public async Task GivenMetadataOfCacheEntryWithKeyStringCorrupted(string key) {
    var metadataCorrupter = new ObjectEntryMetadataCorrupter();
    await metadataCorrupter.CorruptEntryMetadata(_cachesContext.Bucket, key);
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
