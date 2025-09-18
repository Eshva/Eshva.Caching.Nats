using Eshva.Caching.Abstractions;
using Eshva.Caching.Nats.ObjectStore.DataAccessors;
using Microsoft.Extensions.Logging;
using NATS.Client.ObjectStore;

namespace Eshva.Caching.Nats;

public sealed class DefaultNatsObjectStoreBasedCacheDataAccessStrategy : BufferDistributedCacheDataAccessStrategy {
  public DefaultNatsObjectStoreBasedCacheDataAccessStrategy(
    INatsObjStore cacheBucket,
    ICacheEntryExpirationStrategy expirationStrategy,
    ICacheExpiredEntriesPurger expiredEntriesPurger,
    ILogger<DefaultNatsObjectStoreBasedCacheDataAccessStrategy> logger)
    : base(
      new GetEntryAsByteArrayUsingNewByteArray(
        new GetEntryAsByteArrayAsyncUsingNewArray(
          cacheBucket,
          expirationStrategy,
          expiredEntriesPurger,
          logger)),
      new GetEntryAsByteArrayAsyncUsingNewArray(
        cacheBucket,
        expirationStrategy,
        expiredEntriesPurger,
        logger),
      new SetEntryWithByteArrayUsingNewArray(
        new SetEntryWithByteArrayAsyncUsingNewArray(
          cacheBucket,
          expirationStrategy,
          expiredEntriesPurger,
          logger)),
      new SetEntryWithByteArrayAsyncUsingNewArray(
        cacheBucket,
        expirationStrategy,
        expiredEntriesPurger,
        logger),
      new RefreshEntryUsingMetadata(
        new RefreshEntryAsyncUsingMetadata(
          cacheBucket,
          expirationStrategy,
          expiredEntriesPurger,
          logger)),
      new RefreshEntryAsyncUsingMetadata(
        cacheBucket,
        expirationStrategy,
        expiredEntriesPurger,
        logger),
      new RemoveEntry(
        new RemoveEntryAsync(
          cacheBucket,
          expirationStrategy,
          expiredEntriesPurger,
          logger)),
      new RemoveEntryAsync(
        cacheBucket,
        expirationStrategy,
        expiredEntriesPurger,
        logger),
      new TryGetEntryAsByteBufferWriterUsingNewArray(
        new TryGetEntryAsByteBufferWriterAsyncUsingNewArray(
          cacheBucket,
          expirationStrategy,
          expiredEntriesPurger,
          logger)),
      new TryGetEntryAsByteBufferWriterAsyncUsingNewArray(
        cacheBucket,
        expirationStrategy,
        expiredEntriesPurger,
        logger),
      new SetEntryWithByteReadOnlySequenceUsingNewArray(
        new SetEntryWithByteReadOnlySequenceAsyncUsingNewArray(
          cacheBucket,
          expirationStrategy,
          expiredEntriesPurger,
          logger)),
      new SetEntryWithByteReadOnlySequenceAsyncUsingNewArray(
        cacheBucket,
        expirationStrategy,
        expiredEntriesPurger,
        logger)) { }
}
