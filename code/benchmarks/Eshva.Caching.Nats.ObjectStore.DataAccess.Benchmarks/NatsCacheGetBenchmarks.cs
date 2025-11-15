using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using Eshva.Caching.Abstractions.Distributed;
using Eshva.Caching.Nats.Benchmarks.Tools;
using Eshva.Caching.Nats.TestWebApp;
using Eshva.Testing.OutOfProcessDeployments.Nats;
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
    if (ShouldSimulateDataRead) {
      var contentHash = await SHA256.HashDataAsync(destination);
      if (!contentHash.SequenceEqual(_imageHash)) throw new Exception("Read data differed from original.");
    }

    return result && destination.Length == EntrySize;
  }

  [Benchmark(Description = "obj-get")]
  public async Task<bool> ObjectStoreGetAsyncWithByteStream() {
    var bytes = await _objectStoreBasedTestee.GetAsync(EntryName);
    if (ShouldSimulateDataRead) {
      var contentHash = await SHA256.HashDataAsync(new MemoryStream(bytes!));
      if (!contentHash.SequenceEqual(_imageHash)) throw new Exception("Read data differed from original.");
    }

    return bytes != null && bytes.Length == EntrySize;
  }

  [Benchmark(Description = "kv-try-get")]
  public async Task<bool> KeyValueTryGetAsyncWithByteStream() {
    var destination = StreamManager.GetStream();
    var result = await _keyValueBasedTestee.TryGetAsync(EntryName, destination);
    destination.Position = 0;
    if (ShouldSimulateDataRead) {
      var contentHash = await SHA256.HashDataAsync(destination);
      if (!contentHash.SequenceEqual(_imageHash)) throw new Exception("Read data differed from original.");
    }

    return result && destination.Length == EntrySize;
  }

  [Benchmark(Description = "kv-get", Baseline = true)]
  public async Task<bool> KeyValueGetAsyncWithByteStream() {
    var bytes = await _keyValueBasedTestee.GetAsync(EntryName);
    if (ShouldSimulateDataRead) {
      var contentHash = await SHA256.HashDataAsync(new MemoryStream(bytes!));
      if (!contentHash.SequenceEqual(_imageHash)) throw new Exception("Read data differed from original.");
    }

    return bytes != null && bytes.Length == EntrySize;
  }

  [GlobalSetup]
  public async Task SetupDeployment() {
    var hostNetworkClientPort = NetworkTools.GetFreeTcpPort();
    var hostNetworkHttpManagementPort = NetworkTools.GetFreeTcpPort((ushort)(hostNetworkClientPort + 1));
    _deployment = NatsServerDeployment
      .Named($"{nameof(NatsCacheGetBenchmarks)}-{EntrySize}")
      .FromImageTag("nats:2.11")
      .WithContainerName($"nats-cache-get-benchmarks-{Random.Shared.Next()}")
      .WithHostNetworkClientPort(hostNetworkClientPort)
      .WithHostNetworkHttpManagementPort(hostNetworkHttpManagementPort)
      .EnabledJetStream();
    await _deployment.Build();
    await _deployment.Start();

    var natsClient = CreateNatsClient();
    _objectStoreBasedTestee = await CreateObjectStoreBasedTestee(natsClient);
    _keyValueBasedTestee = await CreateKeyValueBasedTestee(natsClient);
  }

  [IterationSetup]
  public void AddImageIntoCache() {
    var image = new byte[EntrySize];
    Random.Shared.NextBytes(image);
    _imageHash = SHA256.HashData(image);

    var bucket = _deployment!.ObjectStoreContext.GetObjectStoreAsync(ObjectStoreBucketName).AsTask().GetAwaiter().GetResult();
    bucket.PutAsync(EntryName, image).AsTask().GetAwaiter().GetResult();

    var entriesStore = _deployment!.KeyValueContext.GetStoreAsync(KeyValueStoreBucketName).AsTask().GetAwaiter().GetResult();
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

  private NatsConnection CreateNatsClient() => new(new NatsOpts { Url = _deployment!.Connection.Opts.Url });

  private async Task<NatsObjectStoreBasedCache> CreateObjectStoreBasedTestee(NatsConnection natsClient) {
    var cacheBucket = await natsClient.CreateObjectStoreContext().CreateObjectStoreAsync(ObjectStoreBucketName);
    var expiryCalculator = new CacheEntryExpiryCalculator(TimeSpan.FromMinutes(minutes: 1), TimeProvider.System);
    var cacheDatastore = new ObjectStoreBasedDatastore(cacheBucket, expiryCalculator);
    var cacheInvalidation = new ObjectStoreBasedCacheInvalidation(
      cacheBucket,
      TimeSpan.FromMinutes(minutes: 5),
      TimeSpan.FromMinutes(minutes: 4),
      expiryCalculator,
      TimeProvider.System);
    return new NatsObjectStoreBasedCache(cacheDatastore, cacheInvalidation);
  }

  private async Task<NatsKeyValueStoreBasedCache> CreateKeyValueBasedTestee(NatsConnection natsClient) {
    var entriesStore = await natsClient.CreateKeyValueStoreContext().CreateStoreAsync(KeyValueStoreBucketName);
    var expiryCalculator = new CacheEntryExpiryCalculator(TimeSpan.FromMinutes(minutes: 1), TimeProvider.System);
    var entryExpirySerializer = new CacheEntryExpiryBinarySerializer();
    var cacheDatastore = new KeyValueBasedDatastore(
      entriesStore,
      entryExpirySerializer,
      expiryCalculator);
    var cacheInvalidation = new KeyValueBasedCacheInvalidation(
      entriesStore,
      TimeSpan.FromMinutes(minutes: 5),
      TimeSpan.FromMinutes(minutes: 4),
      entryExpirySerializer,
      expiryCalculator,
      TimeProvider.System);
    return new NatsKeyValueStoreBasedCache(cacheDatastore, cacheInvalidation);
  }

  private NatsServerDeployment? _deployment;
  private byte[] _imageHash = [];
  private HttpClient? _webAppClient;
  private WebApplicationFactory<AssemblyTag>? _webAppFactory;
  private NatsObjectStoreBasedCache _objectStoreBasedTestee = null!;
  private NatsKeyValueStoreBasedCache _keyValueBasedTestee = null!;
  private const string MetadataSuffix = "-metadata";
  private const bool ShouldSimulateDataRead = true;
  private const string ObjectStoreBucketName = "images-object";
  private const string KeyValueStoreBucketName = "image-key-value";
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
