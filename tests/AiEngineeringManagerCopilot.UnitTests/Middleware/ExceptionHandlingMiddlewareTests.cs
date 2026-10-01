using System.Text.Json;
using AiEngineeringManagerCopilot.Api.Middleware;
using AiEngineeringManagerCopilot.Application.Common;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace AiEngineeringManagerCopilot.UnitTests.Middleware;

public sealed class ExceptionHandlingMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WhenNoException_ShouldCallNext()
    {
        var nextCalled = false;

        RequestDelegate next = _ =>
        {
            nextCalled = true;

            return Task.CompletedTask;
        };

        var middleware = CreateMiddleware(next);

        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_WhenValidationException_ShouldReturnBadRequest()
    {
        RequestDelegate next = _ =>
            throw new ValidationException(
                new Dictionary<string, string[]>
                {
                    ["Name"] = ["Name is required."]
                });

        var middleware = CreateMiddleware(next);

        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should()
            .Be(StatusCodes.Status400BadRequest);

        context.Response.ContentType.Should()
            .StartWith("application/problem+json");
        
        var json = await ReadResponseAsync(context);

        json.RootElement
            .GetProperty("type")
            .GetString()
            .Should()
            .Be(
                "https://api.ai-engineering-manager/errors/validation");

        json.RootElement
            .GetProperty("title")
            .GetString()
            .Should()
            .Be("Validation error");

        json.RootElement
            .GetProperty("status")
            .GetInt32()
            .Should()
            .Be(StatusCodes.Status400BadRequest);

        json.RootElement
            .GetProperty("detail")
            .GetString()
            .Should()
            .Be("One or more validation errors occurred.");

        json.RootElement
            .GetProperty("traceId")
            .GetString()
            .Should()
            .Be(context.TraceIdentifier);

        json.RootElement
            .GetProperty("errors")
            .GetProperty("Name")[0]
            .GetString()
            .Should()
            .Be("Name is required.");
    }

    [Fact]
    public async Task InvokeAsync_WhenConflictException_ShouldReturnConflict()
    {
        RequestDelegate next = _ =>
            throw new ConflictException(
                "Resource already exists.");

        var middleware = CreateMiddleware(next);

        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should()
            .Be(StatusCodes.Status409Conflict);

        context.Response.ContentType.Should()
            .StartWith("application/problem+json");
        
        var json = await ReadResponseAsync(context);

        json.RootElement
            .GetProperty("type")
            .GetString()
            .Should()
            .Be(
                "https://api.ai-engineering-manager/errors/conflict");

        json.RootElement
            .GetProperty("title")
            .GetString()
            .Should()
            .Be("Conflict");

        json.RootElement
            .GetProperty("status")
            .GetInt32()
            .Should()
            .Be(StatusCodes.Status409Conflict);

        json.RootElement
            .GetProperty("detail")
            .GetString()
            .Should()
            .Be("Resource already exists.");

        json.RootElement
            .GetProperty("traceId")
            .GetString()
            .Should()
            .Be(context.TraceIdentifier);
    }

    [Fact]
    public async Task InvokeAsync_WhenUnexpectedException_ShouldReturnInternalServerError()
    {
        RequestDelegate next = _ =>
            throw new InvalidOperationException(
                "Sensitive internal information.");

        var middleware = CreateMiddleware(next);

        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should()
            .Be(StatusCodes.Status500InternalServerError);

        context.Response.ContentType.Should()
            .StartWith("application/problem+json");
        
        var json = await ReadResponseAsync(context);

        json.RootElement
            .GetProperty("type")
            .GetString()
            .Should()
            .Be(
                "https://api.ai-engineering-manager/errors/internal");

        json.RootElement
            .GetProperty("title")
            .GetString()
            .Should()
            .Be("Internal server error");

        json.RootElement
            .GetProperty("status")
            .GetInt32()
            .Should()
            .Be(StatusCodes.Status500InternalServerError);

        json.RootElement
            .GetProperty("detail")
            .GetString()
            .Should()
            .Be("An unexpected error occurred.");

        json.RootElement
            .GetProperty("detail")
            .GetString()
            .Should()
            .NotContain("Sensitive internal information.");

        json.RootElement
            .GetProperty("traceId")
            .GetString()
            .Should()
            .Be(context.TraceIdentifier);
    }

    [Fact]
    public async Task InvokeAsync_WhenRequestIsCancelled_ShouldNotReturnInternalServerError()
    {
        using var cancellationTokenSource =
            new CancellationTokenSource();

        cancellationTokenSource.Cancel();

        RequestDelegate next = _ =>
            throw new OperationCanceledException(
                cancellationTokenSource.Token);

        var middleware = CreateMiddleware(next);

        var context = CreateHttpContext();

        context.RequestAborted =
            cancellationTokenSource.Token;

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should()
            .NotBe(
                StatusCodes.Status500InternalServerError);

        context.Response.Body.Length.Should().Be(0);
    }

    private static ExceptionHandlingMiddleware CreateMiddleware(
        RequestDelegate next)
    {
        return new ExceptionHandlingMiddleware(
            next,
            NullLogger<ExceptionHandlingMiddleware>.Instance);
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext
        {
            TraceIdentifier = "test-trace-id"
        };

        context.Response.Body =
            new MemoryStream();

        return context;
    }

    private static async Task<JsonDocument> ReadResponseAsync(
        HttpContext context)
    {
        context.Response.Body.Position = 0;

        return await JsonDocument.ParseAsync(
            context.Response.Body);
    }
}