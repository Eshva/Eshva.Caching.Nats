using Microsoft.Extensions.Caching.Distributed;

namespace Eshva.Caching.Abstractions;

public interface ISetEntryWithByteArrayAsync {
  Task SetAsync(
    string key,
    byte[] value,
    DistributedCacheEntryOptions options,
    CancellationToken token = default);
}
