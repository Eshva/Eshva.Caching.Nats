using FluentAssertions;
using Reqnroll;

namespace Eshva.Caching.Nats.Tests.InProcess.Common;

[Binding]
public class ErrorHandlingSteps {
  public ErrorHandlingSteps(ErrorHandlingContext errorHandlingContext) {
    _errorHandlingContext = errorHandlingContext;
  }

  [Then("no errors are reported")]
  public void ThenNoErrorsAreReported() => _errorHandlingContext.LastException.Should().BeNull();

  [Then("argument not specified exception should be reported")]
  public void ThenArgumentNotSpecifiedExceptionShouldBeReported() =>
    _errorHandlingContext.LastException.Should().NotBeNull().And.BeOfType<ArgumentNullException>();

  private readonly ErrorHandlingContext _errorHandlingContext;
}
