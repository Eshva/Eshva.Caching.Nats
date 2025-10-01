using Eshva.Caching.Abstractions;
using Microsoft.Extensions.Caching.Distributed;

namespace Eshva.Caching.Nats.TestWebApp.ObjectStoreBasedCache;

internal static class Module {
  public static void AddServices(IServiceCollection services) {
    services.AddTransient<IBufferDistributedCache, NatsObjectStoreBasedCache>();
    services.AddTransient<ICacheEntryExpirationStrategy, StandardCacheEntryExpirationStrategy>(diContainer =>
      new StandardCacheEntryExpirationStrategy(
        new ExpirationStrategySettings { DefaultSlidingExpirationInterval = TimeSpan.FromMinutes(minutes: 5) },
        diContainer.GetRequiredService<TimeProvider>()));
    services.AddTransient<ICacheExpiredEntriesPurger, ObjectStoreBasedCacheExpiredEntriesPurger>(diContainer =>
      new ObjectStoreBasedCacheExpiredEntriesPurger());

    services.AddTransient<GetImageHttpRequestHandler>();
  }

  public static void MapEndpoints(IEndpointRouteBuilder endpoints) =>
    endpoints.MapGet(
      "/object-store/images/{name}",
      async (GetImageHttpRequestHandler handler, string name) => await handler.Handle(name));
}
