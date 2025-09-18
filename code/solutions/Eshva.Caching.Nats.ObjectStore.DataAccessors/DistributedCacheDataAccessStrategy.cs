using System.Runtime.CompilerServices;
using Eshva.Caching.Abstractions;
using Microsoft.Extensions.Caching.Distributed;

namespace Eshva.Caching.Nats.ObjectStore.DataAccessors;

public class DistributedCacheDataAccessStrategy : IDistributedCacheDataAccessStrategy {
  public DistributedCacheDataAccessStrategy(
    IGetEntryAsByteArray getEntryAsByteArray,
    IGetEntryAsByteArrayAsync getEntryAsByteArrayAsync,
    ISetEntryWithByteArray setEntryWithByteArray,
    ISetEntryWithByteArrayAsync setEntryWithByteArrayAsync,
    IRefreshEntry refreshEntry,
    IRefreshEntryAsync refreshEntryAsync,
    IRemoveEntry removeEntry,
    IRemoveEntryAsync removeEntryAsync) {
    _getEntryAsByteArray = getEntryAsByteArray ?? throw new ArgumentNullException(nameof(getEntryAsByteArray));
    _getEntryAsByteArrayAsync = getEntryAsByteArrayAsync ?? throw new ArgumentNullException(nameof(getEntryAsByteArrayAsync));
    _setEntryWithByteArray = setEntryWithByteArray ?? throw new ArgumentNullException(nameof(setEntryWithByteArray));
    _setEntryWithByteArrayAsync = setEntryWithByteArrayAsync ?? throw new ArgumentNullException(nameof(setEntryWithByteArrayAsync));
    _refreshEntry = refreshEntry ?? throw new ArgumentNullException(nameof(refreshEntry));
    _refreshEntryAsync = refreshEntryAsync ?? throw new ArgumentNullException(nameof(refreshEntryAsync));
    _removeEntry = removeEntry ?? throw new ArgumentNullException(nameof(removeEntry));
    _removeEntryAsync = removeEntryAsync ?? throw new ArgumentNullException(nameof(removeEntryAsync));
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  byte[]? IGetEntryAsByteArray.Get(string key) =>
    _getEntryAsByteArray.Get(key);

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  Task<byte[]?> IGetEntryAsByteArrayAsync.GetAsync(
    string key,
    CancellationToken token = default) =>
    _getEntryAsByteArrayAsync.GetAsync(key, token);

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  void ISetEntryWithByteArray.Set(
    string key,
    byte[] value,
    DistributedCacheEntryOptions options) =>
    _setEntryWithByteArray.Set(key, value, options);

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  Task ISetEntryWithByteArrayAsync.SetAsync(
    string key,
    byte[] value,
    DistributedCacheEntryOptions options,
    CancellationToken token = default) =>
    _setEntryWithByteArrayAsync.SetAsync(
      key,
      value,
      options,
      token);

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  void IRefreshEntry.Refresh(string key) =>
    _refreshEntry.Refresh(key);

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  Task IRefreshEntryAsync.RefreshAsync(string key, CancellationToken token = default) =>
    _refreshEntryAsync.RefreshAsync(key, token);

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  void IRemoveEntry.Remove(string key) =>
    _removeEntry.Remove(key);

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  Task IRemoveEntryAsync.RemoveAsync(string key, CancellationToken token = default) =>
    _removeEntryAsync.RemoveAsync(key, token);

  private readonly IGetEntryAsByteArray _getEntryAsByteArray;
  private readonly IGetEntryAsByteArrayAsync _getEntryAsByteArrayAsync;
  private readonly IRefreshEntry _refreshEntry;
  private readonly IRefreshEntryAsync _refreshEntryAsync;
  private readonly IRemoveEntry _removeEntry;
  private readonly IRemoveEntryAsync _removeEntryAsync;
  private readonly ISetEntryWithByteArray _setEntryWithByteArray;
  private readonly ISetEntryWithByteArrayAsync _setEntryWithByteArrayAsync;
}
