using System.Globalization;
using NATS.Client.ObjectStore.Models;

namespace Eshva.Caching.Nats.Distributed;

/// <summary>
/// A NATS cache entry metadata accessor.
/// </summary>
internal sealed class ObjectMetadataAccessor {
  /// <summary>
  /// Initialize a new NATS cache entry metadata accessor instance with entry metadata dictionary.
  /// </summary>
  /// <param name="objectMetadata">Cache entry object metadata.</param>
  /// <exception cref="ArgumentNullException">
  /// Cache entry object metadata is not specified.
  /// </exception>
  public ObjectMetadataAccessor(ObjectMetadata objectMetadata) {
    ObjectMetadata = objectMetadata ?? throw new ArgumentNullException(nameof(objectMetadata));
    _entryMetadata = objectMetadata.Metadata = objectMetadata.Metadata ??= new Dictionary<string, string>();
  }

  /// <summary>
  /// Cache entry object metadata.
  /// </summary>
  public ObjectMetadata ObjectMetadata { get; }

  /// <summary>
  /// Gets or sets the cache entry expiry moment UTC.
  /// </summary>
  /// <value>
  /// <list type="bullet">
  /// <item>If object metadata dictionary value set it returns this value.</item>
  /// <item>If object metadata dictionary value isn't set it returns never expires.</item>
  /// <item>If object metadata dictionary value is set but can not be parsed it returns never expires.</item>
  /// </list>
  /// </value>
  public DateTimeOffset ExpiresAtUtc {
    get => _entryMetadata.TryGetValue(nameof(ExpiresAtUtc), out var expiresAtUtc)
      ? long.TryParse(expiresAtUtc, out var result)
        ? new DateTimeOffset(result, TimeSpan.Zero)
        : NeverExpires
      : NeverExpires;
    set => _entryMetadata[nameof(ExpiresAtUtc)] = value.Ticks.ToString(CultureInfo.InvariantCulture);
  }

  /// <summary>
  /// Gets or sets the cache entry absolute expiry moment UTC.
  /// </summary>
  /// <value>
  /// <list type="bullet">
  /// <item>If object metadata dictionary value set it returns this value.</item>
  /// <item>If object metadata dictionary value isn't set it returns <c>null</c>.</item>
  /// <item>If object metadata dictionary value is set but can not be parsed it returns <c>null</c>.</item>
  /// </list>
  /// </value>
  public DateTimeOffset? AbsoluteExpiryAtUtc {
    get => _entryMetadata.TryGetValue(nameof(AbsoluteExpiryAtUtc), out var absoluteExpiryAtUtc)
      ? long.TryParse(absoluteExpiryAtUtc, out var result)
        ? new DateTimeOffset(result, TimeSpan.Zero)
        : null
      : null;
    set {
      switch (value) {
        case null:
          _entryMetadata.Remove(nameof(AbsoluteExpiryAtUtc));
          return;
        default: _entryMetadata[nameof(AbsoluteExpiryAtUtc)] = value.Value.Ticks.ToString(CultureInfo.InvariantCulture); break;
      }
    }
  }

  /// <summary>
  /// Gets or sets the cache entry sliding expiry interval.
  /// </summary>
  /// <value>
  /// <list type="bullet">
  /// <item>If object metadata dictionary value set it returns this value.</item>
  /// <item>If object metadata dictionary value isn't set it returns <c>null</c>.</item>
  /// <item>If object metadata dictionary value is set but can not be parsed it returns <c>null</c>.</item>
  /// </list>
  /// </value>
  public TimeSpan? SlidingExpiryInterval {
    get => _entryMetadata.TryGetValue(nameof(SlidingExpiryInterval), out var slidingExpiration)
      ? long.TryParse(slidingExpiration, out var result)
        ? new TimeSpan(result)
        : null
      : null;
    set {
      switch (value) {
        case null:
          _entryMetadata.Remove(nameof(SlidingExpiryInterval));
          return;
        default: _entryMetadata[nameof(SlidingExpiryInterval)] = value.Value.Ticks.ToString(CultureInfo.InvariantCulture); break;
      }
    }
  }

  private readonly Dictionary<string, string> _entryMetadata;
  private static readonly DateTimeOffset NeverExpires = DateTimeOffset.MaxValue;
}
