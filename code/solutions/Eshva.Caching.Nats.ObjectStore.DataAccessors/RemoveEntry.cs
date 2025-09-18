using Eshva.Caching.Abstractions;

namespace Eshva.Caching.Nats.ObjectStore.DataAccessors;

public class RemoveEntry : IRemoveEntry {
  public RemoveEntry(IRemoveEntryAsync accessor) {
    _accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));
  }

  public void Remove(string key) =>
    _accessor.RemoveAsync(key, CancellationToken.None).GetAwaiter().GetResult();

  private readonly IRemoveEntryAsync _accessor;
}