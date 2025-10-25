using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Eshva.Caching.Abstractions;

/// <summary>
/// Time-based cache invalidator.
/// </summary>
/// <remarks>
/// <para>
/// Purging is not executed in constant intervals with a timer. It executed if the time passed from the last execution is
/// greater than configured purging interval.
/// </para>
/// </remarks>
public abstract class TimeBasedCacheInvalidator : ICacheInvalidator, IPurgingNotifier, IPurgingSynchronicityController {
  /// <summary>
  /// Initializes a new instance of a time-based cache invalidator
  /// </summary>
  /// <remarks>
  /// If <paramref name="timeProvider"/> is not specified <see cref="Microsoft.Extensions.Internal.SystemClock"/> will be
  /// used. If
  /// <paramref name="logger"/> isn't
  /// specified a null logger will be used.
  /// </remarks>
  /// <param name="timeBasedCacheInvalidatorSettings">Time-based cache invalidator settings.</param>
  /// <param name="minimalExpiredEntriesPurgingInterval">Minimal purging interval allowed.</param>
  /// <param name="timeProvider">Time provider.</param>
  /// <param name="logger">Logger.</param>
  /// <exception cref="ArgumentOutOfRangeException">
  /// <paramref name="timeBasedCacheInvalidatorSettings"/>.ExpiredEntriesPurgingInterval is less than
  /// <paramref name="minimalExpiredEntriesPurgingInterval"/>.
  /// </exception>
  protected TimeBasedCacheInvalidator(
    TimeBasedCacheInvalidatorSettings timeBasedCacheInvalidatorSettings,
    TimeSpan minimalExpiredEntriesPurgingInterval,
    TimeProvider timeProvider,
    ILogger? logger = null) {
    ArgumentNullException.ThrowIfNull(timeBasedCacheInvalidatorSettings);
    ArgumentNullException.ThrowIfNull(timeProvider);
    if (timeBasedCacheInvalidatorSettings.ExpiredEntriesPurgingInterval < minimalExpiredEntriesPurgingInterval) {
      throw new ArgumentOutOfRangeException(
        nameof(timeBasedCacheInvalidatorSettings),
        $"Expired entries purging interval {timeBasedCacheInvalidatorSettings.ExpiredEntriesPurgingInterval} is less "
        + $"than minimal allowed value {minimalExpiredEntriesPurgingInterval}.");
    }

    _expiredEntriesPurgingInterval = timeBasedCacheInvalidatorSettings.ExpiredEntriesPurgingInterval;
    _timeProvider = timeProvider;
    Logger = logger ?? new NullLogger<TimeBasedCacheInvalidator>();
    _lastExpirationScan = _timeProvider.GetUtcNow();
  }

  /// <inheritdoc/>
  public bool ShouldPurgeSynchronously { get; set; }

  /// <inheritdoc/>
  public async Task PurgeEntriesIfRequired(CancellationToken token = default) {
    const byte purgingInProgress = 1;
    const byte notYetPurging = 0;
    if (Interlocked.CompareExchange(ref _isPurgingInProgress, purgingInProgress, notYetPurging) == purgingInProgress) {
      Logger.LogDebug("Purging already in progress");
      return;
    }

    if (!ShouldPurgeEntries()) return;

    try {
      _lastExpirationScan = _timeProvider.GetUtcNow();
      if (ShouldPurgeSynchronously) {
        await DeleteExpiredCacheEntries(token).ConfigureAwait(continueOnCapturedContext: false);
      }
      else {
        _ = Task.Run(() => DeleteExpiredCacheEntries(token), token);
      }
    }
    finally {
      _isPurgingInProgress = notYetPurging;
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

  private bool ShouldPurgeEntries() {
    var utcNow = _timeProvider.GetUtcNow();
    var timePassedSinceTheLastPurging = utcNow - _lastExpirationScan;
    if (timePassedSinceTheLastPurging < _expiredEntriesPurgingInterval) {
      Logger.LogDebug(
        "Since the last purging expired entries {TimePassed} has passed that is less than {PurgingInterval}. Purging is not required",
        timePassedSinceTheLastPurging,
        _expiredEntriesPurgingInterval);
      return false;
    }

    Logger.LogDebug(
      "Since the last purging expired entries {TimePassed} has passed that is greeter than or equals to {PurgingInterval}. Purging is required",
      timePassedSinceTheLastPurging,
      _expiredEntriesPurgingInterval);
    return true;
  }

  /// <inheritdoc/>
  public event EventHandler? PurgeStarted;

  /// <inheritdoc/>
  public event EventHandler<PurgeStatistics>? PurgeCompleted;

  private readonly TimeSpan _expiredEntriesPurgingInterval;
  private readonly TimeProvider _timeProvider;
  private byte _isPurgingInProgress;
  private DateTimeOffset _lastExpirationScan;
}
