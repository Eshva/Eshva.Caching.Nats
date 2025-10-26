using JetBrains.Annotations;

namespace Eshva.Caching.Abstractions;

/// <summary>
/// Contract of a cache invalidation.
/// </summary>
[PublicAPI]
public interface ICacheInvalidation {
  /// <summary>
  /// Execute scan for expired cache entries if required according to behavior logic.
  /// </summary>
  /// <param name="token">Cancellation token.</param>
  Task PurgeEntriesIfRequired(CancellationToken token = default);
}
