using Microsoft.Extensions.Internal;

namespace Eshva.Caching.Nats.Tests.Tools;

/// <summary>
/// System clock that time could be adjusted. A mock of clock.
/// </summary>
public sealed class GovernedSystemClock : ISystemClock {
  public DateTimeOffset UtcNow => _adjustedTime?.UtcDateTime ?? DateTimeOffset.UtcNow;

  public void AdjustTime(DateTimeOffset utcNow) => _adjustedTime = utcNow;

  public void Reset() => _adjustedTime = null;

  private DateTimeOffset? _adjustedTime;
}
