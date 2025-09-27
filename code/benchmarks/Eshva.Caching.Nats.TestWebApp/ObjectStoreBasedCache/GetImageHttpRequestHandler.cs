namespace Eshva.Caching.Nats.TestWebApp.ObjectStoreBasedCache;

public class GetImageHttpRequestHandler {
  public Task<IResult> Handle(string imageName) => Task.FromResult(Results.File([]));
}
