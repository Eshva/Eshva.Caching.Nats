using Eshva.Caching.Abstractions;

namespace Eshva.Caching.Nats.Tests.OutOfProcess.Common;

public interface ICacheStorageDriver {
  Task PutEntry(
    string key,
    byte[] value,
    CacheEntryExpiry entryExpiry);

  Task<bool> DoesExist(string key);

  Task<CacheEntryExpiry> GetMetadata(string key);

  Task Remove(string key);
}
