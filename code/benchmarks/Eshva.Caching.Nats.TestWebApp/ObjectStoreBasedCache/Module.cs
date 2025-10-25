using Eshva.Caching.Abstractions;
using Microsoft.Extensions.Caching.Distributed;
using NATS.Client.Core;
using NATS.Client.ObjectStore;
using NATS.Net;

namespace Eshva.Caching.Nats.TestWebApp.ObjectStoreBasedCache;

internal static class Module {
  public static void AddServices(IServiceCollection services) {
    services.AddKeyedSingleton<INatsConnection>(
      ImagesCacheKey,
      (diContainer, key) => {
        var connectionName = diContainer.GetRequiredService<Settings>().NatsConnectionName;
        return new NatsConnection(
          new NatsOpts {
            Url = diContainer.GetRequiredService<Settings>().NatsServerConnectionString,
            Name = !string.IsNullOrWhiteSpace(connectionName) ? connectionName : $"Connection for {key}"
          });
      });

    services.AddKeyedSingleton<INatsObjStore>(
      ImagesCacheKey,
      (diContainer, key) => diContainer.GetRequiredKeyedService<INatsConnection>(key)
        .CreateJetStreamContext()
        .CreateObjectStoreContext()
        .GetObjectStoreAsync(ImagesCacheBucketName)
        .GetAwaiter()
        .GetResult());

    services.AddKeyedSingleton<StandardTimeBasedCacheInvalidation>(
      ImagesCacheKey,
      (diContainer, _) =>
        new StandardTimeBasedCacheInvalidation(
          new StandardTimeBasedCacheInvalidationSettings { DefaultSlidingExpirationInterval = TimeSpan.FromMinutes(minutes: 5) },
          diContainer.GetRequiredService<TimeProvider>()));

    services.AddKeyedSingleton<ICacheInvalidator, ObjectStoreBasedCacheInvalidator>(
      ImagesCacheKey,
      (diContainer, key) =>
        new ObjectStoreBasedCacheInvalidator(
          diContainer.GetRequiredKeyedService<INatsObjStore>(key),
          diContainer.GetRequiredKeyedService<StandardTimeBasedCacheInvalidation>(key),
          new TimeBasedCacheInvalidatorSettings { ExpiredEntriesPurgingInterval = TimeSpan.FromMinutes(minutes: 5) },
          diContainer.GetRequiredService<TimeProvider>(),
          diContainer.GetRequiredService<ILogger<ObjectStoreBasedCacheInvalidator>>()));

    services.AddKeyedSingleton<IBufferDistributedCache, NatsObjectStoreBasedCache>(
      ImagesCacheKey,
      (diContainer, key) => new NatsObjectStoreBasedCache(
        diContainer.GetRequiredKeyedService<INatsObjStore>(key),
        diContainer.GetRequiredKeyedService<StandardTimeBasedCacheInvalidation>(key),
        diContainer.GetRequiredKeyedService<ICacheInvalidator>(key),
        diContainer.GetRequiredService<ILogger<NatsObjectStoreBasedCache>>()));

    services.AddKeyedTransient<GetImageHttpRequestHandler>(
      ImagesCacheKey,
      (diContainer, key) => new GetImageHttpRequestHandler(
        diContainer.GetRequiredKeyedService<IBufferDistributedCache>(key),
        diContainer.GetRequiredService<ILogger<GetImageHttpRequestHandler>>()));
  }

  public static void MapEndpoints(IEndpointRouteBuilder endpoints) =>
    endpoints.MapGet(
      "/object-store/images/{name}",
      async (
        [FromKeyedServices(ImagesCacheKey)] GetImageHttpRequestHandler handler,
        string name) => await handler.Handle(name));

  private const string ImagesCacheKey = "images";
  private const string ImagesCacheBucketName = "images";
}
