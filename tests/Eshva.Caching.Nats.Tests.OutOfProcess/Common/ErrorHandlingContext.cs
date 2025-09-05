using System;

namespace Eshva.Caching.Nats.Tests.OutOfProcess.Common;

public class ErrorHandlingContext {
  public Exception? LastException { get; set; }
}
