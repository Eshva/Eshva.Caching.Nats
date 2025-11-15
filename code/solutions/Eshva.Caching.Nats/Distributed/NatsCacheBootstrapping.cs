using Eshva.Caching.Abstractions.Distributed;
using JetBrains.Annotations;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using NATS.Net;

#pragma warning disable VSTHRD002

namespace Eshva.Caching.Nats.Distributed;

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
  /// <param name="natsServerKey">NATS server connection service key.</param>
  /// <returns>Service collection.</returns>
  public static IServiceCollection AddNatsObjectStoreBasedCache(
    this IServiceCollection services,
    string serviceKey,
    string natsServerKey) {
    services.AddKeyedSingleton<IBufferDistributedCache, NatsObjectStoreBasedCache>(
        serviceKey,
        (diContainer, key) => CreateNatsObjectStoreBasedCache(
          diContainer.GetRequiredKeyedService<INatsConnection>(natsServerKey),
          diContainer.GetRequiredKeyedService<ObjectStoreBasedCacheSettings>(key),
          diContainer))
      .AddKeyedSingleton<IDistributedCache>(
        serviceKey,
        (diContainer, key) => diContainer.GetRequiredKeyedService<IBufferDistributedCache>(key));
    return services;
  }

  /// <summary>
  /// Add NATS object store based cache as a common service into DI-container.
  /// </summary>
  /// <remarks>
  /// Cache can be placed on a separate NATS cluster. To separate server connection registrations the
  /// <paramref name="natsServerKey"/> is used.
  /// </remarks>
  /// <param name="services">DI-container.</param>
  /// <param name="natsServerKey">NATS server connection service key.</param>
  /// <returns>Service collection.</returns>
  public static IServiceCollection AddNatsObjectStoreBasedCache(this IServiceCollection services, string natsServerKey) {
    services.AddSingleton<IBufferDistributedCache, NatsObjectStoreBasedCache>(diContainer =>
        CreateNatsObjectStoreBasedCache(
          diContainer.GetRequiredKeyedService<INatsConnection>(natsServerKey),
          diContainer.GetRequiredService<ObjectStoreBasedCacheSettings>(),
          diContainer))
      .AddSingleton<IDistributedCache>(diContainer => diContainer.GetRequiredService<IBufferDistributedCache>());
    return services;
  }

  /// <summary>
  /// Add NATS object store based cache as a common service into DI-container.
  /// </summary>
  /// <param name="services">DI-container.</param>
  /// <returns>Service collection.</returns>
  public static IServiceCollection AddNatsObjectStoreBasedCache(this IServiceCollection services) {
    services.AddSingleton<IBufferDistributedCache, NatsObjectStoreBasedCache>(diContainer =>
        CreateNatsObjectStoreBasedCache(
          diContainer.GetRequiredService<INatsConnection>(),
          diContainer.GetRequiredService<ObjectStoreBasedCacheSettings>(),
          diContainer))
      .AddSingleton<IDistributedCache>(diContainer => diContainer.GetRequiredService<IBufferDistributedCache>());
    return services;
  }

  /// <summary>
  /// Add NATS key-value store based cache as a keyed service into DI-container.
  /// </summary>
  /// <remarks>
  /// Cache can be placed on a separate NATS cluster. To separate server connection registrations the
  /// <paramref name="natsServerKey"/> is used. A few caches for different content could be used in an application. To
  /// separate those caches <paramref name="serviceKey"/> is used.
  /// </remarks>
  /// <param name="services">DI-container.</param>
  /// <param name="serviceKey">Cache services key.</param>
  /// <param name="natsServerKey">NATS server connection service key.</param>
  /// <returns>Service collection.</returns>
  public static IServiceCollection AddNatsKeyValueBasedCache(
    this IServiceCollection services,
    string serviceKey,
    string natsServerKey) {
    services.AddKeyedSingleton<IBufferDistributedCache, NatsKeyValueStoreBasedCache>(
        serviceKey,
        (diContainer, key) =>
          CreateNatsKeyValueStoreBasedCache(
            diContainer.GetRequiredKeyedService<INatsConnection>(natsServerKey),
            diContainer.GetRequiredKeyedService<KeyValueBasedCacheSettings>(key),
            diContainer))
      .AddKeyedSingleton<IDistributedCache>(
        serviceKey,
        (diContainer, key) => diContainer.GetRequiredKeyedService<IBufferDistributedCache>(key));
    return services;
  }

  /// <summary>
  /// Add NATS key-value store based cache as a common service into DI-container.
  /// </summary>
  /// <remarks>
  /// Cache can be placed on a separate NATS cluster. To separate server connection registrations the
  /// <paramref name="natsServerKey"/> is used.
  /// </remarks>
  /// <param name="services">DI-container.</param>
  /// <param name="natsServerKey">NATS server connection service key.</param>
  /// <returns>Service collection.</returns>
  public static IServiceCollection AddNatsKeyValueBasedCache(this IServiceCollection services, string natsServerKey) {
    services.AddSingleton<IBufferDistributedCache, NatsKeyValueStoreBasedCache>(diContainer =>
        CreateNatsKeyValueStoreBasedCache(
          diContainer.GetRequiredKeyedService<INatsConnection>(natsServerKey),
          diContainer.GetRequiredService<KeyValueBasedCacheSettings>(),
          diContainer))
      .AddSingleton<IDistributedCache>(diContainer => diContainer.GetRequiredService<IBufferDistributedCache>());
    return services;
  }

  /// <summary>
  /// Add NATS key-value store based cache as a common service into DI-container.
  /// </summary>
  /// <param name="services">DI-container.</param>
  /// <returns>Service collection.</returns>
  public static IServiceCollection AddNatsKeyValueBasedCache(this IServiceCollection services) {
    services.AddSingleton<IBufferDistributedCache, NatsKeyValueStoreBasedCache>(diContainer =>
        CreateNatsKeyValueStoreBasedCache(
          diContainer.GetRequiredService<INatsConnection>(),
          diContainer.GetRequiredService<KeyValueBasedCacheSettings>(),
          diContainer))
      .AddSingleton<IDistributedCache>(diContainer => diContainer.GetRequiredService<IBufferDistributedCache>());
    return services;
  }

  private static NatsObjectStoreBasedCache CreateNatsObjectStoreBasedCache(
    INatsConnection natsClient,
    ObjectStoreBasedCacheSettings settings,
    IServiceProvider diContainer) {
    var objectStoreContext = natsClient.CreateJetStreamContext()
      .CreateObjectStoreContext();
    var bucket = objectStoreContext.GetObjectStoreAsync(settings.BucketName).AsTask().GetAwaiter().GetResult();
    var timeProvider = diContainer.GetRequiredService<TimeProvider>();
    var expiryCalculator = new CacheEntryExpiryCalculator(settings.DefaultSlidingExpirationInterval, timeProvider);
    var datastore = new ObjectStoreBasedDatastore(bucket, expiryCalculator);
    var invalidation = new ObjectStoreBasedCacheInvalidation(
      bucket,
      settings.ExpiredEntriesPurgingInterval,
      settings.MaximalCacheInvalidationDuration,
      expiryCalculator,
      timeProvider,
      diContainer.GetRequiredService<ILogger<ObjectStoreBasedCacheInvalidation>>());
    return new NatsObjectStoreBasedCache(
      datastore,
      invalidation,
      diContainer.GetRequiredService<ILogger<NatsObjectStoreBasedCache>>());
  }

  private static NatsKeyValueStoreBasedCache CreateNatsKeyValueStoreBasedCache(
    INatsConnection natsClient,
    KeyValueBasedCacheSettings settings,
    IServiceProvider diContainer) {
    var keyValueStoreContext = natsClient.CreateJetStreamContext().CreateKeyValueStoreContext();
    var entriesStore = keyValueStoreContext.CreateStoreAsync(settings.BucketName).AsTask().GetAwaiter().GetResult();
    var timeProvider = diContainer.GetRequiredService<TimeProvider>();
    var expiryCalculator = new CacheEntryExpiryCalculator(settings.DefaultSlidingExpirationInterval, timeProvider);
    var expirySerializer = new CacheEntryExpiryBinarySerializer();
    var datastore = new KeyValueBasedDatastore(
      entriesStore,
      expirySerializer,
      expiryCalculator);
    var invalidation = new KeyValueBasedCacheInvalidation(
      entriesStore,
      settings.ExpiredEntriesPurgingInterval,
      settings.MaximalCacheInvalidationDuration,
      expirySerializer,
      expiryCalculator,
      timeProvider,
      diContainer.GetRequiredService<ILogger<KeyValueBasedCacheInvalidation>>());
    return new NatsKeyValueStoreBasedCache(
      datastore,
      invalidation,
      diContainer.GetRequiredService<ILogger<NatsKeyValueStoreBasedCache>>());
  }
}
