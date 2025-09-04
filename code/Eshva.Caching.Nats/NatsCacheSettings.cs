using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace Eshva.Caching.Nats;

/// <summary>
/// Connection settings to Object Store on a NATS-server.
/// </summary>
[PublicAPI]
public sealed record NatsCacheSettings {
  private const int DefaultBucketSizeInMebibytes = 100;
  public static readonly TimeSpan DefaultExpiredEntriesPurgeTaskInterval = TimeSpan.FromMinutes(minutes: 10);
  public static readonly TimeSpan MinimalExpiredItemsPurgeTaskInterval = TimeSpan.FromMinutes(minutes: 1);

  /// <summary>
  /// Bucket name containing cache objects.
  /// </summary>
  /// <remarks>
  /// The name should contain only alphanumeric characters, dashes, and underscores.
  /// </remarks>
  [Required]
  [RegularExpression(
    @"\A[a-zA-Z0-9_-]+\z",
    ErrorMessage = "Bucket name can only contain alphanumeric characters, dashes, and underscores.")]
  public string BucketName { get; set; } = string.Empty;

  /// <summary>
  /// Should create the cache bucket if it's missing.
  /// </summary>
  /// <remarks>
  /// If it's <c>true</c> and the <see cref="BucketName"/> is missing in the object-store, the NATS-cache will try to
  /// create the bucket. The user used to access NATS-server should have rights to do it. From the security point of
  /// view it's better to disallow creating of buckets to the application user and create the bucket beforehand.
  /// </remarks>
  public bool ShouldCreateBucket { get; set; }

  /// <summary>
  /// The cache bucket size if the bucket is missing and creation is allowed.
  /// </summary>
  /// <remarks>
  /// If the bucket size equals 0, the default size of <see cref="DefaultBucketSizeInMebibytes"/> used.
  /// </remarks>
  [Range(minimum: 0, int.MaxValue)]
  public int BucketSizeInMebibytes { get; set; } = DefaultBucketSizeInMebibytes;

  /// <summary>
  /// Interval of purging expired entries in the cache.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Purging is not executed in constant intervals with a timer. It executed in all cache accessing methods if the time
  /// passed from the last execution is greater tha value of this property.
  /// </para>
  /// <para>
  /// It can not be less than <see cref="MinimalExpiredItemsPurgeTaskInterval"/>. By default, it equals
  /// <see cref="DefaultExpiredEntriesPurgeTaskInterval"/>.
  /// </para>
  /// </remarks>
  public TimeSpan ExpiredEntriesPurgeTaskInterval { get; set; } = DefaultExpiredEntriesPurgeTaskInterval;

  private static int Mebibytes(int mebibytes) => mebibytes * 1024 * 1024;
}
