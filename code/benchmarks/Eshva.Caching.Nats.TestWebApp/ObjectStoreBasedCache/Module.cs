namespace Eshva.Caching.Nats.TestWebApp.ObjectStoreBasedCache;

internal static class Module {
  public static void AddServices(IServiceCollection services) =>
    services.AddTransient<GetImageHttpRequestHandler>();

  public static void MapEndpoints(IEndpointRouteBuilder endpoints) =>
    endpoints.MapGet(
      "/object-store/images/{name}",
      async (GetImageHttpRequestHandler handler, string name) => await handler.Handle(name));
}
