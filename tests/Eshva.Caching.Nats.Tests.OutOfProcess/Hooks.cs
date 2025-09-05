using System.Threading.Tasks;
using Eshva.Caching.Nats.Tests.OutOfProcess.Common;
using Eshva.Caching.Nats.Tests.OutOfProcess.Deployments;
using Reqnroll;

namespace Eshva.Caching.Nats.Tests.OutOfProcess;

[Binding]
public sealed class Hooks {
  [BeforeTestRun]
  public static async Task StartTestDeployment() {
    _testDeployment = new DevelopmentEnvironmentDeployment();
    await _testDeployment.Build();
    await _testDeployment.Start();
  }

  [AfterTestRun]
  public static async Task StopTestDeployment() => await _testDeployment.DisposeAsync();

  [BeforeScenario]
  public async Task CreateCaches(ScenarioContext scenarioContext) {
    var bucketName = scenarioContext.ScenarioInfo.Title.Replace(oldChar: ' ', newChar: '-');
    var objectStore = await _testDeployment.ObjectStoreContext.CreateObjectStoreAsync(bucketName);
    var cachesContext = new CachesContext(objectStore);
    scenarioContext.ScenarioContainer.RegisterInstanceAs(cachesContext);
  }

  private static DevelopmentEnvironmentDeployment _testDeployment = null!;
}
