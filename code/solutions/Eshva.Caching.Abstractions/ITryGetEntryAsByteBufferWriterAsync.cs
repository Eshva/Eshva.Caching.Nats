using System.Buffers;

namespace Eshva.Caching.Abstractions;

public interface ITryGetEntryAsByteBufferWriterAsync {
  ValueTask<bool> TryGetAsync(string key, IBufferWriter<byte> destination, CancellationToken token = default);
}
