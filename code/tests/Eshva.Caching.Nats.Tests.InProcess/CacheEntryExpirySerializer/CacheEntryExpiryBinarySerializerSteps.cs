using System.Buffers;
using Eshva.Caching.Abstractions;
using FluentAssertions;
using Reqnroll;

namespace Eshva.Caching.Nats.Tests.InProcess.CacheEntryExpirySerializer;

[Binding]
public class CacheEntryExpiryBinarySerializerSteps {
  [Given("cache entry expiry binary serializer")]
  public void GivenCacheEntryExpiryBinarySerializer() => _sut = new CacheEntryExpiryBinarySerializer();

  [Given("cache entry expiry with '(.*)', '(.*)', '(.*)'")]
  public void GivenCacheEntryExpiryWith(
    DateTimeOffset expiresAtUtc,
    DateTimeOffset? absoluteExpiryAtUtc,
    TimeSpan? slidingExpiryInterval) =>
    _cacheEntryExpiry = new CacheEntryExpiry(expiresAtUtc, absoluteExpiryAtUtc, slidingExpiryInterval);

  [When("I serialize cache entry expiry with binary serializer")]
  public void WhenISerializeCacheEntryExpiryWithBinarySerializer() {
    const int serializedSize = sizeof(long) * 3;
    var writer = new ArrayBufferWriter<byte>(serializedSize);
    _sut.Serialize(writer, _cacheEntryExpiry);
    _serialized = writer.WrittenSpan.ToArray();
  }

  [Then("deserialized cache entry expiry should have '(.*)', '(.*)', '(.*)'")]
  public void ThenDeserializedCacheEntryExpiryShouldHave(
    DateTimeOffset expiresAtUtc,
    DateTimeOffset? absoluteExpiryAtUtc,
    TimeSpan? slidingExpiryInterval) {
    var deserialize = _sut.Deserialize(new ReadOnlySequence<byte>(_serialized));
    deserialize.Should().BeEquivalentTo(_cacheEntryExpiry, options => options.ComparingRecordsByValue());
  }

  private CacheEntryExpiryBinarySerializer _sut = null!;
  private CacheEntryExpiry _cacheEntryExpiry;
  private byte[] _serialized = [];
}
