using System.Threading.Tasks;
using Testcontainers.Nats;

namespace Eshva.Caching.Nats.Tests.OutOfProcess.Deployments;

internal sealed class NatsServerDeployment : IDeployment {
  public NatsServerDeployment(string suffix, ushort hostNetworkClientPort, ushort hostNetworkHttpManagementPort) {
    _suffix = suffix;
    _hostNetworkClientPort = hostNetworkClientPort;
    _hostNetworkHttpManagementPort = hostNetworkHttpManagementPort;
  }

  public string ConnectionString => _container.GetConnectionString();

  public Task Build() =>
    Task.FromResult(
      _container = new NatsBuilder()
        .WithImage("nats:2.11")
        .WithName($"tests-nats-{_suffix}")
        .WithPortBinding(_hostNetworkClientPort, NatsBuilder.NatsClientPort)
        .WithPortBinding(_hostNetworkHttpManagementPort, NatsBuilder.NatsHttpManagementPort)
        .WithCleanUp(cleanUp: true)
        .WithAutoRemove(autoRemove: true)
        .Build());

  public async Task Start() => await _container.StartAsync();

  public async ValueTask DisposeAsync() => await _container.DisposeAsync();

  private readonly ushort _hostNetworkClientPort;
  private readonly ushort _hostNetworkHttpManagementPort;
  private readonly string _suffix;
  private NatsContainer _container;
}
