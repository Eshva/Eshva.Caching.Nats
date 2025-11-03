using Eshva.Caching.Nats.TestWebApp.ObjectStoreBasedCache;
using Microsoft.Extensions.Options;
using NATS.Client.Core;

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

    Module.AddServices(services, CacheNatsServerKey);
  }

  public static void MapEndpoints(this IEndpointRouteBuilder endpoints) => Module.MapEndpoints(endpoints);

  private static void AddConfiguration(this IServiceCollection services) {
    services.AddOptions<NatsServerSettings>()
      .BindConfiguration(CacheNatsServerSectionPath)
      .ValidateDataAnnotations()
      .ValidateOnStart();
    services.AddKeyedSingleton<NatsServerSettings>(
      CacheNatsServerKey,
      (diContainer, _) => diContainer.GetRequiredService<IOptions<NatsServerSettings>>().Value);

    Module.AddConfiguration(services);
  }

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

  private const string CacheNatsServerKey = "CacheNatsServer";
  private const string CacheNatsServerSectionPath = "CacheNatsServer";
}
