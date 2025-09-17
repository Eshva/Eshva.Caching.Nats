using JetBrains.Annotations;

namespace Eshva.Caching.Nats;

/// <summary>
/// Expired cache entries purger settings.
/// </summary>
[PublicAPI]
public class PurgerSettings {
  /// <summary>
  /// Purging interval.
  /// </summary>
  public TimeSpan ExpiredEntriesPurgingInterval { get; set; }
}
