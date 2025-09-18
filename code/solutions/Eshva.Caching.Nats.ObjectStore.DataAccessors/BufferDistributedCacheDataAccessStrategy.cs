using System.Buffers;
using System.Runtime.CompilerServices;
using Eshva.Caching.Abstractions;
using Microsoft.Extensions.Caching.Distributed;

namespace Eshva.Caching.Nats.ObjectStore.DataAccessors;

public class BufferDistributedCacheDataAccessStrategy
  : DistributedCacheDataAccessStrategy,
    IBufferDistributedCacheDataAccessStrategy {
  public BufferDistributedCacheDataAccessStrategy(
    IGetEntryAsByteArray getEntryAsByteArray,
    IGetEntryAsByteArrayAsync getEntryAsByteArrayAsync,
    ISetEntryWithByteArray setEntryWithByteArray,
    ISetEntryWithByteArrayAsync setEntryWithByteArrayAsync,
    IRefreshEntry refreshEntry,
    IRefreshEntryAsync refreshEntryAsync,
    IRemoveEntry removeEntry,
    IRemoveEntryAsync removeEntryAsync,
    ITryGetEntryAsByteBufferWriter tryGetEntryAsByteBufferWriter,
    ITryGetEntryAsByteBufferWriterAsync tryGetEntryAsByteBufferWriterAsync,
    ISetEntryWithByteReadOnlySequence setEntryWithByteReadOnlySequence,
    ISetEntryWithByteReadOnlySequenceAsync setEntryWithByteReadOnlySequenceAsync) : base(
    getEntryAsByteArray,
    getEntryAsByteArrayAsync,
    setEntryWithByteArray,
    setEntryWithByteArrayAsync,
    refreshEntry,
    refreshEntryAsync,
    removeEntry,
    removeEntryAsync) {
    _tryGetEntryAsByteBufferWriter = tryGetEntryAsByteBufferWriter
                                     ?? throw new ArgumentNullException(nameof(tryGetEntryAsByteBufferWriter));
    _tryGetEntryAsByteBufferWriterAsync = tryGetEntryAsByteBufferWriterAsync
                                          ?? throw new ArgumentNullException(nameof(tryGetEntryAsByteBufferWriterAsync));
    _setEntryWithByteReadOnlySequence = setEntryWithByteReadOnlySequence
                                        ?? throw new ArgumentNullException(nameof(setEntryWithByteReadOnlySequence));
    _setEntryWithByteReadOnlySequenceAsync = setEntryWithByteReadOnlySequenceAsync
                                             ?? throw new ArgumentNullException(nameof(setEntryWithByteReadOnlySequenceAsync));
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  bool ITryGetEntryAsByteBufferWriter.TryGet(
    string key,
    IBufferWriter<byte> destination) =>
    _tryGetEntryAsByteBufferWriter.TryGet(key, destination);

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  ValueTask<bool> ITryGetEntryAsByteBufferWriterAsync.TryGetAsync(
    string key,
    IBufferWriter<byte> destination,
    CancellationToken token) =>
    _tryGetEntryAsByteBufferWriterAsync.TryGetAsync(key, destination, token);

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  void ISetEntryWithByteReadOnlySequence.Set(
    string key,
    ReadOnlySequence<byte> value,
    DistributedCacheEntryOptions options) =>
    _setEntryWithByteReadOnlySequence.Set(key, value, options);

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  ValueTask ISetEntryWithByteReadOnlySequenceAsync.SetAsync(
    string key,
    ReadOnlySequence<byte> value,
    DistributedCacheEntryOptions options,
    CancellationToken token) =>
    _setEntryWithByteReadOnlySequenceAsync.SetAsync(
      key,
      value,
      options,
      token);

  private readonly ISetEntryWithByteReadOnlySequence _setEntryWithByteReadOnlySequence;
  private readonly ISetEntryWithByteReadOnlySequenceAsync _setEntryWithByteReadOnlySequenceAsync;
  private readonly ITryGetEntryAsByteBufferWriter _tryGetEntryAsByteBufferWriter;
  private readonly ITryGetEntryAsByteBufferWriterAsync _tryGetEntryAsByteBufferWriterAsync;
}
