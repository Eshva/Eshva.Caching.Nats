namespace Eshva.Caching.Nats;

/// <summary>
/// Contract of a cache expired entries purger.
/// </summary>
public interface ICacheExpiredEntriesPurger {
  /// <summary>
  /// Execute scan for expired cache entries if required according to behavior logic.
  /// </summary>
  /// <param name="token">Cancellation token.</param>
  void ScanForExpiredEntriesIfRequired(CancellationToken token = default);
}
