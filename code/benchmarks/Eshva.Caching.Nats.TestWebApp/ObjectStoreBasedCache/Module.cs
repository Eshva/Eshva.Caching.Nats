using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace Eshva.Caching.Nats.TestWebApp.ObjectStoreBasedCache;

internal static class Module {
  public static void AddConfiguration(IServiceCollection services) {
    services.AddOptions<ObjectStoreBasedCacheSettings>()
      .BindConfiguration(CacheConfigurationSectionPath)
      .ValidateDataAnnotations()
      .ValidateOnStart();
    services.AddKeyedSingleton<ObjectStoreBasedCacheSettings>(
      CacheKey,
      (diContainer, _) => diContainer.GetRequiredService<IOptions<ObjectStoreBasedCacheSettings>>().Value);
  }

  public static void AddServices(IServiceCollection services, string natsServerKey) {
    services.AddNatsObjectStoreBasedCache(CacheKey, natsServerKey);

    services.AddKeyedTransient<GetImageWithTryGetAsyncHttpRequestHandler>(
      CacheKey,
      (diContainer, key) => new GetImageWithTryGetAsyncHttpRequestHandler(
        diContainer.GetRequiredKeyedService<IBufferDistributedCache>(key),
        diContainer.GetRequiredService<ILogger<GetImageWithTryGetAsyncHttpRequestHandler>>()));
    services.AddKeyedTransient<GetImageWithGetAsyncHttpRequestHandler>(
      CacheKey,
      (diContainer, key) => new GetImageWithGetAsyncHttpRequestHandler(
        diContainer.GetRequiredKeyedService<IBufferDistributedCache>(key),
        diContainer.GetRequiredService<ILogger<GetImageWithGetAsyncHttpRequestHandler>>()));
  }

  public static void MapEndpoints(IEndpointRouteBuilder endpoints) {
    endpoints.MapGet(
      "/object-store/try-get-async/{name}",
      async (
        [FromKeyedServices(CacheKey)] GetImageWithTryGetAsyncHttpRequestHandler handler,
        string name) => await handler.Handle(name));
    endpoints.MapGet(
      "/object-store/get-async/{name}",
      async (
        [FromKeyedServices(CacheKey)] GetImageWithGetAsyncHttpRequestHandler handler,
        string name) => await handler.Handle(name));
  }

  private const string CacheKey = "ObjectStoreBasedCache";
  private const string CacheConfigurationSectionPath = "ImageCache:ObjectStoreBasedCache";
}
