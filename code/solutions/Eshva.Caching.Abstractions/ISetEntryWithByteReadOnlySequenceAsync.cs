using System.Buffers;
using Microsoft.Extensions.Caching.Distributed;

namespace Eshva.Caching.Abstractions;

public interface ISetEntryWithByteReadOnlySequenceAsync {
  ValueTask SetAsync(
    string key,
    ReadOnlySequence<byte> value,
    DistributedCacheEntryOptions options,
    CancellationToken token = default);
}
