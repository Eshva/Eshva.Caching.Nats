using System.Text.RegularExpressions;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using JetBrains.Annotations;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.KeyValueStore;
using NATS.Client.ObjectStore;

namespace Eshva.Caching.Nats.Tests.OutOfProcessDeployments;

[PublicAPI]
public partial class NatsServerDeployment(NatsServerDeployment.Configuration configuration) : IOutOfProcessDeployment {
  public INatsConnection Connection { get; private set; } = null!;

  public INatsJSContext JetStreamContext { get; private set; } = null!;

  public INatsObjContext ObjectStoreContext { get; private set; } = null!;

  public INatsKVContext KeyValueContext { get; private set; } = null!;

  public static Configuration Named(string name) => new() { Name = name };

  public static implicit operator NatsServerDeployment(Configuration configuration) => new(configuration);

  public virtual Task Build() {
    var builder = new ContainerBuilder()
      .WithImage(configuration.ImageTag)
      .WithName(configuration.ContainerName)
      .WithPortBinding(configuration.HostNetworkClientPort, NatsClientPort)
      .WithCleanUp(cleanUp: true)
      .WithAutoRemove(autoRemove: true)
      .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("Server is ready"));

    builder = EnableJetStreamIfRequired(builder);
    builder = MapHttpManagementPortToHostIfRequired(builder);
    builder = EnableDebugOutputIfRequired(builder);
    builder = EnableTraceOutputIfRequired(builder);

    return Task.FromResult(_container = builder.Build());
  }

  public virtual async Task Start() {
    if (_container == null) throw new InvalidOperationException("NATS server deployment is not initialized.");

    await _container.StartAsync();
    await ConnectToNats();
    await CreateBuckets();
  }

  public async ValueTask DisposeAsync() {
    if (_container == null) return;

    await _container.DisposeAsync();
    _container = null;
  }

  private ContainerBuilder EnableDebugOutputIfRequired(ContainerBuilder builder) =>
    configuration.ShouldEnableDebugOutput ? builder.WithCommand("--debug") : builder;

  private ContainerBuilder EnableTraceOutputIfRequired(ContainerBuilder builder) =>
    configuration.ShouldEnableTraceOutput ? builder.WithCommand("--trace") : builder;

  private ContainerBuilder MapHttpManagementPortToHostIfRequired(ContainerBuilder builder) {
    if (configuration.HostNetworkHttpManagementPort.HasValue) {
      builder = builder.WithPortBinding(configuration.HostNetworkHttpManagementPort.Value, NatsHttpManagementPort)
        .WithCommand("--http_port", NatsHttpManagementPort.ToString());
    }

    return builder;
  }

  private ContainerBuilder EnableJetStreamIfRequired(ContainerBuilder builder) =>
    configuration.ShouldEnableJetStream ? builder.WithCommand("--jetstream") : builder;

  private async Task ConnectToNats() {
    Connection = new NatsConnection(
      new NatsOpts { Url = $"nats://localhost:{configuration.HostNetworkClientPort}" });
    await Connection.ConnectAsync();
    JetStreamContext = new NatsJSContext(Connection);
    ObjectStoreContext = new NatsObjContext(JetStreamContext);
    KeyValueContext = new NatsKVContext(JetStreamContext);
  }

  private async Task CreateBuckets() {
    if (configuration.Buckets.IsDefaultOrEmpty) return;

    foreach (var bucket in configuration.Buckets) {
      var bucketName = Regex.Replace(configuration.Name, "[^a-zA-Z0-9]", "-");
      await ObjectStoreContext.CreateObjectStoreAsync(
        new NatsObjConfig(bucketName) { MaxBytes = bucket.BucketVolumeBytes });
    }
  }

  private IContainer? _container; // TODO: Add NoneContainer.

  public const ushort NatsClientPort = 4222;
  public const ushort NatsClusterRoutingPort = 6222;
  public const ushort NatsHttpManagementPort = 8222;
}
