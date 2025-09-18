using System.Buffers;
using Microsoft.Extensions.Caching.Distributed;

namespace Eshva.Caching.Abstractions;

public interface ISetEntryWithByteReadOnlySequence {
  void Set(string key, ReadOnlySequence<byte> value, DistributedCacheEntryOptions options);
}
