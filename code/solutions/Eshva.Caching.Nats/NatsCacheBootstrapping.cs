using Eshva.Caching.Abstractions;
using JetBrains.Annotations;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using NATS.Client.ObjectStore;
using NATS.Net;

#pragma warning disable VSTHRD002

namespace Eshva.Caching.Nats;

/// <summary>
/// NATS-based caches bootstrapping extensions.
/// </summary>
[PublicAPI]
public static class NatsCacheBootstrapping {
  /// <summary>
  /// Add NATS object store based cache as a keyed service into DI-container.
  /// </summary>
  /// <remarks>
  /// Cache can be placed on a separate NATS cluster. To separate server connection registrations the
  /// <paramref name="natsConnectionKey"/> is used. A few caches for different content could be used in an application. To
  /// separate those caches <paramref name="serviceKey"/> is used.
  /// </remarks>
  /// <param name="services">DI-container.</param>
  /// <param name="serviceKey">Cache services key.</param>
  /// <param name="natsConnectionKey">NATS connectin service key.</param>
  /// <param name="cacheBucketName">Cache bucket name.</param>
  /// <returns>Service collection.</returns>
  public static IServiceCollection AddNatsObjectStoreBasedCache(
    this IServiceCollection services,
    string serviceKey,
    string natsConnectionKey,
    string cacheBucketName) {
    services.AddKeyedSingleton<INatsObjStore>(
      serviceKey,
      (diContainer, _) => diContainer.GetRequiredKeyedService<INatsConnection>(natsConnectionKey)
        .CreateJetStreamContext()
        .CreateObjectStoreContext()
        .GetObjectStoreAsync(cacheBucketName)
        .AsTask()
        .GetAwaiter()
        .GetResult());

    AddCacheInvalidation(services, serviceKey);
    AddObjectStoreBasedCache(services, serviceKey);

    return services;
  }

  /// <summary>
  /// Add NATS object store based cache as a keyed service into DI-container.
  /// </summary>
  /// <remarks>
  /// A few caches for different content could be used in an application. To separate those caches
  /// <paramref name="serviceKey"/> is used.
  /// </remarks>
  /// <param name="services">DI-container.</param>
  /// <param name="serviceKey">Cache services key.</param>
  /// <param name="cacheBucketName">Cache bucket name.</param>
  /// <returns>Service collection.</returns>
  public static IServiceCollection AddNatsObjectStoreBasedCache(
    this IServiceCollection services,
    string serviceKey,
    string cacheBucketName) {
    services.AddKeyedSingleton<INatsObjStore>(
      serviceKey,
      (diContainer, _) => diContainer.GetRequiredService<INatsConnection>()
        .CreateJetStreamContext()
        .CreateObjectStoreContext()
        .GetObjectStoreAsync(cacheBucketName)
        .AsTask()
        .GetAwaiter()
        .GetResult());

    AddCacheInvalidation(services, serviceKey);
    AddObjectStoreBasedCache(services, serviceKey);

    return services;
  }

  private static void AddCacheInvalidation(IServiceCollection services, string serviceKey) =>
    services.AddKeyedSingleton<ObjectStoreBasedCacheInvalidation>(
      serviceKey,
      (diContainer, key) =>
        new ObjectStoreBasedCacheInvalidation(
          diContainer.GetRequiredKeyedService<INatsObjStore>(key),
          new TimeBasedCacheInvalidationSettings {
            ExpiredEntriesPurgingInterval = TimeSpan.FromMinutes(value: 5D),
            DefaultSlidingExpirationInterval = TimeSpan.FromMinutes(value: 5D)
          },
          diContainer.GetRequiredService<TimeProvider>(),
          diContainer.GetRequiredService<ILogger<ObjectStoreBasedCacheInvalidation>>()));

  private static void AddObjectStoreBasedCache(IServiceCollection services, string serviceKey) =>
    services.AddKeyedSingleton<IBufferDistributedCache, NatsObjectStoreBasedCache1>(
      serviceKey,
      (diContainer, key) => new NatsObjectStoreBasedCache1(
        diContainer.GetRequiredKeyedService<INatsObjStore>(key),
        diContainer.GetRequiredKeyedService<ObjectStoreBasedCacheInvalidation>(key),
        diContainer.GetRequiredService<ILogger<NatsObjectStoreBasedCache1>>()));
}
