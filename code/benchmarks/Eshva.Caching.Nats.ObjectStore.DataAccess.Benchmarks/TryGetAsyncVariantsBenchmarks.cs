using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using CommunityToolkit.HighPerformance.Buffers;
using Eshva.Caching.Nats.Benchmarks.Tools;
using Eshva.Caching.Nats.ObjectStore.DataAccess.Benchmarks.testees;
using JetBrains.Annotations;
using NATS.Client.ObjectStore;
using NATS.Client.ObjectStore.Models;
using Perfolizer.Horology;
using Perfolizer.Metrology;

namespace Eshva.Caching.Nats.ObjectStore.DataAccess.Benchmarks;

[Config(typeof(TryGetAsyncVariantsBenchmarksConfig))]
public class TryGetAsyncVariantsBenchmarks {
  public TryGetAsyncVariantsBenchmarks() {
    var bucket = new InMemoryTestBuket("benchmark-bucket");
    _bucket = bucket;
    _bucketBackend = bucket;
  }

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

  [GlobalSetup]
  public void CreateEntry() {
    var image = new byte[EntrySize];
    Random.Shared.NextBytes(image);
    _imageHash = SHA256.HashData(image);
    _bucketBackend.PutEntry(EntryName, image);
    _bucketBackend.PutMetadata(EntryName, new ObjectMetadata { Name = EntryName, Size = EntrySize });
    _memory = new Memory<byte>(new byte[EntrySize]);
  }

  [Benchmark(Description = "without-intermediate")]
  public async Task<bool> TryGetAsyncWithByteStream() {
    var sut = new TryGetAsyncWithoutIntermediateStream(_bucket);
    var isSuccessful = await sut.TryGetAsync(EntryName, new MemoryBufferWriter<byte>(_memory));
    if (!isSuccessful || !_imageHash.SequenceEqual(SHA256.HashData(_memory.ToArray()))) throw new Exception();
    return isSuccessful;
  }

  [Benchmark(Description = "with-intermediate", Baseline = true)]
  public async Task<bool> TryGetAsyncWithByteSequence() {
    var sut = new TryGetAsyncWithIntermediateStream(_bucket);
    var isSuccessful = await sut.TryGetAsync(EntryName, new MemoryBufferWriter<byte>(_memory));
    if (!isSuccessful || !_imageHash.SequenceEqual(SHA256.HashData(_memory.ToArray()))) throw new Exception();
    return isSuccessful;
  }

  private readonly INatsObjStore _bucket;
  private readonly IBucketBackend _bucketBackend;
  private byte[] _imageHash = [];
  private Memory<byte> _memory;
  private const string EntryName = "benchmark-entry";
}

public sealed class TryGetAsyncVariantsBenchmarksConfig : ManualConfig {
  public TryGetAsyncVariantsBenchmarksConfig() {
    AddDiagnoser(MemoryDiagnoser.Default);
    AddJob(
      new Job()
        .WithId(".NET 9")
        .WithRuntime(CoreRuntime.Core90));
    AddColumn(StatisticColumn.P50, StatisticColumn.P95);
    HideColumns(
      "Error",
      "StdDev",
      "Median",
      "RatioSD");
    SummaryStyle = SummaryStyle.Default
      .WithTimeUnit(TimeUnit.Microsecond)
      .WithSizeUnit(SizeUnit.KB);
  }
}
