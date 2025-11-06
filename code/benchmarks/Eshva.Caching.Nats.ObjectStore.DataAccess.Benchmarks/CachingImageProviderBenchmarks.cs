using System.Diagnostics;
using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using Eshva.Caching.Nats.Tests.OutOfProcessDeployments;
using Eshva.Caching.Nats.TestWebApp;
using Eshva.Common.Testing;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc.Testing;
using Perfolizer.Horology;
using Perfolizer.Metrology;

namespace Eshva.Caching.Nats.ObjectStore.DataAccess.Benchmarks;

[Config(typeof(CachingImageProviderBenchmarksConfig))]
public class CachingImageProviderBenchmarks {
  [Params(
    EntrySizes.Bytes016,
    EntrySizes.Bytes256,
    EntrySizes.KiB001,
    EntrySizes.KiB064,
    EntrySizes.KiB256,
    EntrySizes.MiB001,
    EntrySizes.MiB005,
    EntrySizes.MiB050)]
  public int EntrySize { get; [UsedImplicitly] set; }

  [Benchmark(Description = "try-get-async")]
  public async Task<bool> TryGetAsyncWithByteStream() {
    Debug.Assert(_webAppClient != null, nameof(_webAppClient) + " != null");
    var response = await _webAppClient.GetAsync($"/object-store/try-get-async/{EntryName}");
    // var contentHash = await SHA256.HashDataAsync(await response.Content.ReadAsStreamAsync());
    // if (!response.IsSuccessStatusCode || !contentHash.SequenceEqual(_imageHash)) throw new Exception();
    return response.IsSuccessStatusCode;
  }

  [Benchmark(Description = "get-async", Baseline = true)]
  public async Task<bool> GetAsyncWithByteStream() {
    Debug.Assert(_webAppClient != null, nameof(_webAppClient) + " != null");
    var response = await _webAppClient.GetAsync($"/object-store/get-async/{EntryName}");
    // var contentHash = await SHA256.HashDataAsync(await response.Content.ReadAsStreamAsync());
    // if (!response.IsSuccessStatusCode || !contentHash.SequenceEqual(_imageHash)) throw new Exception();
    return response.IsSuccessStatusCode;
  }

  [GlobalSetup]
  public async Task SetupDeployment() {
    var hostNetworkClientPort = NetworkTools.GetFreeTcpPort();
    var hostNetworkHttpManagementPort = NetworkTools.GetFreeTcpPort((ushort)(hostNetworkClientPort + 1));
    _deployment = NatsBasedCachingTestsDeployment
      .Named($"{nameof(CachingImageProviderBenchmarks)}-{EntrySize}")
      .WithNatsServerInContainer(
        NatsServerDeployment
          .Named($"{nameof(CachingImageProviderBenchmarks)}-{EntrySize}")
          .FromImageTag("nats:2.11")
          .WithContainerName($"caching-image-provider-benchmarks-{EntrySize}")
          .WithHostNetworkClientPort(hostNetworkClientPort)
          .WithHostNetworkHttpManagementPort(hostNetworkHttpManagementPort)
          .EnabledJetStream()
          .CreateBucket(
            ObjectStoreBucket
              .Named($"{nameof(CachingImageProviderBenchmarks)}-{EntrySize}")
              .OfSize(100 * 1024 * 1024)));
    await _deployment.Build();
    await _deployment.Start();

    await CreateCacheBucket();
    SetupWebAppTestee();
  }

  [IterationSetup]
  public void AddImageIntoCache() {
    var bucket = _deployment!.NatsServer.ObjectStoreContext.GetObjectStoreAsync("images").AsTask().GetAwaiter().GetResult();
    var image = new byte[EntrySize];
    Random.Shared.NextBytes(image);
    _imageHash = SHA256.HashData(image);
    bucket.PutAsync(EntryName, image).AsTask().GetAwaiter().GetResult();
  }

  [GlobalCleanup]
  public async Task CleanupDeployment() {
    if (_deployment != null) {
      await _deployment.DisposeAsync();
      _deployment = null;
    }

    if (_webAppClient != null) {
      _webAppClient.Dispose();
      _webAppClient = null;
    }

    if (_webAppFactory != null) {
      await _webAppFactory.DisposeAsync();
      _webAppFactory = null;
    }
  }

  private async Task CreateCacheBucket() =>
    await _deployment!.NatsServer.ObjectStoreContext.CreateObjectStoreAsync("images");

  private void SetupWebAppTestee() {
    Environment.SetEnvironmentVariable(
      "BENCHMARKS_CacheNatsServer__NatsServerConnectionString",
      _deployment!.NatsServer.Connection.Opts.Url);

    _webAppFactory = new WebApplicationFactory<AssemblyTag>();
    _webAppClient = _webAppFactory.CreateClient();
  }

  private NatsBasedCachingTestsDeployment? _deployment;
  private byte[] _imageHash = [];
  private HttpClient? _webAppClient;
  private WebApplicationFactory<AssemblyTag>? _webAppFactory;
  private const string EntryName = "benchmark-entry";
}

public sealed class CachingImageProviderBenchmarksConfig : ManualConfig {
  public CachingImageProviderBenchmarksConfig() {
    AddDiagnoser(MemoryDiagnoser.Default);
    AddJob(
      new Job()
        .WithId(".NET 9")
        .WithRuntime(CoreRuntime.Core90)
        .WithIterationCount(count: 100)
        .WithAnalyzeLaunchVariance(value: true)
        .WithGcConcurrent(value: true)
        .WithGcServer(value: true)
        .WithLaunchCount(count: 1));
    AddColumn(StatisticColumn.P50, StatisticColumn.P95);
    HideColumns(
      "Error",
      "StdDev",
      "Median");
    SummaryStyle = SummaryStyle.Default
      .WithTimeUnit(TimeUnit.Microsecond)
      .WithSizeUnit(SizeUnit.KB);
  }
}
