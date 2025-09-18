using Eshva.Caching.Abstractions;

namespace Eshva.Caching.Nats.ObjectStore.DataAccessors;

public class RefreshEntryUsingMetadata : IRefreshEntry {
  public RefreshEntryUsingMetadata(IRefreshEntryAsync accessor) {
    _accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));
  }

  public void Refresh(string key) => _accessor.RefreshAsync(key).GetAwaiter().GetResult();

  private readonly IRefreshEntryAsync _accessor;
}
