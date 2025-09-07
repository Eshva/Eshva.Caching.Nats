using JetBrains.Annotations;
using Microsoft.Extensions.Internal;

namespace Eshva.Caching.Nats;

/// <summary>
/// Standard cache entry expiration strategy.
/// </summary>
/// <remarks>
/// Same logic as in <c>SqlServerCache</c> from ASP.NET.
/// </remarks>
[PublicAPI]
public class StandardCacheEntryExpirationStrategy : ICacheEntryExpirationStrategy {
  /// <summary>
  /// Initializes new instance of standard cache entry expiration strategy with default sliding expiration time
  /// <paramref name="defaultSlidingExpirationTime"/> and system clock <paramref name="clock"/>.
  /// </summary>
  /// <remarks>
  /// If <paramref name="clock"/> isn't specified the computer system clock will be used. If
  /// <paramref name="defaultSlidingExpirationTime"/> isn't specified <see cref="DefaultSlidingExpirationInterval"/> is used
  /// as its value.
  /// </remarks>
  /// <param name="defaultSlidingExpirationTime">Default sliding expiration time of cache entries.</param>
  /// <param name="clock">System clock.</param>
  public StandardCacheEntryExpirationStrategy(
    TimeSpan? defaultSlidingExpirationTime = null,
    ISystemClock? clock = null) {
    DefaultSlidingExpirationTime = defaultSlidingExpirationTime ?? DefaultSlidingExpirationInterval;
    _clock = clock ?? new SystemClock();
  }

  public TimeSpan DefaultSlidingExpirationTime { get; }

  /// <inheritdoc/>
  /// <remarks>
  /// Entry is expired if its expiration moment equals or greater than the current date/time.
  /// </remarks>
  public bool IsCacheEntryExpired(DateTimeOffset expiresAtUtc) => expiresAtUtc <= _clock.UtcNow;

  /// <inheritdoc/>
  /// <remarks>
  /// <list type="bullet">
  /// <item>If only <paramref name="absoluteExpirationUtc"/> given returns <paramref name="absoluteExpirationUtc"/> value.</item>
  /// <item>
  /// If only <paramref name="slidingExpiration"/> given returns current UTC-time plus
  /// <paramref name="slidingExpiration"/> value.
  /// </item>
  /// <item>
  /// If both <paramref name="absoluteExpirationUtc"/> and <paramref name="slidingExpiration"/> given and absolute
  /// expiration happens earlier than sliding returns <paramref name="absoluteExpirationUtc"/>.
  /// </item>
  /// <item>If both arguments not provided returns current UTC-time plus <see cref="DefaultSlidingExpirationTime"/> value.</item>
  /// <item>Otherwise returns current UTC-time plus <paramref name="slidingExpiration"/> value.</item>
  /// </list>
  /// </remarks>
  public DateTimeOffset CalculateExpiration(DateTimeOffset? absoluteExpirationUtc, TimeSpan? slidingExpiration) {
    if (absoluteExpirationUtc.HasValue && !slidingExpiration.HasValue) return absoluteExpirationUtc.Value;
    if (!absoluteExpirationUtc.HasValue && slidingExpiration.HasValue) return _clock.UtcNow.Add(slidingExpiration.Value);
    if (!absoluteExpirationUtc.HasValue || !slidingExpiration.HasValue) return _clock.UtcNow.Add(DefaultSlidingExpirationTime);
    var slidingExpirationUtc = _clock.UtcNow.Add(slidingExpiration.Value);
    return absoluteExpirationUtc.Value <= slidingExpirationUtc ? absoluteExpirationUtc.Value : slidingExpirationUtc;
  }

  private readonly ISystemClock _clock;
  public static readonly TimeSpan DefaultSlidingExpirationInterval = TimeSpan.FromMinutes(minutes: 10);
}
