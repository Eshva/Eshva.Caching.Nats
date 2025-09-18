using System.Buffers;
using Eshva.Caching.Abstractions;
using Microsoft.Extensions.Caching.Distributed;

namespace Eshva.Caching.Nats.ObjectStore.DataAccessors;

public class SetEntryWithByteReadOnlySequenceUsingNewArray : ISetEntryWithByteReadOnlySequence {
  public SetEntryWithByteReadOnlySequenceUsingNewArray(ISetEntryWithByteReadOnlySequenceAsync accessor) {
    _accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));
  }

  public void Set(string key, ReadOnlySequence<byte> value, DistributedCacheEntryOptions options) =>
    _accessor.SetAsync(
        key,
        value,
        options,
        CancellationToken.None)
      .GetAwaiter()
      .GetResult();

  private readonly ISetEntryWithByteReadOnlySequenceAsync _accessor;
}
