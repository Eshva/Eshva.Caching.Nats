using Eshva.Caching.Abstractions.Distributed;
using Eshva.Caching.Nats.Distributed;
using Eshva.Testing.Reqnroll.Contexts;
using Moq;
using NATS.Client.Core;
using NATS.Client.KeyValueStore;
using NATS.Client.ObjectStore;
using Reqnroll;

namespace Eshva.Caching.Nats.Tests.InProcess.Datastores;

[Binding]
internal sealed class DatastoreConstructionSteps {
  public DatastoreConstructionSteps(ErrorHandlingContext errorHandlingContext) {
    _errorHandlingContext = errorHandlingContext ?? throw new ArgumentNullException(nameof(errorHandlingContext));
  }

  [Given("key-value bucket")]
  public void GivenKeyValueBucket() =>
    _keyValueBucket = Mock.Of<INatsKVStore>();

  [Given("object store bucket")]
  public void GivenObjectStoreBucket() =>
    _objectStoreBucket = Mock.Of<INatsObjStore>();

  [Given("cache entry expiry serializer")]
  public void GivenCacheEntryExpirySerializer() =>
    _serializer = Mock.Of<INatsSerializer<CacheEntryExpiry>>();

  [Given("expiry calculator")]
  public void GivenExpiryCalculator() =>
    _expiryCalculator = new CacheEntryExpiryCalculator(TimeSpan.MaxValue, TimeProvider.System);

  [When("I construct key-value based datastore with given arguments")]
  public void WhenIConstructKeyValueBasedDatastoreWithGivenArguments() {
    try {
      // ReSharper disable once ObjectCreationAsStatement
      new KeyValueBasedDatastore(_keyValueBucket, _serializer, _expiryCalculator);
    }
    catch (Exception exception) {
      _errorHandlingContext.LastException = exception;
    }
  }

  [When("I construct object store based datastore with given arguments")]
  public void WhenIConstructObjectStoreBasedDatastoreWithGivenArguments() {
    try {
      // ReSharper disable once ObjectCreationAsStatement
      new ObjectStoreBasedDatastore(_objectStoreBucket, _expiryCalculator);
    }
    catch (Exception exception) {
      _errorHandlingContext.LastException = exception;
    }
  }

  private readonly ErrorHandlingContext _errorHandlingContext;
  private INatsKVStore _keyValueBucket = null!;
  private INatsSerializer<CacheEntryExpiry> _serializer = null!;
  private CacheEntryExpiryCalculator _expiryCalculator = null!;
  private INatsObjStore _objectStoreBucket = null!;
}
