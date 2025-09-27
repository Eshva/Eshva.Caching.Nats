using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;

namespace Eshva.Caching.Nats.ObjectStore.DataAccess.Benchmarks;

public sealed class BenchmarkDefaultConfig : ManualConfig {
  public BenchmarkDefaultConfig() {
    AddJob(
      new Job()
        .WithId(".NET 9")
        .WithRuntime(CoreRuntime.Core90));
  }
}
