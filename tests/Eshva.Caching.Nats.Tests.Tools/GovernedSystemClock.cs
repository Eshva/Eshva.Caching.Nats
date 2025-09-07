using Microsoft.Extensions.Internal;

namespace Eshva.Caching.Nats.Tests.Tools;

/// <summary>
/// System clock that time could be adjusted. A mock of clock.
/// </summary>
public sealed class GovernedSystemClock : ISystemClock {
  public GovernedSystemClock(DateTimeOffset utcNow) {
    _utcNow = utcNow;
  }

  public DateTimeOffset UtcNow => _utcNow;

  public void AdjustTime(TimeSpan adjustment) => _utcNow = _utcNow.Add(adjustment);

  private DateTimeOffset _utcNow;
}
