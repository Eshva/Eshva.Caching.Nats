using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using Eshva.Caching.Abstractions;
using Eshva.Caching.Nats.TestWebApp;
using Eshva.Common.Testing;
using Eshva.Testing.OutOfProcessDeployments.Nats;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc.Testing;
using Perfolizer.Horology;
using Perfolizer.Metrology;

namespace Eshva.Caching.Nats.ObjectStore.DataAccess.Benchmarks;

[Config(typeof(CachingHttpApplicationBenchmarksConfig))]
public class CachingHttpApplicationBenchmarks {
  [Params(
    EntrySizes.Bytes016,
    EntrySizes.Bytes256,
    EntrySizes.KiB001,
    EntrySizes.KiB064,
    EntrySizes.KiB256,
    EntrySizes.MB001)]
  public int EntrySize { get; [UsedImplicitly] set; }

  [Benchmark(Description = "obj-try-get")]
  public async Task<bool> ObjectStoreTryGetAsyncWithByteStream() {
    Debug.Assert(_webAppClient != null, nameof(_webAppClient) + " != null");
    var response = await _webAppClient.GetAsync($"/object-store/try-get-async/{EntryName}");
    if (ShouldSimulateDataReading) {
      var contentHash = await SHA256.HashDataAsync(await response.Content.ReadAsStreamAsync());
      if (!response.IsSuccessStatusCode) throw new Exception($"response.StatusCode: {response.StatusCode}.");
      if (!contentHash.SequenceEqual(_imageHash)) throw new Exception("Read data differed from original.");
    }

    return response.IsSuccessStatusCode;
  }

  [Benchmark(Description = "obj-get")]
  public async Task<bool> ObjectStoreGetAsyncWithByteStream() {
    Debug.Assert(_webAppClient != null, nameof(_webAppClient) + " != null");
    var response = await _webAppClient.GetAsync($"/object-store/get-async/{EntryName}");
    if (ShouldSimulateDataReading) {
      var contentHash = await SHA256.HashDataAsync(await response.Content.ReadAsStreamAsync());
      if (!response.IsSuccessStatusCode) throw new Exception($"response.StatusCode: {response.StatusCode}.");
      if (!contentHash.SequenceEqual(_imageHash)) throw new Exception("Read data differed from original.");
    }

    return response.IsSuccessStatusCode;
  }

  [Benchmark(Description = "kv-try-get")]
  public async Task<bool> TryGetAsyncWithByteStream1() {
    Debug.Assert(_webAppClient != null, nameof(_webAppClient) + " != null");
    var response = await _webAppClient.GetAsync($"/key-value/try-get-async/{EntryName}");
    if (ShouldSimulateDataReading) {
      var contentHash = await SHA256.HashDataAsync(await response.Content.ReadAsStreamAsync());
      if (!response.IsSuccessStatusCode) throw new Exception($"response.StatusCode: {response.StatusCode}.");
      if (!contentHash.SequenceEqual(_imageHash)) throw new Exception("Read data differed from original.");
    }

    return response.IsSuccessStatusCode;
  }

  [Benchmark(Description = "kv-get", Baseline = true)]
  public async Task<bool> GetAsyncWithByteStream1() {
    Debug.Assert(_webAppClient != null, nameof(_webAppClient) + " != null");
    var response = await _webAppClient.GetAsync($"/key-value/get-async/{EntryName}");
    if (ShouldSimulateDataReading) {
      var contentHash = await SHA256.HashDataAsync(await response.Content.ReadAsStreamAsync());
      if (!response.IsSuccessStatusCode) throw new Exception($"response.StatusCode: {response.StatusCode}.");
      if (!contentHash.SequenceEqual(_imageHash)) throw new Exception("Read data differed from original.");
    }

    return response.IsSuccessStatusCode;
  }

  [GlobalSetup]
  public async Task SetupDeployment() {
    var hostNetworkClientPort = NetworkTools.GetFreeTcpPort();
    var hostNetworkHttpManagementPort = NetworkTools.GetFreeTcpPort((ushort)(hostNetworkClientPort + 1));
    _deployment = NatsServerDeployment
      .Named($"{nameof(CachingHttpApplicationBenchmarks)}-{EntrySize}")
      .FromImageTag("nats:2.11")
      .WithContainerName($"caching-http-application-benchmarks-{EntrySize}")
      .WithHostNetworkClientPort(hostNetworkClientPort)
      .WithHostNetworkHttpManagementPort(hostNetworkHttpManagementPort)
      .EnabledJetStream();
    await _deployment.Build();
    await _deployment.Start();

    // await CreateCacheBucket();
    SetupWebAppTestee();
  }

  [IterationSetup]
  public void AddImageIntoCache() {
    var image = new byte[EntrySize];
    Random.Shared.NextBytes(image);
    _imageHash = SHA256.HashData(image);

    var bucket = _deployment!.ObjectStoreContext.CreateObjectStoreAsync(ObjectStoreBucketName).AsTask().GetAwaiter().GetResult();
    bucket.PutAsync(EntryName, image).AsTask().GetAwaiter().GetResult();

    var entriesStore = _deployment!.KeyValueContext.CreateStoreAsync(KeyValueStoreBucketName).AsTask().GetAwaiter().GetResult();
    entriesStore.PutAsync(EntryName, image).AsTask().GetAwaiter().GetResult();
    entriesStore.PutAsync(
        MakeMetadataKey(EntryName),
        new CacheEntryExpiry(DateTimeOffset.Now.AddDays(days: 1), AbsoluteExpiryAtUtc: null, TimeSpan.FromDays(days: 1)),
        new CacheEntryExpiryBinarySerializer())
      .AsTask()
      .GetAwaiter()
      .GetResult();
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

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private static string MakeMetadataKey(string key) => $"{key}{MetadataSuffix}";

  private void SetupWebAppTestee() {
    Environment.SetEnvironmentVariable(
      "BENCHMARKS_CacheNatsServer__NatsServerConnectionString",
      _deployment!.Connection.Opts.Url);

    _webAppFactory = new WebApplicationFactory<AssemblyTag>();
    _webAppClient = _webAppFactory.CreateClient();
  }

  private NatsServerDeployment? _deployment;
  private byte[] _imageHash = [];
  private HttpClient? _webAppClient;
  private WebApplicationFactory<AssemblyTag>? _webAppFactory;
  private const string MetadataSuffix = "-metadata";
  private const bool ShouldSimulateDataReading = true;
  private const string KeyValueStoreBucketName = "images-key-value";
  private const string EntryName = "benchmark-entry";
  private const string ObjectStoreBucketName = "images-object";
}

public sealed class CachingHttpApplicationBenchmarksConfig : ManualConfig {
  public CachingHttpApplicationBenchmarksConfig() {
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
      .WithTimeUnit(TimeUnit.Millisecond)
      .WithSizeUnit(SizeUnit.KB);
  }
}
