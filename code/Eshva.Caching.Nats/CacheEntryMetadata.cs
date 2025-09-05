using System.Globalization;

namespace Eshva.Caching.Nats;

/// <summary>
/// A NATS cache entry meta-data accessor.
/// </summary>
internal class CacheEntryMetadata {
  /// <summary>
  /// Initialize a new NATS cache entry meta-data accessor instance with entry meta-data dictionary.
  /// </summary>
  /// <param name="entryMetadata">Entry meta-data dictionary.</param>
  /// <exception cref="ArgumentNullException">
  /// Entry meta-data dictionary is not specified.
  /// </exception>
  public CacheEntryMetadata(Dictionary<string, string>? entryMetadata) {
    _entryMetadata = entryMetadata ?? new Dictionary<string, string>();
  }

  /// <summary>
  /// Gets the entry expiration moment in time.
  /// </summary>
  /// <remarks>
  /// UTC-based time in ticks.
  /// </remarks>
  /// <value>
  /// <list type="bullet">
  /// <item>If the value set in expires on meta-data entry then returns this value.</item>
  /// <item>If expires on meta-data value isn't set returns that never expires.</item>
  /// <item>If the value is set but can not be parsed return that never expires.</item>
  /// </list>
  /// </value>
  public DateTimeOffset ExpiresOnUtc {
    get =>
      _entryMetadata.TryGetValue(ExpiresOnValueName, out var expiresOn)
        ? long.TryParse(expiresOn, CultureInfo.InvariantCulture, out var result) ? new DateTimeOffset(result, TimeSpan.Zero) : NeverExpires
        : NeverExpires;
    set => _entryMetadata[ExpiresOnValueName] = value.Ticks.ToString(CultureInfo.InvariantCulture);
  }

  public static implicit operator Dictionary<string, string>(CacheEntryMetadata metadata) => metadata._entryMetadata;

  private readonly Dictionary<string, string> _entryMetadata;
  private const string ExpiresOnValueName = "ExpiresOn";
  private static readonly DateTimeOffset NeverExpires = DateTimeOffset.MaxValue;
}
