using NATS.Client.JetStream;
using NATS.Client.ObjectStore;
using NATS.Client.ObjectStore.Models;

namespace Eshva.Caching.Nats.Benchmarks.Tools;

public class InMemoryTestBuket : INatsObjStore, IBucketBackend {
  public InMemoryTestBuket(string bucketName) {
    _bucket = bucketName ?? throw new ArgumentNullException(nameof(bucketName));
  }

  ValueTask<byte[]> INatsObjStore.GetBytesAsync(string key, CancellationToken cancellationToken) =>
    _entries.TryGetValue(key, out var entry)
      ? ValueTask.FromResult(entry)
      : throw new InvalidOperationException($"Entry with key {key} was not found.");

  ValueTask<ObjectMetadata> INatsObjStore.GetAsync(
    string key,
    Stream stream,
    bool leaveOpen,
    CancellationToken cancellationToken) {
    if (!_entries.TryGetValue(key, out var entry)) throw new InvalidOperationException($"Entry with key {key} was not found.");

    stream.Write(entry);
    return ValueTask.FromResult(_metadata[key]);
  }

  ValueTask<ObjectMetadata> INatsObjStore.PutAsync(string key, byte[] value, CancellationToken cancellationToken) {
    _entries[key] = value;
    _metadata[key] = new ObjectMetadata {
      Name = key,
      Size = value.Length,
      Bucket = _bucket
    };
    return ValueTask.FromResult(_metadata[key]);
  }

  ValueTask<ObjectMetadata> INatsObjStore.PutAsync(
    string key,
    Stream stream,
    bool leaveOpen,
    CancellationToken cancellationToken) {
    using var reader = new BinaryReader(stream);
    _entries[key] = reader.ReadBytes((int)stream.Length);
    if (!leaveOpen) stream.Close();
    _metadata[key] = new ObjectMetadata {
      Name = key,
      Size = (int)stream.Length,
      Bucket = _bucket
    };
    return ValueTask.FromResult(_metadata[key]);
  }

  ValueTask<ObjectMetadata> INatsObjStore.PutAsync(
    ObjectMetadata meta,
    Stream stream,
    bool leaveOpen,
    CancellationToken cancellationToken) {
    using var reader = new BinaryReader(stream);
    _entries[meta.Name] = reader.ReadBytes((int)stream.Length);
    if (!leaveOpen) stream.Close();

    _metadata[meta.Name] = meta;
    return ValueTask.FromResult(meta);
  }

  ValueTask<ObjectMetadata> INatsObjStore.UpdateMetaAsync(string key, ObjectMetadata meta, CancellationToken cancellationToken) {
    _metadata[key] = meta;
    return ValueTask.FromResult(meta);
  }

  ValueTask<ObjectMetadata> INatsObjStore.AddLinkAsync(string link, string target, CancellationToken cancellationToken) =>
    throw new NotImplementedException();

  ValueTask<ObjectMetadata> INatsObjStore.AddLinkAsync(string link, ObjectMetadata target, CancellationToken cancellationToken) =>
    throw new NotImplementedException();

  ValueTask<ObjectMetadata> INatsObjStore.AddBucketLinkAsync(
    string link,
    INatsObjStore target,
    CancellationToken cancellationToken) => throw new NotImplementedException();

  ValueTask INatsObjStore.SealAsync(CancellationToken cancellationToken) =>
    throw new NotImplementedException();

  ValueTask<ObjectMetadata> INatsObjStore.GetInfoAsync(string key, bool showDeleted, CancellationToken cancellationToken) =>
    _metadata.TryGetValue(key, out var metadata) && (showDeleted || !metadata.Deleted)
      ? ValueTask.FromResult(metadata)
      : throw new InvalidOperationException($"Entry with key {key} was not found.");

  IAsyncEnumerable<ObjectMetadata> INatsObjStore.ListAsync(NatsObjListOpts? opts, CancellationToken cancellationToken) =>
    opts is not null && !opts.ShowDeleted
      ? _metadata.Values.Where(metadata => metadata.Deleted).ToAsyncEnumerable()
      : _metadata.Values.ToAsyncEnumerable();

  ValueTask<NatsObjStatus> INatsObjStore.GetStatusAsync(CancellationToken cancellationToken) =>
    throw new NotImplementedException();

  IAsyncEnumerable<ObjectMetadata> INatsObjStore.WatchAsync(NatsObjWatchOpts? opts, CancellationToken cancellationToken) =>
    throw new NotImplementedException();

  ValueTask INatsObjStore.DeleteAsync(string key, CancellationToken cancellationToken) {
    _entries.Remove(key);
    _metadata[key].Deleted = true;
    return ValueTask.CompletedTask;
  }

  void IBucketBackend.PutEntry(string key, byte[] data) => _entries[key] = data;

  byte[] IBucketBackend.GetEntry(string key) => _entries[key];

  void IBucketBackend.PutMetadata(string key, ObjectMetadata metadata) => _metadata[key] = metadata;

  ObjectMetadata IBucketBackend.GetMetadata(string key) => _metadata[key];

  INatsJSContext INatsObjStore.JetStreamContext => throw new NotImplementedException();

  string INatsObjStore.Bucket => _bucket;

  private readonly string _bucket;
  private readonly Dictionary<string, byte[]> _entries = new(StringComparer.Ordinal);
  private readonly Dictionary<string, ObjectMetadata> _metadata = new();
}
