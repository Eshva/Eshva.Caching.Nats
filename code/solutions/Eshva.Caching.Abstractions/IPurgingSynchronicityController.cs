using JetBrains.Annotations;

namespace Eshva.Caching.Abstractions;

/// <summary>
/// Contract of purging synchronicity controller.
/// </summary>
[PublicAPI]
public interface IPurgingSynchronicityController {
  /// <summary>
  /// Should purger wait until purge finishes its work.
  /// </summary>
  bool ShouldPurgeSynchronously { get; set; }
}
