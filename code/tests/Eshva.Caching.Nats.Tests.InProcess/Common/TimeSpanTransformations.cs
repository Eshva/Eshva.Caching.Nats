using Reqnroll;

namespace Eshva.Caching.Nats.Tests.InProcess.Common;

[Binding]
internal class TimeSpanTransformations {
  [StepArgumentTransformation]
  public TimeSpan? TimeSpanNullableTransformation(string value) => !value.Equals("null") ? TimeSpan.Parse(value) : null;
}
