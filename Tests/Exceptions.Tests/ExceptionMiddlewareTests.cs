using System.Globalization;
using System.Text.Json;

using FluentAssertions;

using HEAppE.Exceptions.Internal;
using HEAppE.Exceptions.Resources;
using HEAppE.RestApi;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

using Moq;

namespace Exceptions.Tests;

public class ExceptionMiddlewareTests
{
    private readonly Mock<RequestDelegate> _nextMock;
    private readonly Mock<ILogger<ExceptionMiddleware>> _loggerMock;
    private readonly Mock<IStringLocalizer<ExceptionsMessages>> _localizerMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly ExceptionMiddleware _middleware;

    public ExceptionMiddlewareTests()
    {
        _nextMock = new Mock<RequestDelegate>();
        _loggerMock = new Mock<ILogger<ExceptionMiddleware>>();
        _localizerMock = new Mock<IStringLocalizer<ExceptionsMessages>>();
        _configurationMock = new Mock<IConfiguration>();

        _localizerMock.Setup(l => l["SlurmException_UnableToParseNodeUsage"])
            .Returns(new LocalizedString("SlurmException_UnableToParseNodeUsage", "Nelze analyzovat využití cluster node z plánovače HPC."));

        _localizerMock.Setup(l => l["PbsException_UnableToParseResponse"])
            .Returns(new LocalizedString("PbsException_UnableToParseResponse", "Nelze analyzovat odpověď od plánovače PBS Professional."));

        _configurationMock.Setup(c => c["ClusterManagementLogMessageRedactingRegex"])
            .Returns(@"(/[^ ]+)|(\b[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\b)");

        _middleware = new ExceptionMiddleware(_nextMock.Object, _loggerMock.Object, _localizerMock.Object, _configurationMock.Object);
    }

    [Theory]
    [InlineData("Sensitive information /path/to/file 123e4567-e89b-12d3-a456-426614174000", "Sensitive information *REDACTED* *REDACTED*")]
    [InlineData("Non sensitive information", "Non sensitive information")]
    public async Task HandleException_Should_Redact_Sensitive_Information_In_SlurmException(string commandError, string expectedResultMessage)
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var exception = new SlurmException("Slurm error") { CommandError = commandError };

        // Act
        await InvokeHandleException(context, exception);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status502BadGateway);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var response = await new StreamReader(context.Response.Body).ReadToEndAsync();
        ProblemDetails? problemDetails = JsonSerializer.Deserialize<ProblemDetails>(response);

        problemDetails.Should().NotBeNull();
        problemDetails.Title.Should().Be("Slurm Problem");
        problemDetails.Detail.Should().Be(expectedResultMessage);
    }

    [Theory]
    [InlineData("UnableToParseNodeUsage", "Nelze analyzovat využití cluster node z plánovače HPC.")]
    public async Task HandleException_Should_Localize_Message_When_CommandError_Is_Null_In_SlurmException(string exceptionMessage, string localizedMessage)
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var exception = new SlurmException(exceptionMessage) { CommandError = null };

        // Act
        await InvokeHandleException(context, exception);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status502BadGateway);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var response = await new StreamReader(context.Response.Body).ReadToEndAsync();
        ProblemDetails? problemDetails = JsonSerializer.Deserialize<ProblemDetails>(response);

        problemDetails.Should().NotBeNull();
        problemDetails.Title.Should().Be("Slurm Problem");
        problemDetails.Detail.Should().Be(localizedMessage);
    }



    [Theory]
    [InlineData("Sensitive information /path/to/file 123e4567-e89b-12d3-a456-426614174000", "Sensitive information *REDACTED* *REDACTED*")]
    [InlineData("Non sensitive information", "Non sensitive information")]
    public async Task HandleException_Should_Redact_Sensitive_Information_In_PbsException(string commandError, string expectedResultMessage)
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var exception = new PbsException("Pbs error") { CommandError = commandError };

        // Act
        await InvokeHandleException(context, exception);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status502BadGateway);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var response = await new StreamReader(context.Response.Body).ReadToEndAsync();
        ProblemDetails? problemDetails = JsonSerializer.Deserialize<ProblemDetails>(response);

        problemDetails.Should().NotBeNull();
        problemDetails.Title.Should().Be("Pbs Problem");
        problemDetails.Detail.Should().Be(expectedResultMessage);
    }


    [Theory]
    [InlineData("UnableToParseResponse", "Nelze analyzovat odpověď od plánovače PBS Professional.")]
    public async Task HandleException_Should_Localize_Message_When_CommandError_Is_Null_In_PbsException(string exceptionMessage, string localizedMessage)
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var exception = new PbsException(exceptionMessage) { CommandError = null };

        // Act
        await InvokeHandleException(context, exception);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status502BadGateway);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var response = await new StreamReader(context.Response.Body).ReadToEndAsync();
        ProblemDetails? problemDetails = JsonSerializer.Deserialize<ProblemDetails>(response);

        problemDetails.Should().NotBeNull();
        problemDetails.Title.Should().Be("Pbs Problem");
        problemDetails.Detail.Should().Be(localizedMessage);
    }

    private async Task InvokeHandleException(HttpContext context, Exception exception)
    {
        CultureInfo.CurrentCulture = new CultureInfo("cs-CZ");
        System.Reflection.MethodInfo? method = typeof(ExceptionMiddleware).GetMethod("HandleException", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        await (Task)method.Invoke(_middleware, new object[] { context, exception });
    }
}
