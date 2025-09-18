using JetBrains.Annotations;

namespace Eshva.Caching.Abstractions;

/// <summary>
/// Contract of purging progress notifier.
/// </summary>
[PublicAPI]
public interface IPurgingNotifier {
  /// <summary>
  /// Notifies purge is started.
  /// </summary>
  event EventHandler PurgeStarted;

  /// <summary>
  /// Notifies purge is completed.
  /// </summary>
  event EventHandler<PurgeStatistics> PurgeCompleted;
}
