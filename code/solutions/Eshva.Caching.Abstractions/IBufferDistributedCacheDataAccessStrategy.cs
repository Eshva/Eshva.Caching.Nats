namespace Eshva.Caching.Abstractions;

public interface IBufferDistributedCacheDataAccessStrategy
  : IDistributedCacheDataAccessStrategy,
    ITryGetEntryAsByteBufferWriter,
    ITryGetEntryAsByteBufferWriterAsync,
    ISetEntryWithByteReadOnlySequence,
    ISetEntryWithByteReadOnlySequenceAsync;
