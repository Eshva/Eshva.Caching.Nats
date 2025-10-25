using BenchmarkDotNet.Attributes;
using CommunityToolkit.HighPerformance.Buffers;
using Eshva.Caching.Nats.Benchmarks.Tools;
using Eshva.Caching.Nats.ObjectStore.DataAccess.Benchmarks.testees;
using NATS.Client.ObjectStore;
using NATS.Client.ObjectStore.Models;

namespace Eshva.Caching.Nats.ObjectStore.DataAccess.Benchmarks;

[MemoryDiagnoser]
// ReSharper disable once InconsistentNaming
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
  public int EntrySize { get; set; }

  [GlobalSetup]
  public void CreateEntry() {
    _bucketBackend.PutEntry(EntryName, new byte[EntrySize]);
    _bucketBackend.PutMetadata(
      EntryName,
      new ObjectMetadata { Name = EntryName, Size = EntrySize });
    _memory = new Memory<byte>(new byte[EntrySize]);
  }

  [Benchmark(Description = "with a stream")]
  public async Task<bool> TryGetAsyncWithByteStream() {
    var sut = new TryGetAsyncWithByteStream(_bucket);
    return await sut.TryGetAsync(EntryName, new MemoryBufferWriter<byte>(_memory));
  }

  [Benchmark(Description = "with an array", Baseline = true)]
  public async Task<bool> TryGetAsyncWithByteSequence() {
    var sut = new TryGetAsyncWithByteSequence(_bucket);
    return await sut.TryGetAsync(EntryName, new MemoryBufferWriter<byte>(_memory));
  }

  private readonly INatsObjStore _bucket;
  private readonly IBucketBackend _bucketBackend;
  private Memory<byte> _memory;
  private const string EntryName = "benchmark-entry";
}
