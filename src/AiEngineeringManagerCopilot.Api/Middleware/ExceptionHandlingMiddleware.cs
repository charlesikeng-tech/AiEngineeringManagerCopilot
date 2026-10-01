using System.Text.Json;
using AiEngineeringManagerCopilot.Application.Common;

namespace AiEngineeringManagerCopilot.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException exception)
        {
            await HandleValidationExceptionAsync(
                context,
                exception);
        }
        catch (ConflictException exception)
        {
            await HandleConflictExceptionAsync(
                context,
                exception);
        }
        catch (OperationCanceledException)
            when (context.RequestAborted.IsCancellationRequested)
        {
            logger.LogDebug(
                "Request cancelled by client for {Method} {Path}",
                context.Request.Method,
                context.Request.Path);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Unhandled exception while processing {Method} {Path}",
                context.Request.Method,
                context.Request.Path);

            await HandleUnexpectedExceptionAsync(context);
        }
    }

    private static Task HandleValidationExceptionAsync(
        HttpContext context,
        ValidationException exception)
    {
        return WriteProblemAsync(
            context,
            StatusCodes.Status400BadRequest,
            new
            {
                type = "https://api.ai-engineering-manager/errors/validation",
                title = "Validation error",
                status = 400,
                detail = exception.Message,
                errors = exception.Errors,
                traceId = context.TraceIdentifier
            });
    }

    private static Task HandleConflictExceptionAsync(
        HttpContext context,
        ConflictException exception)
    {
        return WriteProblemAsync(
            context,
            StatusCodes.Status409Conflict,
            new
            {
                type = "https://api.ai-engineering-manager/errors/conflict",
                title = "Conflict",
                status = 409,
                detail = exception.Message,
                traceId = context.TraceIdentifier
            });
    }

    private static Task HandleUnexpectedExceptionAsync(
        HttpContext context)
    {
        return WriteProblemAsync(
            context,
            StatusCodes.Status500InternalServerError,
            new
            {
                type = "https://api.ai-engineering-manager/errors/internal",
                title = "Internal server error",
                status = 500,
                detail = "An unexpected error occurred.",
                traceId = context.TraceIdentifier
            });
    }
    
    private static async Task WriteProblemAsync<T>(
        HttpContext context,
        int statusCode,
        T problem)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        await JsonSerializer.SerializeAsync(
            context.Response.Body,
            problem);
    }
}