using FluentAssertions;
using Reqnroll;

namespace Eshva.Caching.Nats.Tests.OutOfProcess.Common;

[Binding]
public class ErrorHandlingSteps {
  public ErrorHandlingSteps(ErrorHandlingContext errorHandlingContext) {
    _errorHandlingContext = errorHandlingContext;
  }

  [Then("no errors are reported")]
  public void ThenNoErrorsAreReported() => _errorHandlingContext.LastException.Should().BeNull();

  [Then("invalid operation exception should be reported")]
  public void ThenInvalidOperationExceptionShouldBeReported() =>
    _errorHandlingContext.LastException.Should().NotBeNull().And.BeOfType<InvalidOperationException>();

  [Then("argument out of range exception should be reported")]
  public void ThenArgumentOutOfRangeExceptionShouldBeReported() =>
    _errorHandlingContext.LastException.Should().NotBeNull().And.BeOfType<ArgumentOutOfRangeException>();

  private readonly ErrorHandlingContext _errorHandlingContext;
}
