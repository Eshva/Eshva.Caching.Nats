using Microsoft.Extensions.Caching.Distributed;

namespace Eshva.Caching.Nats.TestWebApp.ObjectStoreBasedCache;

public class GetImageWithGetAsyncHttpRequestHandler {
  public GetImageWithGetAsyncHttpRequestHandler(
    IBufferDistributedCache cache,
    ILogger<GetImageWithGetAsyncHttpRequestHandler> logger) {
    _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
  }

  public async Task<IResult> Handle(string imageName) {
    // TODO: Specify etag in FileResult.
    var bytes = await _cache.GetAsync(imageName);
    if (bytes == null) return Results.NotFound();

    _logger.LogInformation("Found image {ImageName} with size {SizeInBytes} bytes", imageName, bytes.Length);

    var result = Results.File(bytes, @"image/avif", imageName);
    return result;
  }

  private readonly IBufferDistributedCache _cache;
  private readonly ILogger<GetImageWithGetAsyncHttpRequestHandler> _logger;
}
