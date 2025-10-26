using JetBrains.Annotations;

namespace Eshva.Caching.Abstractions;

/// <summary>
/// Cache invalidation statistics.
/// </summary>
/// <param name="TotalEntriesCount">Total cache entries scanned.</param>
/// <param name="PurgedEntriesCount">Cache entries purged.</param>
[PublicAPI]
public readonly record struct CacheInvalidationStatistics(uint TotalEntriesCount, uint PurgedEntriesCount);
