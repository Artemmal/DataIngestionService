using DataIngestionService.Api.Exceptions;
using DataIngestionService.Api.Middleware;
using DataIngestionService.Api.Models.Responses;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;

namespace DataIngestionService.Tests.Middleware;

public sealed class ExceptionHandlingMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ShouldReturnBadRequest_WhenValidationExceptionIsThrown()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var validationException = new ValidationException(
            new[]
            {
                new ValidationFailure("Amount", "Amount must be greater than zero.")
            });

        var middleware = new ExceptionHandlingMiddleware(
            _ => throw validationException,
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);

        var response = await ReadErrorResponseAsync(context);

        response.Status.Should().Be(StatusCodes.Status400BadRequest);
        response.Title.Should().Be("Validation failed");
        response.Detail.Should().Be("One or more validation errors occurred.");

        response.Errors.Should().NotBeNull();
        response.Errors.Should().ContainSingle(error =>
            error.Field == "Amount" &&
            error.Message == "Amount must be greater than zero.");
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturnConflict_WhenDuplicateTransactionExceptionIsThrown()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new DuplicateTransactionException(),
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status409Conflict);

        var response = await ReadErrorResponseAsync(context);

        response.Status.Should().Be(StatusCodes.Status409Conflict);
        response.Title.Should().Be("Duplicate transaction");
        response.Detail.Should().Be(
            "A transaction with the same customer, date, amount, currency and source channel already exists.");
        response.Errors.Should().BeNull();
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturnInternalServerError_WhenUnexpectedExceptionIsThrown()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new InvalidOperationException("Unexpected failure."),
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);

        var responseBody = await ReadResponseBodyAsync(context);
        var response = DeserializeErrorResponse(responseBody);

        response.Status.Should().Be(StatusCodes.Status500InternalServerError);
        response.Title.Should().Be("Internal server error");
        response.Detail.Should().Be("An unexpected error occurred while processing the request.");
        response.Errors.Should().BeNull();

        responseBody.Should().NotContain("Unexpected failure.");
    }

    private static async Task<ErrorResponse> ReadErrorResponseAsync(HttpContext context)
    {
        var responseBody = await ReadResponseBodyAsync(context);

        return DeserializeErrorResponse(responseBody);
    }

    private static async Task<string> ReadResponseBodyAsync(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);

        using var reader = new StreamReader(context.Response.Body, leaveOpen: true);

        return await reader.ReadToEndAsync();
    }

    private static ErrorResponse DeserializeErrorResponse(string responseBody)
    {
        var response = JsonSerializer.Deserialize<ErrorResponse>(
            responseBody,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        response.Should().NotBeNull();

        return response!;
    }
}