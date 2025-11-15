using System.Buffers;
using CommunityToolkit.HighPerformance;
using Eshva.Caching.Abstractions.Distributed;
using NATS.Client.Core;

namespace Eshva.Caching.Nats;

/// <summary>
/// Cache entry expiry binary serializer.
/// </summary>
public class CacheEntryExpiryBinarySerializer : INatsSerializer<CacheEntryExpiry> {
  /// <inheritdoc/>
  public void Serialize(IBufferWriter<byte> bufferWriter, CacheEntryExpiry value) {
    bufferWriter.Write(value.ExpiresAtUtc.Ticks);
    bufferWriter.Write(value.AbsoluteExpiryAtUtc?.Ticks ?? -1);
    bufferWriter.Write(value.SlidingExpiryInterval?.Ticks ?? -1);
  }

  /// <inheritdoc/>
  public CacheEntryExpiry Deserialize(in ReadOnlySequence<byte> buffer) {
    var reader = new SequenceReader<byte>(buffer);
    return reader.TryReadLittleEndian(out long expiresAtUtc)
           && reader.TryReadLittleEndian(out long absoluteExpiryAtUtc)
           && reader.TryReadLittleEndian(out long slidingExpiryInterval)
      ? new CacheEntryExpiry(
        new DateTimeOffset(expiresAtUtc, TimeSpan.Zero),
        absoluteExpiryAtUtc >= 0 ? new DateTimeOffset(absoluteExpiryAtUtc, TimeSpan.Zero) : null,
        slidingExpiryInterval >= 0 ? new TimeSpan(slidingExpiryInterval) : null)
      : throw new ArgumentException("Can't deserialize cache entry expiry.", nameof(buffer));
  }

  /// <inheritdoc/>
  public INatsSerializer<CacheEntryExpiry> CombineWith(INatsSerializer<CacheEntryExpiry> next) => throw new NotImplementedException();
}
