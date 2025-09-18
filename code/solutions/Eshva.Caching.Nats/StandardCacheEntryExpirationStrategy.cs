using Eshva.Caching.Abstractions;
using JetBrains.Annotations;

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
  /// Initializes new instance of standard cache entry expiration strategy with system clock <paramref name="timeProvider"/>.
  /// </summary>
  /// <param name="expirationStrategySettings">Expiration strategy settings.</param>
  /// <param name="timeProvider">Time provider.</param>
  public StandardCacheEntryExpirationStrategy(
    ExpirationStrategySettings expirationStrategySettings,
    TimeProvider timeProvider) {
    ArgumentNullException.ThrowIfNull(expirationStrategySettings);
    ArgumentNullException.ThrowIfNull(timeProvider);
    DefaultSlidingExpirationInterval = expirationStrategySettings.DefaultSlidingExpirationInterval;
    _timeProvider = timeProvider;
  }

  public TimeSpan DefaultSlidingExpirationInterval { get; }

  /// <inheritdoc/>
  /// <remarks>
  /// Entry is expired if its expiration moment equals or greater than the current date/time.
  /// </remarks>
  public bool IsCacheEntryExpired(DateTimeOffset expiresAtUtc) => expiresAtUtc <= _timeProvider.GetUtcNow();

  /// <inheritdoc/>
  /// <remarks>
  /// If given absolute expiration returns it. If given relative expiration returns adjust the current moment by relative
  /// expiration. If both are <c>null</c> return <c>null</c>.
  /// </remarks>
  public DateTimeOffset? CalculateAbsoluteExpiration(DateTimeOffset? absoluteExpiration, TimeSpan? relativeExpiration) {
    if (absoluteExpiration.HasValue) return absoluteExpiration.Value;
    if (relativeExpiration.HasValue) return _timeProvider.GetUtcNow().Add(relativeExpiration.Value);
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
    if (absoluteExpirationUtc.HasValue && !slidingExpiration.HasValue) {
      return absoluteExpirationUtc.Value;
    }

    if (!absoluteExpirationUtc.HasValue && slidingExpiration.HasValue) {
      return _timeProvider.GetUtcNow().Add(slidingExpiration.Value);
    }

    if (!absoluteExpirationUtc.HasValue || !slidingExpiration.HasValue) {
      return _timeProvider.GetUtcNow().Add(DefaultSlidingExpirationInterval);
    }

    var slidingExpirationUtc = _timeProvider.GetUtcNow().Add(slidingExpiration.Value);
    return absoluteExpirationUtc.Value <= slidingExpirationUtc ? absoluteExpirationUtc.Value : slidingExpirationUtc;
  }

  private readonly TimeProvider _timeProvider;
}
