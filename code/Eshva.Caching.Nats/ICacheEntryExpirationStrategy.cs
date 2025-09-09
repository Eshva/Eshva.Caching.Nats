using JetBrains.Annotations;

namespace Eshva.Caching.Nats;

/// <summary>
/// Contract of a cache entries expiration strategy.
/// </summary>
[PublicAPI]
public interface ICacheEntryExpirationStrategy {
  /// <summary>
  /// Decide is cache entry expired given its expiration moment <paramref name="expiresAtUtc"/>.
  /// </summary>
  /// <param name="expiresAtUtc">Cache entry expiration moment.</param>
  /// <returns>
  /// <c>true</c> - entry is expired and should be deleted from the cache, <c>false</c> - entry is not expired yet.
  /// </returns>
  bool IsCacheEntryExpired(DateTimeOffset expiresAtUtc);

  /// <summary>
  /// Calculates absolute expiration given absolute and relative expiration.
  /// </summary>
  /// <param name="absoluteExpiration">Absolute expiration.</param>
  /// <param name="relativeExpiration">Relative expiration to the current moment.</param>
  /// <returns>Absolute expiration.</returns>
  DateTimeOffset? CalculateAbsoluteExpiration(DateTimeOffset? absoluteExpiration, TimeSpan? relativeExpiration);

  /// <summary>
  /// Calculate cache entry expiration moment given its expiration options.
  /// </summary>
  /// <param name="absoluteExpirationUtc">Absolute expiration data/time.</param>
  /// <param name="slidingExpiration">Sliding expiration time.</param>
  /// <returns>
  /// New cache entry expiration moment.
  /// </returns>
  DateTimeOffset CalculateExpiration(DateTimeOffset? absoluteExpirationUtc, TimeSpan? slidingExpiration);
}
