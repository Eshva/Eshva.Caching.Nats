using Eshva.Caching.Abstractions;
using JetBrains.Annotations;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;
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
  /// <param name="natsServerKey">NATS server connection service key.</param>
  /// <returns>Service collection.</returns>
  public static IServiceCollection AddNatsObjectStoreBasedCache(
    this IServiceCollection services,
    string serviceKey,
    string natsServerKey) {
    services.AddKeyedSingleton<IBufferDistributedCache, NatsObjectStoreBasedCache>(
      serviceKey,
      (diContainer, key) => {
        var natsClient = diContainer.GetRequiredKeyedService<INatsConnection>(natsServerKey);
        var settings = diContainer.GetRequiredKeyedService<ObjectStoreBasedCacheSettings>(key);
        var objectStoreContext = natsClient.CreateJetStreamContext()
          .CreateObjectStoreContext();
        var bucket = objectStoreContext.GetObjectStoreAsync(settings.BucketName).AsTask().GetAwaiter().GetResult();
        var timeProvider = diContainer.GetRequiredService<TimeProvider>();
        var expiryCalculator = new CacheEntryExpiryCalculator(settings.DefaultSlidingExpirationInterval, timeProvider);
        var datastore = new ObjectStoreBasedDatastore(bucket, expiryCalculator);
        var invalidation = new ObjectStoreBasedCacheInvalidation(
          bucket,
          settings.ExpiredEntriesPurgingInterval,
          expiryCalculator,
          timeProvider,
          diContainer.GetRequiredService<ILogger<ObjectStoreBasedCacheInvalidation>>());
        return new NatsObjectStoreBasedCache(
          datastore,
          invalidation,
          diContainer.GetRequiredService<ILogger<NatsObjectStoreBasedCache>>());
      });

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
      (diContainer, key) => {
        var natsClient = diContainer.GetRequiredKeyedService<INatsConnection>(natsServerKey);
        var settings = diContainer.GetRequiredKeyedService<KeyValueBasedCacheSettings>(key);
        var keyValueStoreContext = natsClient.CreateJetStreamContext().CreateKeyValueStoreContext();
        var valueStore = keyValueStoreContext.CreateStoreAsync(settings.ValueStore).AsTask().GetAwaiter().GetResult();
        var metadataStore = keyValueStoreContext.CreateStoreAsync(settings.MetadataStore).AsTask().GetAwaiter().GetResult();
        var timeProvider = diContainer.GetRequiredService<TimeProvider>();
        var expiryCalculator = new CacheEntryExpiryCalculator(settings.DefaultSlidingExpirationInterval, timeProvider);
        var expirySerializer = new CacheEntryExpiryJsonSerializer();
        var datastore = new KeyValueBasedDatastore(
          valueStore,
          metadataStore,
          expirySerializer,
          expiryCalculator);
        var invalidation = new KeyValueBasedCacheInvalidation(
          valueStore,
          metadataStore,
          settings.ExpiredEntriesPurgingInterval,
          expirySerializer,
          expiryCalculator,
          timeProvider,
          diContainer.GetRequiredService<ILogger<KeyValueBasedCacheInvalidation>>());
        return new NatsKeyValueStoreBasedCache(
          datastore,
          invalidation,
          diContainer.GetRequiredService<ILogger<NatsKeyValueStoreBasedCache>>());
      });

    return services;
  }
}
