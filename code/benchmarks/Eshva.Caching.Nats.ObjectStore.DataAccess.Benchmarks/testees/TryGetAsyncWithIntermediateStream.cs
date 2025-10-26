using System.Buffers;
using CommunityToolkit.HighPerformance;
using NATS.Client.ObjectStore;

namespace Eshva.Caching.Nats.ObjectStore.DataAccess.Benchmarks.testees;

public class TryGetAsyncWithIntermediateStream {
  public TryGetAsyncWithIntermediateStream(INatsObjStore cacheBucket) {
    _cacheBucket = cacheBucket ?? throw new ArgumentNullException(nameof(cacheBucket));
  }

  public async ValueTask<bool> TryGetAsync(string key, IBufferWriter<byte> destination) {
    var stream = new MemoryStream();
    var objectMetadata = await _cacheBucket.GetAsync(
        key,
        stream,
        leaveOpen: true,
        CancellationToken.None)
      .ConfigureAwait(continueOnCapturedContext: false);
    stream.Seek(offset: 0, SeekOrigin.Begin);
    await stream.CopyToAsync(destination.AsStream(), CancellationToken.None).ConfigureAwait(continueOnCapturedContext: false);
    return true;
  }

  private readonly INatsObjStore _cacheBucket;
}
