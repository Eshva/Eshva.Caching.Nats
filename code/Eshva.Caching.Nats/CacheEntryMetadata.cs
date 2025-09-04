namespace Eshva.Caching.Nats;

/// <summary>
/// A NATS cache entry meta-data accessor.
/// </summary>
internal class CacheEntryMetadata {
  private const string ExpiresOnValueName = "ExpiresOn";
  private static readonly long NeverExpires = DateTimeOffset.MaxValue.Ticks;
  private readonly Dictionary<string, string> _entryMetadata;

  /// <summary>
  /// Initialize a new NATS cache entry meta-data accessor instance with entry meta-data dictionary.
  /// </summary>
  /// <param name="entryMetadata">Entry meta-data dictionary.</param>
  /// <exception cref="ArgumentNullException">
  /// Entry meta-data dictionary is not specified.
  /// </exception>
  public CacheEntryMetadata(Dictionary<string, string> entryMetadata) {
    _entryMetadata = entryMetadata ?? throw new ArgumentNullException(nameof(entryMetadata));
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
  public long ExpiresOn =>
    _entryMetadata.TryGetValue(ExpiresOnValueName, out var expiresOn)
      ? int.TryParse(expiresOn, out var ticks) ? ticks : NeverExpires
      : NeverExpires;
}