using System;
using System.Threading.Tasks;

namespace Eshva.Caching.Nats.Tests.OutOfProcess.Deployments;

internal interface IDeployment : IAsyncDisposable {
  Task Build();

  Task Start();
}
