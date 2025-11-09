using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using Eshva.Caching.Abstractions;
using Eshva.Caching.Nats.Tests.OutOfProcessDeployments;
using Eshva.Caching.Nats.TestWebApp;
using Eshva.Common.Testing;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.IO;
using NATS.Client.Core;
using NATS.Net;
using Perfolizer.Horology;
using Perfolizer.Metrology;

namespace Eshva.Caching.Nats.ObjectStore.DataAccess.Benchmarks;

[Config(typeof(NatsCacheGetBenchmarksConfig))]
public class NatsCacheGetBenchmarks {
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
    var destination = StreamManager.GetStream();
    var result = await _objectStoreBasedTestee.TryGetAsync(EntryName, destination);
    destination.Position = 0;
    if (ShouldCompareOriginalAndGottenData) {
      var contentHash = await SHA256.HashDataAsync(destination);
      if (!contentHash.SequenceEqual(_imageHash)) throw new Exception($"Bytes: {destination.Length}");
    }

    return result && destination.Length == EntrySize;
  }

  [Benchmark(Description = "obj-get")]
  public async Task<bool> ObjectStoreGetAsyncWithByteStream() {
    var bytes = await _objectStoreBasedTestee.GetAsync(EntryName);
    if (ShouldCompareOriginalAndGottenData) {
      var contentHash = await SHA256.HashDataAsync(new MemoryStream(bytes!));
      if (!contentHash.SequenceEqual(_imageHash)) throw new Exception($"Bytes: {bytes!.Length}");
    }

    return bytes != null && bytes.Length == EntrySize;
  }

  [Benchmark(Description = "kv-try-get")]
  public async Task<bool> KeyValueTryGetAsyncWithByteStream() {
    var destination = StreamManager.GetStream();
    var result = await _keyValueBasedTestee.TryGetAsync(EntryName, destination);
    destination.Position = 0;
    if (ShouldCompareOriginalAndGottenData) {
      var contentHash = await SHA256.HashDataAsync(destination);
      if (!contentHash.SequenceEqual(_imageHash)) throw new Exception($"Bytes: {destination.Length}");
    }

    return result && destination.Length == EntrySize;
  }

  [Benchmark(Description = "kv-get", Baseline = true)]
  public async Task<bool> KeyValueGetAsyncWithByteStream() {
    var bytes = await _keyValueBasedTestee.GetAsync(EntryName);
    if (ShouldCompareOriginalAndGottenData) {
      var contentHash = await SHA256.HashDataAsync(new MemoryStream(bytes!));
      if (!contentHash.SequenceEqual(_imageHash)) throw new Exception($"Bytes: {bytes!.Length}");
    }

    return bytes != null && bytes.Length == EntrySize;
  }

  [GlobalSetup]
  public async Task SetupDeployment() {
    var hostNetworkClientPort = NetworkTools.GetFreeTcpPort();
    var hostNetworkHttpManagementPort = NetworkTools.GetFreeTcpPort((ushort)(hostNetworkClientPort + 1));
    _deployment = NatsBasedCachingTestsDeployment
      .Named($"{nameof(CachingHttpApplicationBenchmarks)}-{EntrySize}")
      .WithNatsServerInContainer(
        NatsServerDeployment
          .Named($"{nameof(CachingHttpApplicationBenchmarks)}-{EntrySize}")
          .FromImageTag("nats:2.11")
          .WithContainerName($"nats-caches-get-benchmarks-{Random.Shared.Next().ToString()}")
          .WithHostNetworkClientPort(hostNetworkClientPort)
          .WithHostNetworkHttpManagementPort(hostNetworkHttpManagementPort)
          .EnabledJetStream());
    await _deployment.Build();
    await _deployment.Start();

    await CreateCacheBucket();
    var natsClient = CreateNatsClient();
    _objectStoreBasedTestee = await CreateObjectStoreBasedTestee(natsClient);
    _keyValueBasedTestee = await CreateKeyValueBasedTestee(natsClient);
  }

  [IterationSetup]
  public void AddImageIntoCache() {
    var image = new byte[EntrySize];
    Random.Shared.NextBytes(image);
    _imageHash = SHA256.HashData(image);

    var bucket = _deployment!.NatsServer.ObjectStoreContext.GetObjectStoreAsync(BucketName).AsTask().GetAwaiter().GetResult();
    bucket.PutAsync(EntryName, image).AsTask().GetAwaiter().GetResult();

    var valueStore = _deployment!.NatsServer.KeyValueContext.GetStoreAsync(ValueStoreName).AsTask().GetAwaiter().GetResult();
    var metadataStore = _deployment!.NatsServer.KeyValueContext.GetStoreAsync(MetadataStoreName).AsTask().GetAwaiter().GetResult();
    valueStore.PutAsync(EntryName, image).AsTask().GetAwaiter().GetResult();
    metadataStore.PutAsync(
        EntryName,
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

  private NatsConnection CreateNatsClient() => new(new NatsOpts { Url = _deployment!.NatsServer.Connection.Opts.Url });

  private async Task<NatsObjectStoreBasedCache> CreateObjectStoreBasedTestee(NatsConnection natsClient) {
    var cacheBucket = await natsClient.CreateObjectStoreContext().GetObjectStoreAsync(BucketName);
    var expiryCalculator = new CacheEntryExpiryCalculator(TimeSpan.FromMinutes(minutes: 1), TimeProvider.System);
    var cacheDatastore = new ObjectStoreBasedDatastore(cacheBucket, expiryCalculator);
    var cacheInvalidation = new ObjectStoreBasedCacheInvalidation(
      cacheBucket,
      TimeSpan.FromMinutes(minutes: 5),
      expiryCalculator,
      TimeProvider.System);
    return new NatsObjectStoreBasedCache(cacheDatastore, cacheInvalidation);
  }

  private async Task<NatsKeyValueStoreBasedCache> CreateKeyValueBasedTestee(NatsConnection natsClient) {
    var metadataStore = await natsClient.CreateKeyValueStoreContext().CreateStoreAsync(MetadataStoreName);
    var valueStore = await natsClient.CreateKeyValueStoreContext().CreateStoreAsync(ValueStoreName);
    var expiryCalculator = new CacheEntryExpiryCalculator(TimeSpan.FromMinutes(minutes: 1), TimeProvider.System);
    var entryExpirySerializer = new CacheEntryExpiryBinarySerializer();
    var cacheDatastore = new KeyValueBasedDatastore(
      valueStore,
      metadataStore,
      entryExpirySerializer,
      expiryCalculator);
    var cacheInvalidation = new KeyValueBasedCacheInvalidation(
      valueStore,
      metadataStore,
      TimeSpan.FromMinutes(minutes: 5),
      entryExpirySerializer,
      expiryCalculator,
      TimeProvider.System);
    return new NatsKeyValueStoreBasedCache(cacheDatastore, cacheInvalidation);
  }

  private async Task CreateCacheBucket() =>
    await _deployment!.NatsServer.ObjectStoreContext.CreateObjectStoreAsync("images");

  private NatsBasedCachingTestsDeployment? _deployment;
  private byte[] _imageHash = [];
  private HttpClient? _webAppClient;
  private WebApplicationFactory<AssemblyTag>? _webAppFactory;
  private NatsObjectStoreBasedCache _objectStoreBasedTestee = null!;
  private NatsKeyValueStoreBasedCache _keyValueBasedTestee = null!;

  private const bool ShouldCompareOriginalAndGottenData = true;
  private const string BucketName = "images";
  private const string MetadataStoreName = "image-metadata";
  private const string ValueStoreName = "image-values";
  private const string EntryName = "benchmark-entry";
  private static readonly RecyclableMemoryStreamManager StreamManager = new();
}

public sealed class NatsCacheGetBenchmarksConfig : ManualConfig {
  public NatsCacheGetBenchmarksConfig() {
    AddDiagnoser(MemoryDiagnoser.Default);
    AddJob(
      new Job()
        .WithId(".NET 9")
        .WithRuntime(CoreRuntime.Core90)
        .WithAnalyzeLaunchVariance(value: true)
        .WithGcConcurrent(value: true)
        .WithGcServer(value: true));
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
