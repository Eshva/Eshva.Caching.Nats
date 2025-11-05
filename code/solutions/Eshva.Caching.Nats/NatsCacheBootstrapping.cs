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
  /// <paramref name="natsServerKey"/> is used. A few caches for different content could be used in an application. To
  /// separate those caches <paramref name="serviceKey"/> is used.
  /// </remarks>
  /// <param name="services">DI-container.</param>
  /// <param name="serviceKey">Cache services key.</param>
  /// <param name="natsServerKey">NATS server connectin service key.</param>
  /// <returns>Service collection.</returns>
  public static IServiceCollection AddNatsObjectStoreBasedCache(
    this IServiceCollection services,
    string serviceKey,
    string natsServerKey) {
    services.AddKeyedSingleton<INatsObjStore>(
      serviceKey,
      (diContainer, _) => diContainer.GetRequiredKeyedService<INatsConnection>(natsServerKey)
        .CreateJetStreamContext()
        .CreateObjectStoreContext()
        .GetObjectStoreAsync(diContainer.GetRequiredKeyedService<ObjectStoreBasedCacheSettings>(serviceKey).BucketName)
        .AsTask()
        .GetAwaiter()
        .GetResult());

    AddCacheEntryExpiryCalculator(services, serviceKey);
    AddCacheDatastore(services, serviceKey);
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
  /// <returns>Service collection.</returns>
  public static IServiceCollection AddNatsObjectStoreBasedCache(
    this IServiceCollection services,
    string serviceKey) {
    services.AddKeyedSingleton<INatsObjStore>(
      serviceKey,
      (diContainer, _) => diContainer.GetRequiredService<INatsConnection>()
        .CreateJetStreamContext()
        .CreateObjectStoreContext()
        .GetObjectStoreAsync(diContainer.GetRequiredKeyedService<ObjectStoreBasedCacheSettings>(serviceKey).BucketName)
        .AsTask()
        .GetAwaiter()
        .GetResult());

    AddCacheEntryExpiryCalculator(services, serviceKey);
    AddCacheDatastore(services, serviceKey);
    AddCacheInvalidation(services, serviceKey);
    AddObjectStoreBasedCache(services, serviceKey);

    return services;
  }

  private static void AddCacheEntryExpiryCalculator(IServiceCollection services, string serviceKey) =>
    services.AddKeyedSingleton<CacheEntryExpiryCalculator>(
      serviceKey,
      (diContainer, key) => new CacheEntryExpiryCalculator(
        diContainer.GetRequiredKeyedService<ObjectStoreBasedCacheSettings>(key).DefaultSlidingExpirationInterval,
        diContainer.GetRequiredService<TimeProvider>()));

  private static void AddCacheDatastore(IServiceCollection services, string serviceKey) =>
    services.AddKeyedSingleton<ObjectStoreBasedDatastore>(
      serviceKey,
      (diContainer, key) => new ObjectStoreBasedDatastore(
        diContainer.GetRequiredKeyedService<INatsObjStore>(key),
        diContainer.GetRequiredKeyedService<CacheEntryExpiryCalculator>(key),
        diContainer.GetRequiredService<ILogger<ObjectStoreBasedDatastore>>()));

  private static void AddCacheInvalidation(IServiceCollection services, string serviceKey) =>
    services.AddKeyedSingleton<ObjectStoreBasedCacheInvalidation>(
      serviceKey,
      (diContainer, key) => {
        var settings = diContainer.GetRequiredKeyedService<ObjectStoreBasedCacheSettings>(key);
        return new ObjectStoreBasedCacheInvalidation(
          diContainer.GetRequiredKeyedService<INatsObjStore>(key),
          settings.ExpiredEntriesPurgingInterval,
          diContainer.GetRequiredKeyedService<CacheEntryExpiryCalculator>(key),
          diContainer.GetRequiredService<TimeProvider>(),
          diContainer.GetRequiredService<ILogger<ObjectStoreBasedCacheInvalidation>>());
      });

  private static void AddObjectStoreBasedCache(IServiceCollection services, string serviceKey) =>
    services.AddKeyedSingleton<IBufferDistributedCache, NatsObjectStoreBasedCache>(
      serviceKey,
      (diContainer, key) => new NatsObjectStoreBasedCache(
        diContainer.GetRequiredKeyedService<ObjectStoreBasedDatastore>(key),
        diContainer.GetRequiredKeyedService<ObjectStoreBasedCacheInvalidation>(key),
        diContainer.GetRequiredService<ILogger<NatsObjectStoreBasedCache>>()));
}
