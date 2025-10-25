namespace Eshva.Caching.Abstractions;

/// <summary>
/// Contract of a cache expired entries purger.
/// </summary>
public interface ICacheInvalidator {
  /// <summary>
  /// Execute scan for expired cache entries if required according to behavior logic.
  /// </summary>
  /// <param name="token">Cancellation token.</param>
  Task PurgeEntriesIfRequired(CancellationToken token = default);
}
