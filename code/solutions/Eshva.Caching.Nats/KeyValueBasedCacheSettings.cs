using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace Eshva.Caching.Nats;

/// <summary>
/// NATS object store based cache settings.
/// </summary>
[PublicAPI]
public class KeyValueBasedCacheSettings {
  /// <summary>
  /// Values bucket name.
  /// </summary>
  [Required]
  public string ValueStore { get; set; } = string.Empty;

  /// <summary>
  /// Metadata bucket name.
  /// </summary>
  [Required]
  public string MetadataStore { get; set; } = string.Empty;

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
}
