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
  /// Initializes new instance of standard cache entry expiration strategy with system clock <paramref name="clock"/>.
  /// </summary>
  /// <remarks>
  /// If <paramref name="clock"/> isn't specified the computer system clock will be used.
  /// </remarks>
  /// <param name="expirationStrategySettings">Expiration strategy settings.</param>
  /// <param name="clock">System clock.</param>
  public StandardCacheEntryExpirationStrategy(
    ExpirationStrategySettings expirationStrategySettings,
    ISystemClock? clock = null) {
    ArgumentNullException.ThrowIfNull(expirationStrategySettings);
    DefaultSlidingExpirationInterval = expirationStrategySettings.DefaultSlidingExpirationInterval;
    _clock = clock ?? new SystemClock();
  }

  public TimeSpan DefaultSlidingExpirationInterval { get; }

  /// <inheritdoc/>
  /// <remarks>
  /// Entry is expired if its expiration moment equals or greater than the current date/time.
  /// </remarks>
  public bool IsCacheEntryExpired(DateTimeOffset expiresAtUtc) => expiresAtUtc <= _clock.UtcNow;

  /// <inheritdoc/>
  /// <remarks>
  /// If given absolute expiration returns it. If given relative expiration returns adjust the current moment by relative
  /// expiration. If both are <c>null</c> return <c>null</c>.
  /// </remarks>
  public DateTimeOffset? CalculateAbsoluteExpiration(DateTimeOffset? absoluteExpiration, TimeSpan? relativeExpiration) {
    if (absoluteExpiration.HasValue) return absoluteExpiration.Value;
    if (relativeExpiration.HasValue) return _clock.UtcNow.Add(relativeExpiration.Value);
    return null;
  }

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
  /// <item>
  /// If both arguments not provided returns current UTC-time plus <see cref="DefaultSlidingExpirationInterval"/>
  /// value.
  /// </item>
  /// <item>Otherwise returns current UTC-time plus <paramref name="slidingExpiration"/> value.</item>
  /// </list>
  /// </remarks>
  public DateTimeOffset CalculateExpiration(DateTimeOffset? absoluteExpirationUtc, TimeSpan? slidingExpiration) {
    if (absoluteExpirationUtc.HasValue && !slidingExpiration.HasValue) return absoluteExpirationUtc.Value;
    if (!absoluteExpirationUtc.HasValue && slidingExpiration.HasValue) return _clock.UtcNow.Add(slidingExpiration.Value);
    if (!absoluteExpirationUtc.HasValue || !slidingExpiration.HasValue) return _clock.UtcNow.Add(DefaultSlidingExpirationInterval);
    var slidingExpirationUtc = _clock.UtcNow.Add(slidingExpiration.Value);
    return absoluteExpirationUtc.Value <= slidingExpirationUtc ? absoluteExpirationUtc.Value : slidingExpirationUtc;
  }

  private readonly ISystemClock _clock;
}
