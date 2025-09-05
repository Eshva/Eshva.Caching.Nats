using System.Buffers;
using JetBrains.Annotations;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NATS.Client.ObjectStore;

namespace Eshva.Caching.Nats;

[PublicAPI]
public sealed class NatsObjectStoreBaseCache : IBufferDistributedCache, IDisposable {
  public NatsObjectStoreBaseCache(
    INatsObjStore cacheBucket,
    NatsCacheSettings settings,
    ISystemClock clock,
    ILogger<NatsObjectStoreBaseCache>? logger = null) {
    ArgumentNullException.ThrowIfNull(cacheBucket);
    ArgumentNullException.ThrowIfNull(settings);
    ArgumentNullException.ThrowIfNull(clock);

    var settingErrors = settings.Validate();
    if (settingErrors.Any()) {
      throw new ArgumentException(
        "Settings contain the following errors:" + Environment.NewLine + string.Join(Environment.NewLine, settingErrors));
    }

    _cacheBucket = cacheBucket;
    _settings = settings;
    _clock = clock;
    _logger = logger ?? new NullLogger<NatsObjectStoreBaseCache>();
    _lastExpirationScan = clock.UtcNow;
  }

  /// <summary>
  /// Get value of a cache key <paramref name="key"/>.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Read the key <paramref name="key"/> value from the object-store bucket and returns it as a byte array if it's
  /// found.
  /// </para>
  /// <para>
  /// If it's time purge all expired entries in the cache.
  /// </para>
  /// </remarks>
  /// <param name="key"></param>
  /// <param name="token"></param>
  /// <returns>
  /// <para></para>
  /// Depending on different circumstances returns:
  /// <list type="bullet">
  /// <item>byte array - read key value,</item>
  /// <item>null - cache key <paramref name="key"/> isn't found.</item>
  /// </list>
  /// </returns>
  /// <exception cref="ArgumentException">
  /// The key is not specified.
  /// </exception>
  /// <exception cref="InvalidOperationException">
  /// One of:
  /// <list type="bullet">
  /// <item>Failed to read cache key value.</item>
  /// <item>Length of the read value less than length of the cache value.</item>
  /// </list>
  /// </exception>
  public byte[]? Get(string key) => GetAsync(key).GetAwaiter().GetResult();

  /// <summary>
  /// Get value of a cache key <paramref name="key"/>.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Read the key <paramref name="key"/> value from the object-store bucket and returns it as a byte array if it's
  /// found.
  /// </para>
  /// <para>
  /// If it's time purge all expired entries in the cache.
  /// </para>
  /// </remarks>
  /// <param name="key"></param>
  /// <param name="token"></param>
  /// <returns>
  /// <para></para>
  /// Depending on different circumstances returns:
  /// <list type="bullet">
  /// <item>byte array - read key value,</item>
  /// <item>null - cache key <paramref name="key"/> isn't found.</item>
  /// </list>
  /// </returns>
  /// <exception cref="ArgumentException">
  /// The key is not specified.
  /// </exception>
  /// <exception cref="InvalidOperationException">
  /// One of:
  /// <list type="bullet">
  /// <item>Failed to read cache key value.</item>
  /// <item>Length of the read value less than length of the cache value.</item>
  /// </list>
  /// </exception>
  public async Task<byte[]?> GetAsync(string key, CancellationToken token = default) {
    ValidateKey(key);

    var valueStream = new MemoryStream();

    try {
      var objectMetadata = await _cacheBucket.GetAsync(
        key,
        valueStream,
        leaveOpen: true,
        token);
      _logger.LogDebug(
        "An object with the key '{Key}' has been read. Object meta-data: @{ObjectMetadata}",
        key,
        objectMetadata);
    }
    catch (NatsObjNotFoundException) {
      return null;
    }
    catch (NatsObjException exception) {
      throw new InvalidOperationException($"Failed to read cache key '{key}' value.", exception);
    }

    if (valueStream.Length == 0) {
      _logger.LogDebug("No key '{Key}' has been found in the object-store", key);
      return null;
    }

    valueStream.Seek(offset: 0, SeekOrigin.Begin);
    var buffer = new byte[valueStream.Length];
    var bytesRead = await valueStream.ReadAsync(buffer, token);

    if (bytesRead != valueStream.Length) {
      throw new InvalidOperationException(
        $"Should be read {valueStream.Length} bytes but read {bytesRead} bytes for a cache entry wih ID '{key}'.");
    }

    ScanForExpiredItemsIfRequired(token);
    return buffer;
  }

