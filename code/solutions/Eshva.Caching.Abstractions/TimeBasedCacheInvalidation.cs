using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Eshva.Caching.Abstractions;

/// <summary>
/// Time-based cache invalidation.
/// </summary>
/// <remarks>
/// Cache invalidation is not executed in constant intervals with a timer. It executed if the time passed from the last
/// execution is  greater than configured purging interval.
/// </remarks>
[PublicAPI]
public abstract class TimeBasedCacheInvalidation
  : ICacheInvalidation, ICacheInvalidationNotifier, ICacheInvalidationSynchronicityController {
  /// <summary>
  /// Initializes a new instance of a time-based cache invalidation.
  /// </summary>
  /// <param name="settings">Time-based cache invalidation settings.</param>
  /// <param name="minimalExpiredEntriesPurgingInterval">Minimal purging interval allowed.</param>
  /// <param name="timeProvider">Time provider.</param>
  /// <param name="logger">Logger.</param>
  /// <exception cref="ArgumentNullException">
  /// Value of a required parameter isn't specified.
  /// </exception>
  /// <exception cref="ArgumentOutOfRangeException">
  /// <paramref name="settings"/>.ExpiredEntriesPurgingInterval is less than
  /// <paramref name="minimalExpiredEntriesPurgingInterval"/>.
  /// </exception>
  protected TimeBasedCacheInvalidation(
    TimeBasedCacheInvalidationSettings settings,
    TimeSpan minimalExpiredEntriesPurgingInterval,
    TimeProvider timeProvider,
    ILogger? logger = null) {
    ArgumentNullException.ThrowIfNull(settings);
    ArgumentNullException.ThrowIfNull(timeProvider);
    if (settings.ExpiredEntriesPurgingInterval < minimalExpiredEntriesPurgingInterval) {
      throw new ArgumentOutOfRangeException(
        nameof(settings),
        $"Expired entries purging interval {settings.ExpiredEntriesPurgingInterval} is less "
        + $"than minimal allowed value {minimalExpiredEntriesPurgingInterval}.");
    }

    _settings = settings;
    _timeProvider = timeProvider;
    Logger = logger ?? new NullLogger<TimeBasedCacheInvalidation>();
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
  /// If both arguments not provided returns current UTC-time plus
  /// <see cref="TimeBasedCacheInvalidationSettings.DefaultSlidingExpirationInterval"/> value.
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
      return _timeProvider.GetUtcNow().Add(_settings.DefaultSlidingExpirationInterval);
    }

    var slidingExpirationUtc = _timeProvider.GetUtcNow().Add(slidingExpiration.Value);
    return absoluteExpirationUtc.Value <= slidingExpirationUtc ? absoluteExpirationUtc.Value : slidingExpirationUtc;
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
  /// Notify cache invalidation started.
  /// </summary>
  protected void NotifyPurgeStarted() => CacheInvalidationStarted?.Invoke(this, EventArgs.Empty);

  /// <summary>
  /// Notify cache invalidation completed.
  /// </summary>
  /// <param name="totalCount">Total cache entries scanned.</param>
  /// <param name="purgedCount">Cache entries purged.</param>
  protected void NotifyPurgeCompleted(uint totalCount, uint purgedCount) =>
    CacheInvalidationCompleted?.Invoke(this, new CacheInvalidationStatistics(totalCount, purgedCount));

  private bool ShouldPurgeEntries() {
    var utcNow = _timeProvider.GetUtcNow();
    var timePassedSinceTheLastPurging = utcNow - _lastExpirationScan;
    if (timePassedSinceTheLastPurging < _settings.ExpiredEntriesPurgingInterval) {
      Logger.LogDebug(
        "Since the last cache invalidation {TimePassed} has passed that is less than {PurgingInterval}. Purging is not required",
        timePassedSinceTheLastPurging,
        _settings.ExpiredEntriesPurgingInterval);
      return false;
    }

    Logger.LogDebug(
      "Since the last cache invalidation {TimePassed} has passed that is greeter than or equals to {PurgingInterval}. Purging is required",
      timePassedSinceTheLastPurging,
      _settings.ExpiredEntriesPurgingInterval);
    return true;
  }

  /// <inheritdoc/>
  public event EventHandler? CacheInvalidationStarted;

  /// <inheritdoc/>
  public event EventHandler<CacheInvalidationStatistics>? CacheInvalidationCompleted;

  private readonly TimeBasedCacheInvalidationSettings _settings;
  private readonly TimeProvider _timeProvider;
  private byte _isPurgingInProgress;
  private DateTimeOffset _lastExpirationScan;
}
