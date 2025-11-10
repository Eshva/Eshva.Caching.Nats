using System.Text.RegularExpressions;
using Eshva.Caching.Nats.Tests.OutOfProcess.Common;
using Eshva.Common.Testing;
using Eshva.Testing.OutOfProcessDeployments.Nats;
using Reqnroll;
using Xunit.Abstractions;

namespace Eshva.Caching.Nats.Tests.OutOfProcess;

[Binding]
public sealed class Hooks {
  [BeforeTestRun]
  public static async Task StartTestDeployment() {
    _suffix = Randomize.String(length: 5);

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

    var entryValueKeyValueBucketName = Regex.Replace($"{scenarioContext.ScenarioInfo.Title}-Values", "[^a-zA-Z0-9]", "-");
    var entryMetadataKeyValueBucketName = Regex.Replace($"{scenarioContext.ScenarioInfo.Title}-Metadata", "[^a-zA-Z0-9]", "-");
    var entryValueKeyValueStore = await _deployment.KeyValueContext.CreateStoreAsync(entryValueKeyValueBucketName);
    var entryMetadataKeyValueStore = await _deployment.KeyValueContext.CreateStoreAsync(entryMetadataKeyValueBucketName);

    var cachesContext = new CachesContext(
      objectStore,
      entryValueKeyValueStore,
      entryMetadataKeyValueStore,
      logger);
    scenarioContext.ScenarioContainer.RegisterInstanceAs(cachesContext);
  }

  private static NatsServerDeployment? _deployment;
  private static ushort _hostNetworkHttpManagementPort;
  private static ushort _hostNetworkClientPort;
  private static string _suffix = string.Empty;
}
