namespace Eshva.Caching.Abstractions;

public interface IGetEntryAsByteArrayAsync {
  Task<byte[]?> GetAsync(string key, CancellationToken token = default);
}
