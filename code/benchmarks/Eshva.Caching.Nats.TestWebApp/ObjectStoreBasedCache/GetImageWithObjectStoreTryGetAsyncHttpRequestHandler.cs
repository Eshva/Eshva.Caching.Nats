using CommunityToolkit.HighPerformance;
using CommunityToolkit.HighPerformance.Buffers;
using Microsoft.Extensions.Caching.Distributed;

namespace Eshva.Caching.Nats.TestWebApp.ObjectStoreBasedCache;

public class GetImageWithObjectStoreTryGetAsyncHttpRequestHandler {
  public GetImageWithObjectStoreTryGetAsyncHttpRequestHandler(
    IBufferDistributedCache cache,
    ILogger<GetImageWithObjectStoreTryGetAsyncHttpRequestHandler> logger) {
    _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
  }

  public async Task<IResult> Handle(string imageName) {
    // TODO: Specify etag in FileResult.
    var writer = new ArrayPoolBufferWriter<byte>();
    if (!await _cache.TryGetAsync(imageName, writer)) return Results.NotFound();

    var memory = writer.WrittenMemory;

    _logger.LogInformation("Found image {ImageName} with size {SizeInBytes} bytes", imageName, memory.Length);

    var result = Results.File(memory.AsStream(), @"image/avif", imageName);
    return result;
  }

  private readonly IBufferDistributedCache _cache;
  private readonly ILogger<GetImageWithObjectStoreTryGetAsyncHttpRequestHandler> _logger;
}
