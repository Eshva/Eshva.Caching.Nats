namespace Eshva.Caching.Nats;

/// <summary>
/// 
/// </summary>
/// <param name="ExpiresAtUtc"></param>
/// <param name="AbsoluteExpirationUtc"></param>
/// <param name="SlidingExpiration"></param>
public readonly record struct CacheEntryExpiry(
  DateTimeOffset ExpiresAtUtc,
  DateTimeOffset? AbsoluteExpirationUtc,
  TimeSpan? SlidingExpiration);
