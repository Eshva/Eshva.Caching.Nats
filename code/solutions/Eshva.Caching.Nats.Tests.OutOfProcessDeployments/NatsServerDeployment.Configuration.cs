using System.Collections.Immutable;

namespace Eshva.Caching.Nats.Tests.OutOfProcessDeployments;

public partial class NatsServerDeployment {
  public readonly record struct Configuration(
    string Name,
    string ImageTag,
    string ContainerName,
    ushort HostNetworkClientPort,
    ushort? HostNetworkHttpManagementPort,
    bool ShouldEnableJetStream,
    bool ShouldEnableDebugOutput,
    bool ShouldEnableTraceOutput,
    ImmutableArray<ObjectStoreBucket> Buckets) {
    public Configuration FromImageTag(string imageTag) =>
      this with { ImageTag = imageTag };

    public Configuration WithContainerName(string containerName) =>
      this with { ContainerName = containerName };

    public Configuration WithHostNetworkClientPort(ushort hostNetworkClientPort) =>
      this with { HostNetworkClientPort = hostNetworkClientPort };

    public Configuration WithHostNetworkHttpManagementPort(ushort hostNetworkHttpManagementPort) =>
      this with { HostNetworkHttpManagementPort = hostNetworkHttpManagementPort };

    public Configuration EnabledJetStream() =>
      this with { ShouldEnableJetStream = true };

    public Configuration EnableDebugOutput() =>
      this with { ShouldEnableDebugOutput = true };

    public Configuration EnableTraceOutput() =>
      this with { ShouldEnableTraceOutput = true };

    public Configuration CreateBucket(ObjectStoreBucket bucketSettings) =>
      this with {
        Buckets = !Buckets.IsDefault
          ? Buckets.Add(bucketSettings)
          : [bucketSettings]
      };
  }
}
