using System.Buffers;
using CommunityToolkit.HighPerformance;
using NATS.Client.ObjectStore;

namespace Eshva.Caching.Nats.ObjectStore.DataAccess.Benchmarks.testees;

public class TryGetAsyncWithByteStream {
  public TryGetAsyncWithByteStream(INatsObjStore cacheBucket) {
    _cacheBucket = cacheBucket ?? throw new ArgumentNullException(nameof(cacheBucket));
  }

  public async ValueTask<bool> TryGetAsync(string key, IBufferWriter<byte> destination, CancellationToken token = new()) {
    await _cacheBucket.GetAsync(
      key,
      destination.AsStream(),
      leaveOpen: true,
      token);
    return true;
  }

  private readonly INatsObjStore _cacheBucket;
}
