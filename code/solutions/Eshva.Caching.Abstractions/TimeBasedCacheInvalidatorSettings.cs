using JetBrains.Annotations;

namespace Eshva.Caching.Abstractions;

/// <summary>
/// Time-based cache invalidator settings.
/// </summary>
[PublicAPI]
public class TimeBasedCacheInvalidatorSettings {
  /// <summary>
  /// Purging interval.
  /// </summary>
  public TimeSpan ExpiredEntriesPurgingInterval { get; set; }
}
