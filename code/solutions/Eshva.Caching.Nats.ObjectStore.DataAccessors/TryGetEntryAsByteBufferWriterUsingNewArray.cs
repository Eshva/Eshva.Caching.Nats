using System.Buffers;
using Eshva.Caching.Abstractions;

namespace Eshva.Caching.Nats.ObjectStore.DataAccessors;

public class TryGetEntryAsByteBufferWriterUsingNewArray : ITryGetEntryAsByteBufferWriter {
  public TryGetEntryAsByteBufferWriterUsingNewArray(ITryGetEntryAsByteBufferWriterAsync accessor) {
    _accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));
  }

  public bool TryGet(string key, IBufferWriter<byte> destination) =>
    _accessor.TryGetAsync(key, destination, CancellationToken.None).GetAwaiter().GetResult();

  private readonly ITryGetEntryAsByteBufferWriterAsync _accessor;
}
