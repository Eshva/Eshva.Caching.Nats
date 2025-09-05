using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.ObjectStore;
using NATS.Client.ObjectStore.Models;

namespace Eshva.Caching.Nats.Tests.Tools;

/// <summary>
/// Corrupts metadata of an object-store entry to support tests.
/// </summary>
/// <remarks>
/// <see cref="INatsObjStore.UpdateMetaAsync"/> doesn't allow to corrupt metadata of an object in store, but it's necessary
/// to test negative scenarios.
/// </remarks>
public class ObjectEntryMetadataCorrupter {
  public async Task CorruptEntryMetadata(INatsObjStore bucket, string key) {
    var corruptedObjectMetadata = await bucket.GetInfoAsync(key) with {
      Nuid = null,
      Description = "Corrupted!",
      Digest = "Corrupted!"
    };
    await PublishMeta(
      bucket.JetStreamContext,
      bucket.Bucket,
      corruptedObjectMetadata,
      CancellationToken.None);
  }

  private async ValueTask PublishMeta(
    INatsJSContext jetStreamContext,
    string bucket,
    ObjectMetadata meta,
    CancellationToken cancellationToken) {
    var natsRollupHeaders = new NatsHeaders {
      { NatsRollup, RollupSubject }
    };
    var ack = await jetStreamContext.PublishAsync(
      GetMetaSubject(meta.Name, bucket),
      meta,
      NatsObjJsonSerializer<ObjectMetadata>.Default,
      headers: natsRollupHeaders,
      opts: new NatsJSPubOpts {
        RetryAttempts = 1
      },
      cancellationToken: cancellationToken);
    ack.EnsureSuccess();
  }

  private string GetMetaSubject(string key, string bucket) => $"$O.{bucket}.M.{Base64UrlEncoder.Encode(key)}";

  private const string NatsRollup = "Nats-Rollup";
  private const string RollupSubject = "sub";
}
