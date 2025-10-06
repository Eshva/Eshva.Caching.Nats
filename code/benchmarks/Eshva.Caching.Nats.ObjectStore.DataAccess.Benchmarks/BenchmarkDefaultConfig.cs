using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;

namespace Eshva.Caching.Nats.ObjectStore.DataAccess.Benchmarks;

public sealed class BenchmarkDefaultConfig : ManualConfig {
  public BenchmarkDefaultConfig() {
    AddJob(
      new Job()
        .WithId(".NET 9")
        .WithRuntime(CoreRuntime.Core90)
        .WithIterationCount(count: 25)
        .WithAnalyzeLaunchVariance(value: true)
        .WithGcConcurrent(value: true)
        .WithGcServer(value: true)
        .WithLaunchCount(count: 4));
    AddColumn(StatisticColumn.P50, StatisticColumn.P95);
    HideColumns(
      "Error",
      "StdDev",
      "Median",
      "RatioSD");
  }
}
