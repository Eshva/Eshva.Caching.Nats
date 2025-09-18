using JetBrains.Annotations;

namespace Eshva.Caching.Abstractions;

/// <summary>
/// Time-based expiration strategy settings.
/// </summary>
[PublicAPI]
public class ExpirationStrategySettings {
  /// <summary>
  /// Default sliding expiration time of cache entries.
  /// </summary>
  public TimeSpan DefaultSlidingExpirationInterval { get; set; }
}
