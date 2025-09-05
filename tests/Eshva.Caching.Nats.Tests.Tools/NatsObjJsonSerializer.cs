using NATS.Client.Core;

namespace Eshva.Caching.Nats.Tests.Tools;

/// <remarks>
/// Copied from .NET NATS-client.
/// </remarks>
internal static class NatsObjJsonSerializer<T> {
  public static readonly INatsSerializer<T> Default = new NatsJsonContextSerializer<T>(NatsObjJsonSerializerContext.Default);
}
