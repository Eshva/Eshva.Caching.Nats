using System.Buffers;
using CommunityToolkit.HighPerformance;
using NATS.Client.ObjectStore;

namespace Eshva.Caching.Nats.ObjectStore.DataAccess.Benchmarks.testees;

public class TryGetAsyncWithoutIntermediateStream {
  public TryGetAsyncWithoutIntermediateStream(INatsObjStore cacheBucket) {
    _cacheBucket = cacheBucket ?? throw new ArgumentNullException(nameof(cacheBucket));
  }

  public async ValueTask<bool> TryGetAsync(string key, IBufferWriter<byte> destination) {
    var objectMetadata = await _cacheBucket.GetAsync(
        key,
        destination.AsStream(),
        leaveOpen: true,
        CancellationToken.None)
      .ConfigureAwait(continueOnCapturedContext: false);
    return true;
  }

  private readonly INatsObjStore _cacheBucket;
}
