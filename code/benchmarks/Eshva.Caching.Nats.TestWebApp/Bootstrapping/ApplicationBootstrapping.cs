using Eshva.Caching.Nats.TestWebApp.ObjectStoreBasedCache;
using Microsoft.Extensions.Options;

namespace Eshva.Caching.Nats.TestWebApp.Bootstrapping;

public static class ApplicationBootstrapping {
  public static void AddConfiguration(this WebApplicationBuilder builder) =>
    builder.Configuration
      .AddJsonFile(@"appsettings.json")
      .AddEnvironmentVariables("BENCHMARKS_");

  public static void AddServices(this WebApplicationBuilder builder) {
    var services = builder.Services;

    services.AddConfiguration();
    services.AddSharedServices();

    Module.AddServices(services);
  }

  public static void MapEndpoints(this IEndpointRouteBuilder endpoints) => Module.MapEndpoints(endpoints);

  private static void AddSharedServices(this IServiceCollection services) { }

  private static void AddConfiguration(this IServiceCollection services) {
    services.AddOptions<Settings>()
      .BindConfiguration("ObjectStoreBasedCache")
      .ValidateDataAnnotations()
      .ValidateOnStart();
    services.AddTransient<Settings>(diContainer => diContainer.GetRequiredService<IOptions<Settings>>().Value);
  }
}
