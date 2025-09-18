namespace Eshva.Caching.Abstractions;

public interface IRemoveEntryAsync {
  Task RemoveAsync(string key, CancellationToken token = default);
}