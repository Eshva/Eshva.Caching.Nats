using Eshva.Caching.Abstractions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NATS.Client.ObjectStore;
using NATS.Client.ObjectStore.Models;

namespace Eshva.Caching.Nats.ObjectStore.DataAccessors;

public abstract class NatsObjectStoreBasedDataAccessor {
  protected NatsObjectStoreBasedDataAccessor(
    INatsObjStore cacheBucket,
    ICacheEntryExpirationStrategy expirationStrategy,
    ICacheExpiredEntriesPurger expiredEntriesPurger,
    ILogger? logger = null) {
    CacheBucket = cacheBucket ?? throw new ArgumentNullException(nameof(cacheBucket));
    ExpirationStrategy = expirationStrategy ?? throw new ArgumentNullException(nameof(expirationStrategy));
    ExpiredEntriesPurger = expiredEntriesPurger ?? throw new ArgumentNullException(nameof(expiredEntriesPurger));
    Logger = logger ?? NullLogger.Instance;
  }

  public ILogger Logger { get; }

  protected INatsObjStore CacheBucket { get; }

  protected ICacheEntryExpirationStrategy ExpirationStrategy { get; }

  protected ICacheExpiredEntriesPurger ExpiredEntriesPurger { get; }

  protected static void ValidateKey(string key) =>
    ArgumentException.ThrowIfNullOrWhiteSpace(key, "The key is not specified.");

  protected async Task RefreshExpiresAt(ObjectMetadata objectMetadata, CancellationToken token) {
    objectMetadata.Metadata ??= new Dictionary<string, string>();
    var entryMetadata = new CacheEntryMetadata(objectMetadata.Metadata);
    entryMetadata.ExpiresAtUtc = ExpirationStrategy.CalculateExpiration(
      entryMetadata.AbsoluteExpirationUtc,
      entryMetadata.SlidingExpiration);
    await CacheBucket.UpdateMetaAsync(objectMetadata.Name, objectMetadata, token);
  }

  protected Dictionary<string, string> FillCacheEntryMetadata(DistributedCacheEntryOptions options) {
    var absoluteExpirationUtc = ExpirationStrategy.CalculateAbsoluteExpiration(
      options.AbsoluteExpiration,
      options.AbsoluteExpirationRelativeToNow);
    return new CacheEntryMetadata {
      SlidingExpiration = options.SlidingExpiration,
      AbsoluteExpirationUtc = absoluteExpirationUtc,
      ExpiresAtUtc = ExpirationStrategy.CalculateExpiration(absoluteExpirationUtc, options.SlidingExpiration)
    };
  }
}
