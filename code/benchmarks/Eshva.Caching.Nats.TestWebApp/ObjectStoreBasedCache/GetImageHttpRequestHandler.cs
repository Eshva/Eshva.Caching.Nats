using Microsoft.Extensions.Caching.Distributed;

namespace Eshva.Caching.Nats.TestWebApp.ObjectStoreBasedCache;

public class GetImageHttpRequestHandler {
  public GetImageHttpRequestHandler(IBufferDistributedCache cache) {
    _cache = cache ?? throw new ArgumentNullException(nameof(cache));
  }

  public Task<IResult> Handle(string imageName) => Task.FromResult(Results.File([]));

  private readonly IBufferDistributedCache _cache;
}
