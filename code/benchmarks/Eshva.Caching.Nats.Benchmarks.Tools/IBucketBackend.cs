using NATS.Client.ObjectStore.Models;

namespace Eshva.Caching.Nats.Benchmarks.Tools;

public interface IBucketBackend {
  void PutEntry(string key, byte[] data);

  byte[] GetEntry(string key);

  void PutMetadata(string key, ObjectMetadata metadata);

  ObjectMetadata GetMetadata(string key);
}
