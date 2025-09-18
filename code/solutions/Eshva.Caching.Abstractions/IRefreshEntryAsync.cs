namespace Eshva.Caching.Abstractions;

public interface IRefreshEntryAsync {
  Task RefreshAsync(string key, CancellationToken token = default);
}
