using AppSupportHub.Application.Abstractions.Results;

namespace AppSupportHub.UnitTests.Application.Abstractions;

public sealed class ApplicationResultTests
{
    [Fact]
    public void SuccessContainsValueWithoutError()
    {
        ApplicationResult<string> result = ApplicationResultFactory.Success("value");

        Assert.True(result.IsSuccess);
        Assert.Equal("value", result.Value);
        Assert.Null(result.Error);
    }

    [Fact]
    public void FailureContainsOneErrorAndRejectsValueAccess()
    {
        var error = new ApplicationError(
            "test.failure",
            "Expected failure",
            ApplicationErrorType.BusinessRule);
        ApplicationResult<string> result = ApplicationResultFactory.Failure<string>(error);

        Assert.False(result.IsSuccess);
        Assert.Same(error, result.Error);
        Assert.Throws<InvalidOperationException>(() => _ = result.Value);
    }

    [Theory]
    [InlineData("", "Description")]
    [InlineData("   ", "Description")]
    [InlineData("code", "")]
    [InlineData("code", "   ")]
    public void ApplicationErrorRejectsEmptyCodeOrDescription(string code, string description)
    {
        Assert.Throws<ArgumentException>(() => new ApplicationError(
            code,
            description,
            ApplicationErrorType.Validation));
    }

    [Fact]
    public void ApplicationErrorTrimsCodeAndDescription()
    {
        var error = new ApplicationError(
            " test.code ",
            " Description ",
            ApplicationErrorType.Validation);

        Assert.Equal("test.code", error.Code);
        Assert.Equal("Description", error.Description);
    }
}
