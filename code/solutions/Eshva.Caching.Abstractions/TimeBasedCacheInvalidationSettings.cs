using JetBrains.Annotations;

namespace Eshva.Caching.Abstractions;

/// <summary>
/// Time-based cache invalidation settings.
/// </summary>
[PublicAPI]
public class TimeBasedCacheInvalidationSettings {
  /// <summary>
  /// Default sliding expiration time of cache entries.
  /// </summary>
  public TimeSpan DefaultSlidingExpirationInterval { get; set; }

  /// <summary>
  /// Purging interval.
  /// </summary>
  public TimeSpan ExpiredEntriesPurgingInterval { get; set; }
}
