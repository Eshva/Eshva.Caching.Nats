using JetBrains.Annotations;

namespace Eshva.Caching.Nats.TestWebApp.ObjectStoreBasedCache;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class Settings {
  public string NatsServerConnectionString { get; init; } = string.Empty;

  public string NatsConnectionName { get; init; } = string.Empty;
}
