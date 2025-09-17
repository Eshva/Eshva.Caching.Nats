using JetBrains.Annotations;

namespace Eshva.Caching.Nats;

/// <summary>
/// Completed purge operation statistics.
/// </summary>
/// <param name="TotalEntriesCount">Total cache entries scanned.</param>
/// <param name="PurgedEntriesCount">Cache entries purged.</param>
[PublicAPI]
public readonly record struct PurgeStatistics(uint TotalEntriesCount, uint PurgedEntriesCount);
