namespace Eshva.Tests.Deployments;

public interface IOutOfProcessDeployment : IAsyncDisposable {
  Task Build();

  Task Start();
}
