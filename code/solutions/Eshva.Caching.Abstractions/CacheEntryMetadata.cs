using System.Globalization;

namespace Eshva.Caching.Abstractions;

/// <summary>
/// A NATS cache entry meta-data accessor.
/// </summary>
public class CacheEntryMetadata {
  /// <summary>
  /// Initialize a new NATS cache entry meta-data accessor instance with entry meta-data dictionary.
  /// </summary>
  /// <param name="entryMetadata">Entry meta-data dictionary.</param>
  /// <exception cref="ArgumentNullException">
  /// Entry meta-data dictionary is not specified.
  /// </exception>
  public CacheEntryMetadata(Dictionary<string, string>? entryMetadata = null) {
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
  public DateTimeOffset ExpiresAtUtc {
    get =>
      _entryMetadata.TryGetValue(nameof(ExpiresAtUtc), out var expiresAtUtc)
        ? long.TryParse(expiresAtUtc, CultureInfo.InvariantCulture, out var result)
          ? new DateTimeOffset(result, TimeSpan.Zero)
          : NeverExpires
        : NeverExpires;
    set => _entryMetadata[nameof(ExpiresAtUtc)] = value.Ticks.ToString(CultureInfo.InvariantCulture);
  }

  public DateTimeOffset? AbsoluteExpirationUtc {
    get {
      if (!_entryMetadata.TryGetValue(nameof(AbsoluteExpirationUtc), out var absoluteExpiration)) return null;
      if (DateTimeOffset.TryParseExact(
            absoluteExpiration,
            "O",
            CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal,
            out var result)) {
        return result;
      }

      return null;
    }
    set {
      switch (value) {
        case null:
          _entryMetadata.Remove(nameof(AbsoluteExpirationUtc));
          return;
        default: _entryMetadata[nameof(AbsoluteExpirationUtc)] = value.Value.ToString("O", CultureInfo.InvariantCulture); break;
      }
    }
  }

  public TimeSpan? SlidingExpiration {
    get {
      if (!_entryMetadata.TryGetValue(nameof(SlidingExpiration), out var slidingExpiration)) return null;
      if (TimeSpan.TryParseExact(
            slidingExpiration,
            "G",
            CultureInfo.InvariantCulture,
            out var result)) {
        return result;
      }

      return null;
    }
    set {
      switch (value) {
        case null:
          _entryMetadata.Remove(nameof(SlidingExpiration));
          return;
        default: _entryMetadata[nameof(SlidingExpiration)] = value.Value.ToString("G", CultureInfo.InvariantCulture); break;
      }
    }
  }

  public static implicit operator Dictionary<string, string>(CacheEntryMetadata metadata) => metadata._entryMetadata;

  private readonly Dictionary<string, string> _entryMetadata;
  private static readonly DateTimeOffset NeverExpires = DateTimeOffset.MaxValue;
}
