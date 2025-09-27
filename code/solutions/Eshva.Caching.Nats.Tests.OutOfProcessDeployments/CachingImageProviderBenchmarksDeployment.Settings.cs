namespace Eshva.Caching.Nats.Tests.OutOfProcessDeployments;

public partial class CachingImageProviderBenchmarksDeployment {
  public readonly record struct Configuration(string Name, NatsServerDeployment.Settings NatsServer) {
    public Configuration WithNatsServerInContainer(NatsServerDeployment.Settings natsServer) =>
      this with { NatsServer = natsServer };
  }
}
