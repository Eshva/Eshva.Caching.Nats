using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Eshva.Tests.Deployments;
using JetBrains.Annotations;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.ObjectStore;
using Testcontainers.Nats;

namespace Eshva.Caching.Nats.Tests.OutOfProcessDeployments;

[PublicAPI]
public partial class NatsServerDeployment(NatsServerDeployment.Settings settings) : IOutOfProcessDeployment {
  public INatsConnection Connection { get; private set; } = null!;

  public INatsJSContext JetStreamContext { get; private set; } = null!;

  public INatsObjContext ObjectStoreContext { get; private set; } = null!;

  public static Settings Named(string name) => new() { Name = name };

  public static implicit operator NatsServerDeployment(Settings settings) => new(settings);

  public virtual Task Build() {
    var builder = new ContainerBuilder()
      .WithImage(settings.ImageTag)
      .WithName(settings.ContainerName)
      .WithPortBinding(settings.HostNetworkClientPort, NatsBuilder.NatsClientPort)
      .WithCleanUp(cleanUp: true)
      .WithAutoRemove(autoRemove: true)
      .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("Server is ready"));

    if (settings.IsJetStreamEnabled) builder.WithCommand("--jetstream");
    if (settings.HostNetworkHttpManagementPort.HasValue) {
      builder.WithPortBinding(settings.HostNetworkHttpManagementPort.Value, NatsHttpManagementPort)
        .WithCommand("--http_port", NatsHttpManagementPort.ToString());
    }

    return Task.FromResult(_container = builder.Build());
  }

  public virtual async Task Start() {
    if (_container == null) throw new InvalidOperationException("NATS server deployment is not initialized.");

    await _container.StartAsync();
    await ConnectToNats();
    await CreateBuckets();
  }

  public async ValueTask DisposeAsync() {
    if (_container != null) await _container.DisposeAsync();
  }

  private async Task ConnectToNats() {
    Connection = new NatsConnection(
      new NatsOpts { Url = $"nats://localhost:{settings.HostNetworkClientPort}" });
    JetStreamContext = new NatsJSContext(Connection);
    ObjectStoreContext = new NatsObjContext(JetStreamContext);
    await Connection.ConnectAsync();
  }

  private async Task CreateBuckets() {
    foreach (var bucket in settings.Buckets) {
      await ObjectStoreContext.CreateObjectStoreAsync(
        new NatsObjConfig(settings.Name) { MaxBytes = bucket.BucketVolumeBytes });
    }
  }

  private IContainer _container = null!; // TODO: Add NoneContainer.
  public const ushort NatsClientPort = 4222;
  public const ushort NatsClusterRoutingPort = 6222;
  public const ushort NatsHttpManagementPort = 8222;
}
