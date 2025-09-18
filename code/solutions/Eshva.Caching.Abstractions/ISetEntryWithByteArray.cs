using Microsoft.Extensions.Caching.Distributed;

namespace Eshva.Caching.Abstractions;

public interface ISetEntryWithByteArray {
  void Set(string key, byte[] value, DistributedCacheEntryOptions options);
}
