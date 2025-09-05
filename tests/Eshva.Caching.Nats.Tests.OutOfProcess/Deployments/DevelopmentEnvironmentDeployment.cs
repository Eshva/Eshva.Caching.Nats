using System.Threading.Tasks;
using Eshva.Common.Testing;
using NATS.Client.Core;
using NATS.Client.ObjectStore;
using NATS.Net;

namespace Eshva.Caching.Nats.Tests.OutOfProcess.Deployments;

internal sealed class DevelopmentEnvironmentDeployment : IDeployment {
  public DevelopmentEnvironmentDeployment() {
    _suffix = Randomize.String(length: 5);
    var hostNetworkClientPort = NetworkTools.GetFreeTcpPort();
    var hostNetworkHttpManagementPort = NetworkTools.GetFreeTcpPort((ushort)(hostNetworkClientPort + 1));
    _natsServerDeployment = new NatsServerDeployment(_suffix, hostNetworkClientPort, hostNetworkHttpManagementPort);
  }

  public INatsConnection NatsConnection { get; private set; }

  public INatsObjContext ObjectStoreContext { get; private set; }

  public async Task Build() => await _natsServerDeployment.Build();

  public async Task Start() {
    await _natsServerDeployment.Start();
    var natsOptions = new NatsOpts {
      Url = _natsServerDeployment.ConnectionString,
      Name = $"NatsCache out of process tests with suffix '{_suffix}'"
    };
    NatsConnection = new NatsConnection(natsOptions);
    await NatsConnection.ConnectAsync();
    ObjectStoreContext = NatsConnection.CreateObjectStoreContext();
  }

  public async ValueTask DisposeAsync() {
    await NatsConnection.DisposeAsync();
    await _natsServerDeployment.DisposeAsync();
  }

  private readonly NatsServerDeployment _natsServerDeployment;
  private readonly string _suffix;
}
