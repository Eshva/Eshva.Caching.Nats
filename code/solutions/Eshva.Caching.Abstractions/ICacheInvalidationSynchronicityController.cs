using JetBrains.Annotations;

namespace Eshva.Caching.Abstractions;

/// <summary>
/// Contract of cache invalidation synchronicity controller.
/// </summary>
[PublicAPI]
public interface ICacheInvalidationSynchronicityController {
  /// <summary>
  /// Should cache invalidation wait until purge finishes.
  /// </summary>
  bool ShouldPurgeSynchronously { get; set; }
}
