using System.Collections.Immutable;

namespace Eshva.Caching.Nats.Tests.OutOfProcessDeployments;

public partial class NatsServerDeployment {
  public readonly record struct Settings(
    string Name,
    string ImageTag,
    string ContainerName,
    ushort HostNetworkClientPort,
    ushort? HostNetworkHttpManagementPort,
    bool IsJetStreamEnabled,
    ImmutableArray<ObjectStoreBucket> Buckets) {
    public Settings FromImageTag(string imageTag) =>
      this with { ImageTag = imageTag };

    public Settings WithContainerName(string containerName) =>
      this with { ContainerName = containerName };

    public Settings WithHostNetworkClientPort(ushort hostNetworkClientPort) =>
      this with { HostNetworkClientPort = hostNetworkClientPort };

    public Settings WithHostNetworkHttpManagementPort(ushort hostNetworkHttpManagementPort) =>
      this with { HostNetworkHttpManagementPort = hostNetworkHttpManagementPort };

    public Settings WithJetStreamEnabled() =>
      this with { IsJetStreamEnabled = true };

    public Settings CreateBucket(ObjectStoreBucket bucketSettings) =>
      this with { Buckets = Buckets.Add(bucketSettings) };
  }
}