  /// TODO: COPY LATER!
  public void Set(string key, byte[] value, DistributedCacheEntryOptions options) =>
    throw new NotImplementedException();

  public async Task SetAsync(
    string key,
    byte[] value,
    DistributedCacheEntryOptions options,
    CancellationToken token = new()) =>
    throw new NotImplementedException();

  /// TODO: COPY LATER!
  public void Refresh(string key) => throw new NotImplementedException();

  public async Task RefreshAsync(string key, CancellationToken token = new()) => throw new NotImplementedException();

  /// TODO: COPY LATER!
  public void Remove(string key) => throw new NotImplementedException();

  public async Task RemoveAsync(string key, CancellationToken token = new()) => throw new NotImplementedException();

  /// TODO: COPY LATER!
  public bool TryGet(string key, IBufferWriter<byte> destination) => throw new NotImplementedException();

  public async ValueTask<bool>
    TryGetAsync(string key, IBufferWriter<byte> destination, CancellationToken token = new()) =>
    throw new NotImplementedException();

  public void Set(string key, ReadOnlySequence<byte> value, DistributedCacheEntryOptions options) =>
    throw new NotImplementedException();

  public async ValueTask SetAsync(
    string key,
    ReadOnlySequence<byte> value,
    DistributedCacheEntryOptions options,
    CancellationToken token = new()) =>
    throw new NotImplementedException();

  public void Dispose() { }

  private void ScanForExpiredItemsIfRequired(CancellationToken token) {
    lock (_scanForExpiredItemsLock) {
      var utcNow = _clock.UtcNow;
      var timePassedSinceTheLastPurging = utcNow - _lastExpirationScan;
      if (timePassedSinceTheLastPurging < _settings.ExpiredEntriesPurgingInterval) {
        _logger.LogDebug(
          "Since the last purging expired entries {TimePassed} has passed that is less than {PurgingInterval}. Purging is not required",
          timePassedSinceTheLastPurging,
          _settings.ExpiredEntriesPurgingInterval);
        return;
      }

      _logger.LogDebug(
        "Since the last purging expired entries {TimePassed} has passed that is greeter than or equals {PurgingInterval}. Purging is required",
        timePassedSinceTheLastPurging,
        _settings.ExpiredEntriesPurgingInterval);
      _lastExpirationScan = utcNow;
      Task.Run(() => DeleteExpiredCachedEntries(token), token);
    }
  }

  private async Task DeleteExpiredCachedEntries(CancellationToken token) {
    _logger.LogDebug("Deleting expired entries started");
    var entries = _cacheBucket.ListAsync(cancellationToken: token);

    await foreach (var entry in entries) {
      if (EntryMetadata(entry.Metadata).ExpiresOnUtc.Ticks >= _clock.UtcNow.Ticks) continue;

      await _cacheBucket.DeleteAsync(entry.Name, token);
      _logger.LogDebug("Deleted expired entry '{Name}'", entry.Name);
    }
  }

  private static CacheEntryMetadata EntryMetadata(Dictionary<string, string>? entryMetadata) =>
    new(entryMetadata ?? new Dictionary<string, string>());

  private string GetCurrentTimeAsString() => _clock.UtcNow.Ticks.ToString();

  private static void ValidateKey(string key) =>
    ArgumentException.ThrowIfNullOrWhiteSpace(key, "The key is not specified.");

  private readonly ILogger<NatsObjectStoreBaseCache> _logger;
  private readonly NatsCacheSettings _settings;
  private INatsObjStore _cacheBucket;
  private ISystemClock _clock;
  private DateTimeOffset _lastExpirationScan;
  private Lock _scanForExpiredItemsLock = new();
  private const int DefaultBucketSizeInMebibytes = 100;
  private const string ExpireOnMetadataKey = "ExpireOn";
}
