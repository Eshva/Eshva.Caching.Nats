using System.Runtime.CompilerServices;
using Eshva.Caching.Abstractions;
using Eshva.Caching.Nats.Tests.OutOfProcess.Common;
using NATS.Client.Core;
using NATS.Client.KeyValueStore;
using Xunit.Abstractions;

namespace Eshva.Caching.Nats.Tests.OutOfProcess.KeyValueBasedCache;

public sealed class KeyValueStoreDriver : ICacheStorageDriver {
  public KeyValueStoreDriver(
    INatsKVStore entriesStore,
    INatsSerializer<CacheEntryExpiry> expirySerializer,
    ITestOutputHelper logger) {
    _entriesKeyValueStore = entriesStore ?? throw new ArgumentNullException(nameof(entriesStore));
    _expirySerializer = expirySerializer ?? throw new ArgumentNullException(nameof(expirySerializer));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
  }

  public async Task PutEntry(string key, byte[] value, CacheEntryExpiry entryExpiry) {
    await _entriesKeyValueStore.PutAsync(key, value).ConfigureAwait(continueOnCapturedContext: false);
    await _entriesKeyValueStore.PutAsync(MakeMetadataKey(key), entryExpiry, _expirySerializer)
      .ConfigureAwait(continueOnCapturedContext: false);
    _logger.WriteLine($"Put entry '{key}' that expires at {entryExpiry.ExpiresAtUtc}");
  }

  public async Task<bool> DoesExist(string key) {
    var valueStatus = await _entriesKeyValueStore.TryGetEntryAsync(MakeMetadataKey(key), serializer: _expirySerializer)
      .ConfigureAwait(continueOnCapturedContext: false);
    var metadataStatus = await _entriesKeyValueStore.TryGetEntryAsync<byte[]>(key).ConfigureAwait(continueOnCapturedContext: false);
    return valueStatus.Success && metadataStatus.Success;
  }

  public async Task<CacheEntryExpiry> GetMetadata(string key) {
    var metadataStatus = await _entriesKeyValueStore.TryGetEntryAsync(MakeMetadataKey(key), serializer: _expirySerializer)
      .ConfigureAwait(continueOnCapturedContext: false);
    return metadataStatus.Value.Value;
  }

  public async Task Remove(string key) {
    await _entriesKeyValueStore.PurgeAsync(MakeMetadataKey(key)).ConfigureAwait(continueOnCapturedContext: false);
    await _entriesKeyValueStore.PurgeAsync(key).ConfigureAwait(continueOnCapturedContext: false);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private static string MakeMetadataKey(string key) => $"{key}{MetadataSuffix}";

  private readonly INatsKVStore _entriesKeyValueStore;
  private readonly INatsSerializer<CacheEntryExpiry> _expirySerializer;
  private readonly ITestOutputHelper _logger;
  private const string MetadataSuffix = "-metadata";
}
