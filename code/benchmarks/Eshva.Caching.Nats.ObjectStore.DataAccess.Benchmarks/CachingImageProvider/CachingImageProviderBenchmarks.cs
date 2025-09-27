using BenchmarkDotNet.Attributes;
using Eshva.Caching.Nats.Tests.OutOfProcessDeployments;
using Eshva.Common.Testing;

namespace Eshva.Caching.Nats.ObjectStore.DataAccess.Benchmarks.CachingImageProvider;

[MemoryDiagnoser]
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
  public int EntrySize { get; set; }

  [GlobalSetup]
  public async Task SetupDeployment() {
    _hostNetworkClientPort = NetworkTools.GetFreeTcpPort();
    _hostNetworkHttpManagementPort = NetworkTools.GetFreeTcpPort((ushort)(_hostNetworkClientPort + 1));
    _deployment = CachingImageProviderBenchmarksDeployment
      .Named($"{nameof(CachingImageProviderBenchmarks)}-{EntrySize}")
      .WithNatsServerInContainer(
        NatsServerDeployment
          .Named($"{nameof(CachingImageProviderBenchmarks)}-{EntrySize}")
          .FromImageTag("nats:2.11")
          .WithContainerName($"caching-image-provider-benchmarks-{EntrySize}")
          .WithHostNetworkClientPort(_hostNetworkClientPort)
          .WithHostNetworkHttpManagementPort(_hostNetworkHttpManagementPort)
          .WithJetStreamEnabled()
          .CreateBucket(
            ObjectStoreBucket
              .Named($"{nameof(CachingImageProviderBenchmarks)}-{EntrySize}")
              .OfSize(100 * 1024 * 1024)));
    await _deployment.Build();
    await _deployment.Start();
  }

  [GlobalCleanup] public async Task CleanupDeployment() => await _deployment.DisposeAsync();

  [Benchmark(Description = "with a stream")]
  public Task<bool> TryGetAsyncWithByteStream() =>
    // var sut = new TryGetAsyncWithByteStream(_bucket);
    // return await sut.TryGetAsync(EntryName, new MemoryBufferWriter<byte>(_memory));
    Task.FromResult(result: true);

  [Benchmark(Description = "with an array", Baseline = true)]
  public Task<bool> TryGetAsyncWithByteSequence() =>
    // var sut = new TryGetAsyncWithByteSequence(_bucket);
    // return await sut.TryGetAsync(EntryName, new MemoryBufferWriter<byte>(_memory));
    Task.FromResult(result: true);

  private CachingImageProviderBenchmarksDeployment _deployment = null!;
  private ushort _hostNetworkClientPort;
  private ushort _hostNetworkHttpManagementPort;
}
