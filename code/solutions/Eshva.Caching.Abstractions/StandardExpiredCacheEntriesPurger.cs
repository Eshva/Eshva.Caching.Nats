using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Eshva.Caching.Abstractions;

/// <summary>
/// Standard expired cache entries purger.
/// </summary>
/// <remarks>
/// <para>
/// Purging is not executed in constant intervals with a timer. It executed if the time passed from the last execution is
/// greater than configured purging interval.
/// </para>
/// </remarks>
public abstract class StandardExpiredCacheEntriesPurger : ICacheExpiredEntriesPurger, IPurgingNotifier, IPurgingSynchronicityController {
  /// <summary>
  /// Initializes a new instance of a standard expired cache entries purger.
  /// </summary>
  /// <remarks>
  /// If <paramref name="clock"/> is not specified <see cref="Microsoft.Extensions.Internal.SystemClock"/> will be used. If
  /// <paramref name="logger"/> isn't
  /// specified a null logger will be used.
  /// </remarks>
  /// <param name="purgerSettings">Purger settings.</param>
  /// <param name="minimalExpiredEntriesPurgingInterval">Minimal purging interval allowed.</param>
  /// <param name="clock">System clock.</param>
  /// <param name="logger">Logger.</param>
  /// <exception cref="ArgumentOutOfRangeException">
  /// <paramref name="purgerSettings"/>.ExpiredEntriesPurgingInterval is less than
  /// <paramref name="minimalExpiredEntriesPurgingInterval"/>.
  /// </exception>
  [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
  protected StandardExpiredCacheEntriesPurger(
    PurgerSettings purgerSettings,
    TimeSpan minimalExpiredEntriesPurgingInterval,
    ISystemClock? clock = null,
    ILogger? logger = null) {
    ArgumentNullException.ThrowIfNull(purgerSettings);
    if (purgerSettings.ExpiredEntriesPurgingInterval < minimalExpiredEntriesPurgingInterval) {
      throw new ArgumentOutOfRangeException(
        nameof(purgerSettings),
        $"Expired entries purging interval {purgerSettings.ExpiredEntriesPurgingInterval} is less "
        + $"than minimal allowed value {minimalExpiredEntriesPurgingInterval}.");
    }

    _expiredEntriesPurgingInterval = purgerSettings.ExpiredEntriesPurgingInterval;
    _clock = clock ?? new SystemClock();
    Logger = logger ?? new NullLogger<StandardExpiredCacheEntriesPurger>();
    _lastExpirationScan = _clock.UtcNow;
  }

  /// <inheritdoc/>
  public bool ShouldPurgeSynchronously { get; set; }

  /// <inheritdoc/>
  public async Task ScanForExpiredEntriesIfRequired(CancellationToken token = default) {
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
        "Since the last purging expired entries {TimePassed} has passed that is greeter than or equals to {PurgingInterval}. Purging is required",
        timePassedSinceTheLastPurging,
        _expiredEntriesPurgingInterval);
      _lastExpirationScan = utcNow;
    }

    if (ShouldPurgeSynchronously) {
      await Task.Run(() => DeleteExpiredCacheEntries(token), token);
    }
    else {
      _ = Task.Run(() => DeleteExpiredCacheEntries(token), token);
    }
  }

  /// <summary>
  /// Logger.
  /// </summary>
  protected ILogger Logger { get; }

  /// <summary>
  /// Purger logic.
  /// </summary>
  /// <param name="token">Cancellation token.</param>
  /// <returns>Purge operation statistics.</returns>
  protected abstract Task DeleteExpiredCacheEntries(CancellationToken token);

  /// <summary>
  /// Notify purge operation started.
  /// </summary>
  protected void NotifyPurgeStarted() => PurgeStarted?.Invoke(this, EventArgs.Empty);

  /// <summary>
  /// Notify purge operation completed.
  /// </summary>
  /// <param name="totalCount">Total cache entries scanned.</param>
  /// <param name="purgedCount">Cache entries purged.</param>
  protected void NotifyPurgeCompleted(uint totalCount, uint purgedCount) =>
    PurgeCompleted?.Invoke(this, new PurgeStatistics(totalCount, purgedCount));

  /// <inheritdoc/>
  public event EventHandler? PurgeStarted;

  /// <inheritdoc/>
  public event EventHandler<PurgeStatistics>? PurgeCompleted;

  private readonly ISystemClock _clock;
  private readonly TimeSpan _expiredEntriesPurgingInterval;
  private readonly Lock _scanForExpiredItemsLock = new();
  private DateTimeOffset _lastExpirationScan;
}
