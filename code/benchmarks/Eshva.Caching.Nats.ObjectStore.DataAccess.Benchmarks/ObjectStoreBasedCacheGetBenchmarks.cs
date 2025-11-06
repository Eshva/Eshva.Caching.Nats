using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;
using Eshva.Caching.Abstractions;
using Eshva.Caching.Nats.Tests.OutOfProcessDeployments;
using Eshva.Caching.Nats.TestWebApp;
using Eshva.Common.Testing;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.IO;
using NATS.Client.Core;
using NATS.Net;

namespace Eshva.Caching.Nats.ObjectStore.DataAccess.Benchmarks;

[Config(typeof(CachingImageProviderBenchmarksConfig))]
public class ObjectStoreBasedCacheGetBenchmarks {
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

  [Benchmark(Description = "try-get")]
  public async Task<bool> TryGetAsyncWithByteStream() {
    var destination = StreamManager.GetStream();
    var result = await _testee.TryGetAsync(EntryName, destination);
    // destination.Position = 0;
    // var contentHash = await SHA256.HashDataAsync(destination);
    // if (!contentHash.SequenceEqual(_imageHash)) throw new Exception($"Bytes: {destination.Length}");
    return result && destination.Length == EntrySize;
  }

  [Benchmark(Description = "get", Baseline = true)]
  public async Task<bool> GetAsyncWithByteStream() {
    var bytes = await _testee.GetAsync(EntryName);
    // var contentHash = await SHA256.HashDataAsync(new MemoryStream(bytes!));
    // if (!contentHash.SequenceEqual(_imageHash)) throw new Exception($"Bytes: {bytes!.Length}");
    return bytes != null && bytes.Length == EntrySize;
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
          .WithContainerName($"object-store-based-cache-get-benchmarks-{Random.Shared.Next().ToString()}")
          .WithHostNetworkClientPort(hostNetworkClientPort)
          .WithHostNetworkHttpManagementPort(hostNetworkHttpManagementPort)
          .EnabledJetStream()
          .CreateBucket(
            ObjectStoreBucket
              .Named(BucketName)
              .OfSize(100 * 1024 * 1024)));
    await _deployment.Build();
    await _deployment.Start();

    await CreateCacheBucket();
    _testee = await CreateTestee(_deployment.NatsServer.Connection.Opts.Url);
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

  private async Task<NatsObjectStoreBasedCache> CreateTestee(string natsServerUrl) {
    var natsClient = new NatsConnection(new NatsOpts { Url = natsServerUrl });
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

  private async Task CreateCacheBucket() =>
    await _deployment!.NatsServer.ObjectStoreContext.CreateObjectStoreAsync("images");

  private NatsBasedCachingTestsDeployment? _deployment;
  private byte[] _imageHash = [];
  private HttpClient? _webAppClient;
  private WebApplicationFactory<AssemblyTag>? _webAppFactory;
  private NatsObjectStoreBasedCache _testee = null!;

  private const string BucketName = "images";
  private const string EntryName = "benchmark-entry";
  private static readonly RecyclableMemoryStreamManager StreamManager = new();
}
