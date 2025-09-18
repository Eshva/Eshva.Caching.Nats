using Eshva.Caching.Abstractions;
using Microsoft.Extensions.Caching.Distributed;

namespace Eshva.Caching.Nats.ObjectStore.DataAccessors;

public class SetEntryWithByteArrayUsingNewArray : ISetEntryWithByteArray {
  public SetEntryWithByteArrayUsingNewArray(ISetEntryWithByteArrayAsync accessor) {
    _accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));
  }

  public void Set(string key, byte[] value, DistributedCacheEntryOptions options) =>
    _accessor.SetAsync(
        key,
        value,
        options,
        CancellationToken.None)
      .GetAwaiter()
      .GetResult();

  private readonly ISetEntryWithByteArrayAsync _accessor;
}
