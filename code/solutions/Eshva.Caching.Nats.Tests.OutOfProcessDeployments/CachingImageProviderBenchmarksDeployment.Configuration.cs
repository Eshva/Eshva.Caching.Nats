namespace Eshva.Caching.Nats.Tests.OutOfProcessDeployments;

public partial class CachingImageProviderBenchmarksDeployment {
  public readonly record struct Configuration(string Name, NatsServerDeployment.Configuration NatsServer) {
    public Configuration WithNatsServerInContainer(NatsServerDeployment.Configuration natsServer) =>
      this with { NatsServer = natsServer };
  }
}
