using Eshva.Caching.Nats.Distributed;
using Eshva.Testing.Reqnroll.Contexts;
using FluentAssertions;
using NATS.Client.ObjectStore.Models;
using Reqnroll;

namespace Eshva.Caching.Nats.Tests.InProcess.MetadataAccessor;

[Binding]
public class ObjectMetadataAccessorSteps {
  public ObjectMetadataAccessorSteps(ErrorHandlingContext errorHandlingContext) {
    _errorHandlingContext = errorHandlingContext;
  }

  [Given("object metadata with key '(.*)' and without metadata dictionary")]
  public void GivenObjectMetadataWithKeyAndWithoutMetadataDictionary(string key) =>
    _objectMetadata = new ObjectMetadata { Name = key, Metadata = null };

  [Given("object metadata with key '(.*)' and with metadata dictionary")]
  public void GivenObjectMetadataWithKeyAndWithMetadataDictionary(string key) {
    _metadataDictionary = new Dictionary<string, string>();
    _objectMetadata = new ObjectMetadata { Name = key, Metadata = _metadataDictionary };
  }

  [Given("object metadata not specified")]
  public void GivenObjectMetadataNotSpecified() => _objectMetadata = null!;

  [Given("object metadata accessor with defined arguments")]
  public void GivenObjectMetadataAccessorWithDefinedArguments() =>
    CreateAccessor();

  [When("I construct object metadata accessor with defined arguments")]
  public void WhenIConstructObjectMetadataAccessorWithDefinedArguments() =>
    CreateAccessor();

  [When("I set expires at UTC of accessor to '(.*)'")]
  public void WhenISetExpiresAtUtcOfAccessorTo(DateTimeOffset expiresAtUtc) =>
    _sut.ExpiresAtUtc = expiresAtUtc;

  [When("I set absolute expiry at UTC of accessor to '(.*)'")]
  public void WhenISetAbsoluteExpiryAtUtcOfAccessorTo(DateTimeOffset? absoluteExpiryAtUtc) =>
    _sut.AbsoluteExpiryAtUtc = absoluteExpiryAtUtc;

  [When("I set sliding expiry interval of accessor to '(.*)'")]
  public void WhenISetSlidingExpiryIntervalOfAccessorTo(TimeSpan? slidingExpiryInterval) =>
    _sut.SlidingExpiryInterval = slidingExpiryInterval;

  [Then("object metadata of accessor equals one used in constructor")]
  public void ThenObjectMetadataOfAccessorEqualsOneUsedInConstructor() =>
    _sut.ObjectMetadata.Should().BeSameAs(_objectMetadata);

  [Then("object metadata's metadata dictionary assigned")]
  public void ThenObjectMetadatasMetadataDictionaryAssigned() =>
    _objectMetadata.Should().NotBeNull();

  [Then("object metadata's metadata dictionary equals used in object metadata")]
  public void ThenObjectMetadatasMetadataDictionaryEqualsUsedInObjectMetadata() =>
    _objectMetadata.Metadata.Should().BeSameAs(_metadataDictionary);

  [Then("metadata dictionary '(.*)' entry should be set to '(.*)'")]
  public void ThenMetadataDictionaryEntryShouldBeSetTo(string key, string value) =>
    _objectMetadata.Metadata![key].Should().Be(value);

  [Given("metadata dictionary '(.*)' entry set to '(.*)'")]
  public void GivenMetadataDictionaryEntrySetTo(string key, string value) =>
    _objectMetadata.Metadata![key] = !value.Equals("null") ? value : null!;

  [When("I get expires at UTC of accessor")]
  public void WhenIGetExpiresAtUtcOfAccessor() =>
    _expiresAtUtc = _sut.ExpiresAtUtc;

  [Then("gotten expires at UTC should be set to '(.*)'")]
  public void ThenGottenExpiresAtUtcShouldBeSetTo(DateTimeOffset expiresAtUtc) =>
    _expiresAtUtc.Should().Be(expiresAtUtc);

  [Given("metadata dictionary '(.*)' entry missing")]
  public void GivenMetadataDictionaryEntryMissing(string key) =>
    _objectMetadata.Metadata!.Remove(key);

  [Then("metadata dictionary should not contain '(.*)' entry")]
  public void ThenMetadataDictionaryShouldNotContainEntry(string key) =>
    _sut.AbsoluteExpiryAtUtc.Should().BeNull();

  [When("I get absolute expiry at UTC of accessor")]
  public void WhenIGetAbsoluteExpiryAtUtcOfAccessor() =>
    _absoluteExpiryAtUtc = _sut.AbsoluteExpiryAtUtc;

  [When("I get sliding expiry interval of accessor")]
  public void WhenIGetSlidingExpiryIntervalOfAccessor() =>
    _slidingExpiryInterval = _sut.SlidingExpiryInterval;

  [Then("gotten absolute expiry at UTC should be set to '(.*)'")]
  public void ThenGottenAbsoluteExpiryAtUtcShouldBeSetTo(DateTimeOffset? absoluteExpiryAtUtc) =>
    _absoluteExpiryAtUtc.Should().Be(absoluteExpiryAtUtc);

  [Then("gotten sliding expiry interval should be set to '(.*)'")]
  public void ThenGottenSlidingExpiryIntervalShouldBeSetTo(TimeSpan? slidingExpiryInterval) =>
    _slidingExpiryInterval.Should().Be(slidingExpiryInterval);

  private void CreateAccessor() {
    try {
      _sut = new ObjectMetadataAccessor(_objectMetadata);
    }
    catch (Exception exception) {
      _errorHandlingContext.LastException = exception;
    }
  }

  private readonly ErrorHandlingContext _errorHandlingContext;
  private ObjectMetadata _objectMetadata = null!;
  private ObjectMetadataAccessor _sut = null!;
  private Dictionary<string, string>? _metadataDictionary;
  private DateTimeOffset _expiresAtUtc;
  private DateTimeOffset? _absoluteExpiryAtUtc;
  private TimeSpan? _slidingExpiryInterval;
}
