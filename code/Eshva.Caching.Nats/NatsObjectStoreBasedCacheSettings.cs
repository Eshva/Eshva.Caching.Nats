using JetBrains.Annotations;

namespace Eshva.Caching.Nats;

/// <summary>
/// Connection settings to Object Store on a NATS-server.
/// </summary>
[PublicAPI]
public sealed record NatsObjectStoreBasedCacheSettings {
  /// <summary>
  /// Interval of purging expired entries in the cache.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Purging is not executed in constant intervals with a timer. It executed in all cache accessing methods if the time
  /// passed from the last execution is greater tha value of this property.
  /// </para>
  /// <para>
  /// It can not be less than <see cref="MinimalExpiredEntriesPurgingInterval"/>. By default, it equals
  /// <see cref="DefaultExpiredEntriesPurgingInterval"/>.
  /// </para>
  /// </remarks>
  public TimeSpan ExpiredEntriesPurgingInterval { get; set; } = DefaultExpiredEntriesPurgingInterval;

  /// <summary>
  /// Default sliding expiration time of cache entries.
  /// </summary>
  public TimeSpan DefaultSlidingExpirationTime { get; set; } = DefaultSlidingExpirationInterval;

  /// <summary>
  /// Validate settings.
  /// </summary>
  /// <returns>
  /// A list of error validation messages if any found or an empty list if no any errors.
  /// </returns>
  public IReadOnlyList<string> Validate() {
    var result = new List<string>();
    if (ExpiredEntriesPurgingInterval < MinimalExpiredEntriesPurgingInterval) {
      result.Add(
        $"Expired entries purging interval {ExpiredEntriesPurgingInterval} is less "
        + $"than minimal allowed value {MinimalExpiredEntriesPurgingInterval}.");
    }

    if (DefaultSlidingExpirationTime < MinimalSlidingExpirationInterval) {
      result.Add(
        $"Default sliding expiration time of cache entries {DefaultSlidingExpirationTime} is less "
        + $"than minimal allowed value {MinimalSlidingExpirationInterval}.");
    }

    return result;
  }

  public static readonly TimeSpan DefaultSlidingExpirationInterval = TimeSpan.FromMinutes(minutes: 10);
  public static readonly TimeSpan MinimalSlidingExpirationInterval = TimeSpan.FromMinutes(minutes: 1);
  public static readonly TimeSpan DefaultExpiredEntriesPurgingInterval = TimeSpan.FromMinutes(minutes: 10);
  public static readonly TimeSpan MinimalExpiredEntriesPurgingInterval = TimeSpan.FromMinutes(minutes: 1);
}
