namespace Eshva.Tests.Deployments;

public interface IOutOfProcessDeployment {
  Task Build();

  Task Start();
}
