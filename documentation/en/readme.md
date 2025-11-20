# Eshva.Caching.Nats

NATS is a wonderful platform for inter-application communication. In the beginning it was just a message bus, but the authors are constantly expanding its functionality. At the moment NATS already includes key/value and object stores built on the same messaging infrastructure. These stores can be used to build a distributed cache.

This package provides two implementations of the [IDistributedCache](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.caching.distributed.idistributedcache?view=net-9.0-pp) contract (and its development [IBufferDistributedCache](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.caching.distributed.ibufferdistributedcache?view=net-9.0-pp)):

- `NatsKeyValueStoreBasedCache` — an implementation based on the NATS key/value store.
- `NatsObjectStoreBasedCache` — an implementation based on NATS object store.

## Installation

As usual, either add the `Eshva.Caching.Nats` package in your favorite development environment, or using the command line:

```bash
dotnet add package Eshva.Caching.Nats
```

## Usage

It is common for an application to use multiple caches at once, one for each type of object being cached. This is important because the lifetime of objects, data stores, object sizes, etc. vary for different data. Therefore, it is important to be able to add multiple caches with different settings to the DI container of the application. I chose to use so-called keyed services when registering to a DI container. The following extension methods are available to you:

```csharp
// Add a cache based on the NATS object store. The NATS client will be retrieved
// from the DI container by the natsServerKey key, the cache settings will be
// retrieved by the serviceKey key. The cache itself will be registered with the
// key serviceKey.
public static IServiceCollection AddKeyedNatsObjectStoreBasedCache(
    this IServiceCollection services,
    string serviceKey,
    string natsServerKey)

// Add a NATS object store based cache. The NATS client will be retrieved
// from the DI container without using a key, the cache settings will be
// retrieved by the key serviceKey. The cache itself will be registered
// by the key serviceKey.
public static IServiceCollection AddKeyedNatsObjectStoreBasedCache(
		this IServiceCollection services,
		string serviceKey)

// Add a cache based on the NATS object store. The NATS client will be obtained
// from the DI-container by the key natsServerKey, the cache settings will be
// obtained without the key. The cache itself will be registered without using
// the key.
public static IServiceCollection AddNatsObjectStoreBasedCache(
		this IServiceCollection services,
		string natsServerKey)

// Add a cache based on the NATS object store. The NATS client will be obtained
// from the DI container without a key, the cache settings will be obtained
// without a key. The cache itself will be registered without using a key.
public static IServiceCollection AddNatsObjectStoreBasedCache(
		this IServiceCollection services)

// Add a NATS key/value based cache. The NATS client will be fetched
// from the DI container by the natsServerKey key, the cache settings will be
// fetched by the serviceKey key. The cache itself will be registered with the
// key serviceKey.
public static IServiceCollection AddKeyedNatsKeyValueBasedCache(
    this IServiceCollection services,
    string serviceKey,
    string natsServerKey)

// Add a cache based on the NATS key/value store. The NATS client will be retrieved
// from the DI container without using a key, the cache settings will be retrieved
// by the key serviceKey. The cache itself will be registered by the serviceKey key.
public static IServiceCollection AddKeyedNatsKeyValueBasedCache(
    this IServiceCollection services,
    string serviceKey)

// Add a cache based on the NATS key/value store. The NATS client will be retrieved
// from the DI container by the key natsServerKey, the cache settings will be
// retrieved without the key. The cache itself will be registered without using the key.
public static IServiceCollection AddNatsKeyValueBasedCache(
		this IServiceCollection services,
		string natsServerKey)

// Add a NATS key/value based cache. The NATS client will be fetched from the
// DI container by the key natsServerKey, the cache settings will be fetched by
// the key serviceKey. The cache itself will be registered with the key serviceKey.
public static IServiceCollection AddNatsKeyValueBasedCache(
		this IServiceCollection services)
```

For both cache types, a NATS `INatsConnection` client must be registered in the DI container.

For an object store-based cache in the DI container, an instance of settings type `ObjectStoreBasedCacheSettings` must be registered.

For a cache based on a key/value store in a DI container, an instance of settings type `KeyValueBasedCacheSettings` must be registered.

Since the cache is registered as a singleton, all services on which it depends must also be registered as a singletons. Depending on your needs, use one of the four extension methods. The methods that accept `serviceKey` are designed for the case where you have multiple caches of the same type. Methods that accept `natsServerKey` are designed for the case where your application uses multiple NATS instances and therefore clients to connect to them (typically there is only one client for a single NATS instance in an application).

