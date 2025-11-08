using Eshva.Caching.Abstractions;
using Eshva.Caching.Nats.Tests.OutOfProcess.Common;
using NATS.Client.Core;
using NATS.Client.KeyValueStore;
using Xunit.Abstractions;

namespace Eshva.Caching.Nats.Tests.OutOfProcess.KeyValueBasedCache;

public sealed class KeyValueStoreDriver : ICacheStorageDriver {
  public KeyValueStoreDriver(
    INatsKVStore entryValueKeyValueStore,
    INatsKVStore entryMetadataKeyValueStore,
    INatsSerializer<CacheEntryExpiry> expirySerializer,
    ITestOutputHelper logger) {
    _entryValueKeyValueStore = entryValueKeyValueStore ?? throw new ArgumentNullException(nameof(entryValueKeyValueStore));
    _entryMetadataKeyValueStore = entryMetadataKeyValueStore ?? throw new ArgumentNullException(nameof(entryMetadataKeyValueStore));
    _expirySerializer = expirySerializer ?? throw new ArgumentNullException(nameof(expirySerializer));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
  }

  public async Task PutEntry(string key, byte[] value, CacheEntryExpiry entryExpiry) {
    await _entryValueKeyValueStore.PutAsync(key, value);
    await _entryMetadataKeyValueStore.PutAsync(key, entryExpiry, _expirySerializer);
    _logger.WriteLine($"Put entry '{key}' that expires at {entryExpiry.ExpiresAtUtc}");
  }

  public async Task<bool> DoesExist(string key) {
    var valueStatus = await _entryMetadataKeyValueStore.TryGetEntryAsync(key, serializer: _expirySerializer);
    var metadataStatus = await _entryValueKeyValueStore.TryGetEntryAsync<byte[]>(key);
    return valueStatus.Success && metadataStatus.Success;
  }

  public async Task<CacheEntryExpiry> GetMetadata(string key) {
    var metadataStatus = await _entryMetadataKeyValueStore.TryGetEntryAsync(key, serializer: _expirySerializer);
    return metadataStatus.Value.Value;
  }

  public async Task Remove(string key) {
    await _entryMetadataKeyValueStore.PurgeAsync(key);
    await _entryValueKeyValueStore.PurgeAsync(key);
  }

  private readonly INatsKVStore _entryValueKeyValueStore;
  private readonly INatsKVStore _entryMetadataKeyValueStore;
  private readonly INatsSerializer<CacheEntryExpiry> _expirySerializer;
  private readonly ITestOutputHelper _logger;
}
