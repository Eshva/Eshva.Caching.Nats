using JetBrains.Annotations;

namespace Eshva.Caching.Nats.Tests.OutOfProcessDeployments;

[PublicAPI]
public readonly record struct ObjectStoreBucket(
  string Name,
  long? BucketVolumeBytes) {
  public static ObjectStoreBucket Named(string bucketName) => new() { Name = bucketName };

  public ObjectStoreBucket OfSize(long bucketVolumeBytes) => this with { BucketVolumeBytes = bucketVolumeBytes };
}
