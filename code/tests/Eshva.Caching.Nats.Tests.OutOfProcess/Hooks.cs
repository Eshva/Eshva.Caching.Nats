using System.Text.RegularExpressions;
using Eshva.Caching.Nats.Tests.OutOfProcess.Common;
using Eshva.Caching.Nats.Tests.Tools;
using Eshva.Testing.OutOfProcessDeployments.Nats;
using Reqnroll;
using Xunit;

namespace Eshva.Caching.Nats.Tests.OutOfProcess;

[Binding]
public sealed class Hooks {
  [BeforeTestRun]
  public static async Task StartTestDeployment() {
    _suffix = Random.Shared.Next().ToString();

    _hostNetworkClientPort = NetworkTools.GetFreeTcpPort();
    _hostNetworkHttpManagementPort = NetworkTools.GetFreeTcpPort((ushort)(_hostNetworkClientPort + 1));
    _deployment = NatsServerDeployment
      .Named($"NatsCache out of process tests with suffix '{_suffix}'")
      .FromImageTag("nats:2.11")
      .WithContainerName($"nats-cache-tests-{_suffix}")
      .WithHostNetworkClientPort(_hostNetworkClientPort)
      .WithHostNetworkHttpManagementPort(_hostNetworkHttpManagementPort)
      .EnabledJetStream();
    await _deployment.Build();
    await _deployment.Start();
  }

  [AfterTestRun]
  public static async Task StopTestDeployment() {
    if (_deployment == null) return;

    await _deployment.DisposeAsync();
    _deployment = null;
  }

  [BeforeScenario]
  public async Task CreateCaches(ScenarioContext scenarioContext, ITestOutputHelper logger) {
    if (_deployment is null) {
      throw new InvalidOperationException("Cannot create a cache without environment deployment started.");
    }

    var objectStoreBucketName = Regex.Replace(scenarioContext.ScenarioInfo.Title, "[^a-zA-Z0-9]", "-");
    var objectStore = await _deployment.ObjectStoreContext.CreateObjectStoreAsync(objectStoreBucketName);

    var keyValueBucketName = Regex.Replace(scenarioContext.ScenarioInfo.Title, "[^a-zA-Z0-9]", "-");
    var entriesStore = await _deployment.KeyValueContext.CreateStoreAsync(keyValueBucketName);

    var cachesContext = new CachesContext(
      _deployment.Connection,
      objectStore,
      entriesStore,
      logger);
    scenarioContext.ScenarioContainer.RegisterInstanceAs(cachesContext);
  }

  private static NatsServerDeployment? _deployment;
  private static ushort _hostNetworkHttpManagementPort;
  private static ushort _hostNetworkClientPort;
  private static string _suffix = string.Empty;
}
