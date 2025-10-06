namespace Eshva.Caching.Nats.Tests.OutOfProcessDeployments;

public interface IOutOfProcessDeployment : IAsyncDisposable {
  Task Build();

  Task Start();
}
