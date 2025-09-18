namespace Eshva.Caching.Abstractions;

public interface IDistributedCacheDataAccessStrategy
  : IGetEntryAsByteArray,
    IGetEntryAsByteArrayAsync,
    ISetEntryWithByteArray,
    ISetEntryWithByteArrayAsync,
    IRefreshEntry,
    IRefreshEntryAsync,
    IRemoveEntry,
    IRemoveEntryAsync;
