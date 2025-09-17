using System.Text.Json.Serialization;
using NATS.Client.ObjectStore.Models;

namespace Eshva.Caching.Nats.Tests.Tools;

/// <remarks>
/// Copied from .NET NATS-client.
/// </remarks>
[JsonSerializable(typeof(ObjectMetadata))]
[JsonSerializable(typeof(MetaDataOptions))]
[JsonSerializable(typeof(NatsObjLink))]
internal partial class NatsObjJsonSerializerContext : JsonSerializerContext { }
