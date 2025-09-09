using JetBrains.Annotations;

namespace Eshva.Caching.Nats;

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
