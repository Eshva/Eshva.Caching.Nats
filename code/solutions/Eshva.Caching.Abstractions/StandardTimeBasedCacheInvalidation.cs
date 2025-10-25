using JetBrains.Annotations;

namespace Eshva.Caching.Abstractions;

/// <summary>
/// Standard cache entry expiration strategy.
/// </summary>
/// <remarks>
/// Same logic as in <c>SqlServerCache</c> from ASP.NET.
/// </remarks>
[PublicAPI]
public sealed class StandardTimeBasedCacheInvalidation {
  /// <summary>
  /// Initializes new instance of standard cache entry expiration strategy with system clock <paramref name="timeProvider"/>.
  /// </summary>
  /// <param name="standardTimeBasedCacheInvalidationSettings">Expiration strategy settings.</param>
  /// <param name="timeProvider">Time provider.</param>
  public StandardTimeBasedCacheInvalidation(
    StandardTimeBasedCacheInvalidationSettings standardTimeBasedCacheInvalidationSettings,
    TimeProvider timeProvider) {
    ArgumentNullException.ThrowIfNull(standardTimeBasedCacheInvalidationSettings);
    ArgumentNullException.ThrowIfNull(timeProvider);
    DefaultSlidingExpirationInterval = standardTimeBasedCacheInvalidationSettings.DefaultSlidingExpirationInterval;
    _timeProvider = timeProvider;
  }

  public TimeSpan DefaultSlidingExpirationInterval { get; }

  /// <summary>
  /// Decide is cache entry expired given its expiration moment <paramref name="expiresAtUtc"/>.
  /// </summary>
  /// <remarks>
  /// Entry is expired if its expiration moment equals or greater than the current date/time.
  /// </remarks>
  /// <param name="expiresAtUtc">Cache entry expiration moment.</param>
  /// <returns>
  /// <c>true</c> - entry is expired and should be deleted from the cache, <c>false</c> - entry is not expired yet.
  /// </returns>
  public bool IsCacheEntryExpired(DateTimeOffset expiresAtUtc) => expiresAtUtc <= _timeProvider.GetUtcNow();

  /// <summary>
  /// Calculates absolute expiration given absolute and relative expiration.
  /// </summary>
  /// <remarks>
  /// If given absolute expiration returns it. If given relative expiration returns adjust the current moment by relative
  /// expiration. If both are <c>null</c> return <c>null</c>.
  /// </remarks>
  /// <param name="absoluteExpiration">Absolute expiration.</param>
  /// <param name="relativeExpiration">Relative expiration to the current moment.</param>
  /// <returns>Absolute expiration.</returns>
  public DateTimeOffset? CalculateAbsoluteExpiration(DateTimeOffset? absoluteExpiration, TimeSpan? relativeExpiration) {
    if (absoluteExpiration.HasValue) return absoluteExpiration.Value;
    if (relativeExpiration.HasValue) return _timeProvider.GetUtcNow().Add(relativeExpiration.Value);
    return null;
  }

  /// <summary>
  /// Calculate cache entry expiration moment given its expiration options.
  /// </summary>
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
  /// <param name="absoluteExpirationUtc">Absolute expiration data/time.</param>
  /// <param name="slidingExpiration">Sliding expiration time.</param>
  /// <returns>
  /// New cache entry expiration moment.
  /// </returns>
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
