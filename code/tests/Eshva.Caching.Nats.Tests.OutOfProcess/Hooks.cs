using System.Text.RegularExpressions;
using Eshva.Caching.Nats.Tests.OutOfProcess.Common;
using Eshva.Caching.Nats.Tests.OutOfProcessDeployments;
using Eshva.Common.Testing;
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
    _deployment = NatsBasedCachingTestsDeployment
      .Named($"NatsCache out of process tests with suffix '{_suffix}'")
      .WithNatsServerInContainer(
        NatsServerDeployment
          .Named($"NatsCache out of process tests with suffix '{_suffix}'")
          .FromImageTag("nats:2.11")
          .WithContainerName("object-store-cache-tests")
          .WithHostNetworkClientPort(_hostNetworkClientPort)
          .WithHostNetworkHttpManagementPort(_hostNetworkHttpManagementPort)
          .EnabledJetStream()
          .EnableDebugOutput()
          .EnableTraceOutput()
          .CreateBucket(
            ObjectStoreBucket
              .Named("object-store-cache-tests-entries")
              .OfSize(100 * 1024 * 1024)));
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

    var bucketName = Regex.Replace(scenarioContext.ScenarioInfo.Title, "[^a-zA-Z0-9]", "-");
    var objectStore = await _deployment.NatsServer.ObjectStoreContext.CreateObjectStoreAsync(bucketName);
    var cachesContext = new CachesContext(objectStore, logger);
    scenarioContext.ScenarioContainer.RegisterInstanceAs(cachesContext);
  }

  private static NatsBasedCachingTestsDeployment? _deployment;
  private static ushort _hostNetworkHttpManagementPort;
  private static ushort _hostNetworkClientPort;
  private static string _suffix = string.Empty;
}
