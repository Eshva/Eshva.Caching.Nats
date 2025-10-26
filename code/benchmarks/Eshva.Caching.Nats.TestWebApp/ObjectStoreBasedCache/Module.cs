using Microsoft.Extensions.Caching.Distributed;
using NATS.Client.Core;

namespace Eshva.Caching.Nats.TestWebApp.ObjectStoreBasedCache;

internal static class Module {
  public static void AddServices(IServiceCollection services) {
    services.AddKeyedSingleton<INatsConnection>(
      ImagesCacheNatsServerKey,
      (diContainer, key) => {
        var connectionName = diContainer.GetRequiredService<Settings>().NatsConnectionName;
        return new NatsConnection(
          new NatsOpts {
            Url = diContainer.GetRequiredService<Settings>().NatsServerConnectionString,
            Name = !string.IsNullOrWhiteSpace(connectionName) ? connectionName : $"Connection for {key}"
          });
      });

    services.AddNatsObjectStoreBasedCache(ImagesCacheKey, ImagesCacheNatsServerKey, ImagesCacheBucketName);

    services.AddKeyedTransient<GetImageWithTryGetAsyncHttpRequestHandler>(
      ImagesCacheKey,
      (diContainer, key) => new GetImageWithTryGetAsyncHttpRequestHandler(
        diContainer.GetRequiredKeyedService<IBufferDistributedCache>(key),
        diContainer.GetRequiredService<ILogger<GetImageWithTryGetAsyncHttpRequestHandler>>()));
    services.AddKeyedTransient<GetImageWithGetAsyncHttpRequestHandler>(
      ImagesCacheKey,
      (diContainer, key) => new GetImageWithGetAsyncHttpRequestHandler(
        diContainer.GetRequiredKeyedService<IBufferDistributedCache>(key),
        diContainer.GetRequiredService<ILogger<GetImageWithGetAsyncHttpRequestHandler>>()));
  }

  public static void MapEndpoints(IEndpointRouteBuilder endpoints) {
    endpoints.MapGet(
      "/object-store/try-get-async/{name}",
      async (
        [FromKeyedServices(ImagesCacheKey)] GetImageWithTryGetAsyncHttpRequestHandler handler,
        string name) => await handler.Handle(name));
    endpoints.MapGet(
      "/object-store/get-async/{name}",
      async (
        [FromKeyedServices(ImagesCacheKey)] GetImageWithGetAsyncHttpRequestHandler handler,
        string name) => await handler.Handle(name));
  }

  private const string ImagesCacheNatsServerKey = "cache NATS server";
  private const string ImagesCacheKey = "images cache";
  private const string ImagesCacheBucketName = "images";
}
