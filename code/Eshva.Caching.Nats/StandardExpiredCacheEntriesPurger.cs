using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Eshva.Caching.Nats;

/// <summary>
/// Standard expired cache entries purger.
/// </summary>
/// <remarks>
/// <para>
/// Purging is not executed in constant intervals with a timer. It executed if the time passed from the last execution is
/// greater than configured purging interval.
/// </para>
/// <para>
/// It can not be less than <see cref="MinimalExpiredEntriesPurgingInterval"/>. By default, it equals
/// <see cref="DefaultExpiredEntriesPurgingInterval"/>.
/// </para>
/// </remarks>
public abstract class StandardExpiredCacheEntriesPurger : ICacheExpiredEntriesPurger {
  /// <summary>
  /// Initializes a new instance of a standard expired cache entries purger.
  /// </summary>
  /// <remarks>
  /// If <paramref name="expiredEntriesPurgingInterval"/> isn't specified <see cref="DefaultExpiredEntriesPurgingInterval"/>
  /// will be used. If <paramref name="clock"/> is not specified <see cref="SystemClock"/> will be used. If
  /// <paramref name="logger"/> isn't specified a null logger will be used.
  /// </remarks>
  /// <param name="expiredEntriesPurgingInterval">Purging interval.</param>
  /// <param name="clock">System clock.</param>
  /// <param name="logger">Logger.</param>
  /// <exception cref="ArgumentOutOfRangeException">
  /// <paramref name="expiredEntriesPurgingInterval"/> value is less than <see cref="MinimalExpiredEntriesPurgingInterval"/>.
  /// </exception>
  [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
  protected StandardExpiredCacheEntriesPurger(
    TimeSpan? expiredEntriesPurgingInterval = null,
    ISystemClock? clock = null,
    ILogger? logger = null) {
    if (expiredEntriesPurgingInterval.HasValue && expiredEntriesPurgingInterval.Value < MinimalExpiredEntriesPurgingInterval) {
      throw new ArgumentOutOfRangeException(
        nameof(expiredEntriesPurgingInterval),
        $"Expired entries purging interval {expiredEntriesPurgingInterval} is less "
        + $"than minimal allowed value {MinimalExpiredEntriesPurgingInterval}.");
    }

    _expiredEntriesPurgingInterval = expiredEntriesPurgingInterval ?? DefaultExpiredEntriesPurgingInterval;
    _clock = clock ?? new SystemClock();
    Logger = logger ?? new NullLogger<StandardExpiredCacheEntriesPurger>();
    _lastExpirationScan = _clock.UtcNow;
  }

  /// <inheritdoc/>
  public void ScanForExpiredEntriesIfRequired(CancellationToken token = default) {
    lock (_scanForExpiredItemsLock) {
      var utcNow = _clock.UtcNow;
      var timePassedSinceTheLastPurging = utcNow - _lastExpirationScan;
      if (timePassedSinceTheLastPurging < _expiredEntriesPurgingInterval) {
        Logger.LogDebug(
          "Since the last purging expired entries {TimePassed} has passed that is less than {PurgingInterval}. Purging is not required",
          timePassedSinceTheLastPurging,
          _expiredEntriesPurgingInterval);
        return;
      }

      Logger.LogDebug(
        "Since the last purging expired entries {TimePassed} has passed that is greeter than or equals {PurgingInterval}. Purging is required",
        timePassedSinceTheLastPurging,
        _expiredEntriesPurgingInterval);
      _lastExpirationScan = utcNow;
      Task.Run(() => DeleteExpiredCacheEntries(token), token);
    }
  }

  /// <summary>
  /// Purging interval used if it's not specified in constructor.
  /// </summary>
  protected abstract TimeSpan DefaultExpiredEntriesPurgingInterval { get; }

  /// <summary>
  /// Minimal purging interval allowed.
  /// </summary>
  protected abstract TimeSpan MinimalExpiredEntriesPurgingInterval { get; }

  /// <summary>
  /// Logger.
  /// </summary>
  protected ILogger Logger { get; }

  /// <summary>
  /// Purger logic.
  /// </summary>
  /// <param name="token"></param>
  /// <returns></returns>
  protected abstract Task DeleteExpiredCacheEntries(CancellationToken token);

  private readonly ISystemClock _clock;
  private readonly TimeSpan _expiredEntriesPurgingInterval;
  private readonly Lock _scanForExpiredItemsLock = new();
  private DateTimeOffset _lastExpirationScan;
}
