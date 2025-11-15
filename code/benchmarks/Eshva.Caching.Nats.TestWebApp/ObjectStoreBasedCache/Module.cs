using Eshva.Caching.Nats.Distributed;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace Eshva.Caching.Nats.TestWebApp.ObjectStoreBasedCache;

internal static class Module {
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
    services.AddNatsObjectStoreBasedCache(ObjectStoreCacheKey, natsServerKey);
    services.AddNatsKeyValueBasedCache(KeyValueCacheKey, natsServerKey);

    services.AddKeyedTransient<GetImageWithObjectStoreTryGetAsyncHttpRequestHandler>(
      ObjectStoreCacheKey,
      (diContainer, key) => new GetImageWithObjectStoreTryGetAsyncHttpRequestHandler(
        diContainer.GetRequiredKeyedService<IBufferDistributedCache>(key),
        diContainer.GetRequiredService<ILogger<GetImageWithObjectStoreTryGetAsyncHttpRequestHandler>>()));
    services.AddKeyedTransient<GetImageWithObjectStoreGetAsyncHttpRequestHandler>(
      ObjectStoreCacheKey,
      (diContainer, key) => new GetImageWithObjectStoreGetAsyncHttpRequestHandler(
        diContainer.GetRequiredKeyedService<IBufferDistributedCache>(key),
        diContainer.GetRequiredService<ILogger<GetImageWithObjectStoreGetAsyncHttpRequestHandler>>()));

    services.AddKeyedTransient<GetImageWithKeyValueTryGetAsyncHttpRequestHandler>(
      KeyValueCacheKey,
      (diContainer, key) => new GetImageWithKeyValueTryGetAsyncHttpRequestHandler(
        diContainer.GetRequiredKeyedService<IBufferDistributedCache>(key),
        diContainer.GetRequiredService<ILogger<GetImageWithKeyValueTryGetAsyncHttpRequestHandler>>()));
    services.AddKeyedTransient<GetImageWithKeyValueGetAsyncHttpRequestHandler>(
      KeyValueCacheKey,
      (diContainer, key) => new GetImageWithKeyValueGetAsyncHttpRequestHandler(
        diContainer.GetRequiredKeyedService<IBufferDistributedCache>(key),
        diContainer.GetRequiredService<ILogger<GetImageWithKeyValueGetAsyncHttpRequestHandler>>()));
  }

  public static void MapEndpoints(IEndpointRouteBuilder endpoints) {
    endpoints.MapGet(
      "/object-store/try-get-async/{name}",
      async (
        [FromKeyedServices(ObjectStoreCacheKey)]
        GetImageWithObjectStoreTryGetAsyncHttpRequestHandler handler,
        string name) => await handler.Handle(name));
    endpoints.MapGet(
      "/object-store/get-async/{name}",
      async (
        [FromKeyedServices(ObjectStoreCacheKey)]
        GetImageWithObjectStoreGetAsyncHttpRequestHandler handler,
        string name) => await handler.Handle(name));
    endpoints.MapGet(
      "/key-value/try-get-async/{name}",
      async (
        [FromKeyedServices(KeyValueCacheKey)] GetImageWithKeyValueTryGetAsyncHttpRequestHandler handler,
        string name) => await handler.Handle(name));
    endpoints.MapGet(
      "/key-value/get-async/{name}",
      async (
        [FromKeyedServices(KeyValueCacheKey)] GetImageWithKeyValueGetAsyncHttpRequestHandler handler,
        string name) => await handler.Handle(name));
  }

  private const string ObjectStoreCacheKey = "ObjectStoreBasedCache";
  private const string ObjectStoreCacheConfigurationSectionPath = "ImageCache:ObjectStoreBasedCache";
  private const string KeyValueCacheKey = "KeyValueBasedCache";
  private const string KeyValueCacheConfigurationSectionPath = "ImageCache:KeyValueBasedCache";
}
