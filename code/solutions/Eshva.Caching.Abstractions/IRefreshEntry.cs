namespace Eshva.Caching.Abstractions;

public interface IRefreshEntry {
  void Refresh(string key);
}
