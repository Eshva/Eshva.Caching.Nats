using System.Buffers;

namespace Eshva.Caching.Abstractions;

public interface ITryGetEntryAsByteBufferWriter {
  bool TryGet(string key, IBufferWriter<byte> destination);
}
