using JetBrains.Annotations;

namespace Eshva.Caching.Nats.Tests.InProcess.Common;

[UsedImplicitly]
public class ErrorHandlingContext {
  public Exception? LastException { get; set; }
}
