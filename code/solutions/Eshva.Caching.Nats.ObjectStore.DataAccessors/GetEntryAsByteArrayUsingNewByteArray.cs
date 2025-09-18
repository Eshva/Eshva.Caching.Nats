using Eshva.Caching.Abstractions;

namespace Eshva.Caching.Nats.ObjectStore.DataAccessors;

public class GetEntryAsByteArrayUsingNewByteArray : IGetEntryAsByteArray {
  public GetEntryAsByteArrayUsingNewByteArray(IGetEntryAsByteArrayAsync accessor) {
    _accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));
  }

  public byte[]? Get(string key) => _accessor.GetAsync(key).GetAwaiter().GetResult();

  private readonly IGetEntryAsByteArrayAsync _accessor;
}
