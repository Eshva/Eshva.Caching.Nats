using System.Buffers;
using NATS.Client.ObjectStore;

namespace Eshva.Caching.Nats.ObjectStore.DataAccess.Benchmarks.testees;

public class TryGetAsyncWithByteSequence {
  public TryGetAsyncWithByteSequence(INatsObjStore cacheBucket) {
    _cacheBucket = cacheBucket ?? throw new ArgumentNullException(nameof(cacheBucket));
  }

  public async ValueTask<bool> TryGetAsync(string key, IBufferWriter<byte> destination, CancellationToken token = new()) {
    destination.Write(await _cacheBucket.GetBytesAsync(key, token));
    return true;
  }

  private readonly INatsObjStore _cacheBucket;
}
