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
    await _entryValueKeyValueStore.PutAsync(key, value).ConfigureAwait(continueOnCapturedContext: false);
    await _entryMetadataKeyValueStore.PutAsync(key, entryExpiry, _expirySerializer).ConfigureAwait(continueOnCapturedContext: false);
    _logger.WriteLine($"Put entry '{key}' that expires at {entryExpiry.ExpiresAtUtc}");
  }

  public async Task<bool> DoesExist(string key) {
    var valueStatus = await _entryMetadataKeyValueStore.TryGetEntryAsync(key, serializer: _expirySerializer)
      .ConfigureAwait(continueOnCapturedContext: false);
    var metadataStatus = await _entryValueKeyValueStore.TryGetEntryAsync<byte[]>(key).ConfigureAwait(continueOnCapturedContext: false);
    return valueStatus.Success && metadataStatus.Success;
  }

  public async Task<CacheEntryExpiry> GetMetadata(string key) {
    var metadataStatus = await _entryMetadataKeyValueStore.TryGetEntryAsync(key, serializer: _expirySerializer)
      .ConfigureAwait(continueOnCapturedContext: false);
    return metadataStatus.Value.Value;
  }

  public async Task Remove(string key) {
    await _entryMetadataKeyValueStore.PurgeAsync(key).ConfigureAwait(continueOnCapturedContext: false);
    await _entryValueKeyValueStore.PurgeAsync(key).ConfigureAwait(continueOnCapturedContext: false);
  }

  private readonly INatsKVStore _entryValueKeyValueStore;
  private readonly INatsKVStore _entryMetadataKeyValueStore;
  private readonly INatsSerializer<CacheEntryExpiry> _expirySerializer;
  private readonly ITestOutputHelper _logger;
}
