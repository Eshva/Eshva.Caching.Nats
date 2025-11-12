using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace Eshva.Caching.Nats;

/// <summary>
/// NATS object store based cache settings.
/// </summary>
[PublicAPI]
public class ObjectStoreBasedCacheSettings {
  /// <summary>
  /// Cache bucket name.
  /// </summary>
  [Required]
  public string BucketName { get; set; } = string.Empty;

  /// <summary>
  /// Default entry sliding expiration interval.
  /// </summary>
  [Required]
  public TimeSpan DefaultSlidingExpirationInterval { get; set; }

  /// <summary>
  /// Expired entries purging interval.
  /// </summary>
  [Required]
  public TimeSpan ExpiredEntriesPurgingInterval { get; set; }

  /// <summary>
  /// Maximal cache invalidation duration.
  /// </summary>
  [Required]
  public TimeSpan MaximalCacheInvalidationDuration { get; set; }
}
