using JetBrains.Annotations;

namespace Eshva.Caching.Abstractions;

/// <summary>
/// Contract of cache invalidation notifier.
/// </summary>
[PublicAPI]
public interface ICacheInvalidationNotifier {
  /// <summary>
  /// Notifies cache invalidation is started.
  /// </summary>
  event EventHandler CacheInvalidationStarted;

  /// <summary>
  /// Notifies cache invalidation is completed.
  /// </summary>
  event EventHandler<CacheInvalidationStatistics> CacheInvalidationCompleted;
}
