using System.Globalization;
using Reqnroll;

namespace Eshva.Caching.Nats.Tests.InProcess.Common;

[Binding]
internal class DateTimeOffsetTransformations {
  [StepArgumentTransformation]
  public DateTimeOffset DateTimeOffsetTransformation(string value) =>
    value.Equals("never expires", StringComparison.InvariantCultureIgnoreCase) ? DateTimeOffset.MaxValue : ParseDateTimeOffset(value);

  [StepArgumentTransformation]
  public DateTimeOffset? DateTimeOffsetNullableTransformation(string value) =>
    value.Equals("null", StringComparison.InvariantCultureIgnoreCase) ? null : ParseDateTimeOffset(value);

  private static DateTimeOffset ParseDateTimeOffset(string value) =>
    DateTimeOffset.ParseExact(
      value,
      DateTimeFormat,
      formatProvider: null,
      DateTimeStyles.AssumeUniversal);

  private const string DateTimeFormat = "dd.MM.yyyy HH:mm:ss";
}