[An example of registration](https://github.com/Eshva/Eshva.Caching.Nats/blob/master/code/benchmarks/Eshva.Caching.Nats.TestWebApp/Bootstrapping/ApplicationBootstrapping.cs) can be seen in the ASP.NET application located in this repository. It looks something like the following:

```csharp
private static void AddSharedServices(this IServiceCollection services) {
    services.AddSingleton(TimeProvider.System);
    services.AddKeyedSingleton<INatsConnection>(
      CacheNatsServerKey,
      (diContainer, key) => {
        var settings = diContainer.GetRequiredKeyedService<NatsServerSettings>(CacheNatsServerKey);
        var connectionName = settings.NatsConnectionName;
        return new NatsConnection(
          new NatsOpts {
            Url = settings.NatsServerConnectionString,
            Name = !string.IsNullOrWhiteSpace(connectionName) ? connectionName : $"Connection for {key}"
          });
      });
  }

  public static void AddConfiguration(IServiceCollection services) {
    services.AddOptions<ObjectStoreBasedCacheSettings>()
      .BindConfiguration(ObjectStoreCacheConfigurationSectionPath)
      .ValidateDataAnnotations()
      .ValidateOnStart();
    services.AddKeyedSingleton<ObjectStoreBasedCacheSettings>(
      ObjectStoreCacheKey,
      (diContainer, _) => diContainer.GetRequiredService<IOptions<ObjectStoreBasedCacheSettings>>().Value);
    services.AddOptions<KeyValueBasedCacheSettings>()
      .BindConfiguration(KeyValueCacheConfigurationSectionPath)
      .ValidateDataAnnotations()
      .ValidateOnStart();
    services.AddKeyedSingleton<KeyValueBasedCacheSettings>(
      KeyValueCacheKey,
      (diContainer, _) => diContainer.GetRequiredService<IOptions<KeyValueBasedCacheSettings>>().Value);
  }

  public static void AddServices(IServiceCollection services, string natsServerKey) {
    services.AddKeyedNatsObjectStoreBasedCache(ObjectStoreCacheKey, natsServerKey);
    services.AddKeyedNatsKeyValueBasedCache(KeyValueCacheKey, natsServerKey);
  }

  private const string CacheNatsServerKey = "CacheNatsServer";
  private const string ObjectStoreCacheKey = "ObjectStoreBasedCache";
  private const string ObjectStoreCacheConfigurationSectionPath = "ImageCache:ObjectStoreBasedCache";
  private const string KeyValueCacheKey = "KeyValueBasedCache";
  private const string KeyValueCacheConfigurationSectionPath = "ImageCache:KeyValueBasedCache";

```

In order to use the keyed cache in your HTTP request handlers (given an example of using the Minimal API) they need to be registered as follows:

```csharp
  public static void AddHttpHandlers(IServiceCollection services, string natsServerKey) {
    services.AddKeyedTransient<GetImageWithObjectStoreTryGetAsyncHttpRequestHandler>(
      ObjectStoreCacheKey,
      (diContainer, key) => new GetImageWithObjectStoreTryGetAsyncHttpRequestHandler(
        diContainer.GetRequiredKeyedService<IBufferDistributedCache>(key),
        diContainer.GetRequiredService<ILogger<GetImageWithObjectStoreTryGetAsyncHttpRequestHandler>>()));

    services.AddKeyedTransient<GetImageWithKeyValueTryGetAsyncHttpRequestHandler>(
      KeyValueCacheKey,
      (diContainer, key) => new GetImageWithKeyValueTryGetAsyncHttpRequestHandler(
        diContainer.GetRequiredKeyedService<IBufferDistributedCache>(key),
        diContainer.GetRequiredService<ILogger<GetImageWithKeyValueTryGetAsyncHttpRequestHandler>>()));
  }

  public static void MapEndpoints(IEndpointRouteBuilder endpoints) {
    endpoints.MapGet(
      "/object-store/try-get-async/{name}",
      async (
        [FromKeyedServices(ObjectStoreCacheKey)]
        GetImageWithObjectStoreTryGetAsyncHttpRequestHandler handler,
        string name) => await handler.Handle(name));

    endpoints.MapGet(
      "/key-value/try-get-async/{name}",
      async (
        [FromKeyedServices(KeyValueCacheKey)] GetImageWithKeyValueTryGetAsyncHttpRequestHandler handler,
        string name) => await handler.Handle(name));
  }
```

The configuration file looks like this (of course, you can map `ObjectStoreBasedCacheSettings` and `KeyValueBasedCacheSettings` as you like):

```json
{
  "CacheNatsServer": {
    "NatsServerConnectionString": "NOT SPECIFIED",
    "NatsConnectionName": "TestWebApp"
  },
  "ImageCache": {
    "ObjectStoreBasedCache": {
      "BucketName": "images-object",
      "DefaultSlidingExpirationInterval": "00:05:00",
      "ExpiredEntriesPurgingInterval": "00:05:00",
      "MaximalCacheInvalidationDuration": "00:03:00"
    },
    "KeyValueBasedCache": {
      "BucketName": "images-key-value",
      "DefaultSlidingExpirationInterval": "00:05:00",
      "ExpiredEntriesPurgingInterval": "00:05:00",
      "MaximalCacheInvalidationDuration": "00:03:00"
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```
