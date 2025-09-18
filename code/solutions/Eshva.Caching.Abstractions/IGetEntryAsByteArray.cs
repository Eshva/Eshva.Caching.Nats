namespace Eshva.Caching.Abstractions;

public interface IGetEntryAsByteArray {
  byte[]? Get(string key);
}
