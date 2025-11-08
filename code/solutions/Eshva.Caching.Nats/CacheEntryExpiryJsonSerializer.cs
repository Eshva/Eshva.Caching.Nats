using System.Buffers;
using System.Text;
using System.Text.Json;
using Eshva.Caching.Abstractions;
using NATS.Client.Core;

namespace Eshva.Caching.Nats;

/// <summary>
/// Cache entry expiry JSON-serializer.
/// </summary>
public class CacheEntryExpiryJsonSerializer : INatsSerializer<CacheEntryExpiry> {
  /// <inheritdoc/>
  public void Serialize(IBufferWriter<byte> bufferWriter, CacheEntryExpiry value) =>
    bufferWriter.Write(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = false })));

  /// <inheritdoc/>
  public CacheEntryExpiry Deserialize(in ReadOnlySequence<byte> buffer) =>
    JsonSerializer.Deserialize<CacheEntryExpiry>(Encoding.UTF8.GetString(buffer.ToArray()));

  /// <inheritdoc/>
  public INatsSerializer<CacheEntryExpiry> CombineWith(INatsSerializer<CacheEntryExpiry> next) => throw new NotImplementedException();
}
