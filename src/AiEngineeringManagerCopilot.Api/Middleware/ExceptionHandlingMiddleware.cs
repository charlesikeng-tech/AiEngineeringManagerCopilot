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

    private static async Task HandleValidationExceptionAsync(
        HttpContext context,
        ValidationException exception)
    {
        context.Response.StatusCode =
            StatusCodes.Status400BadRequest;

        await context.Response.WriteAsJsonAsync(
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

    private static async Task HandleConflictExceptionAsync(
        HttpContext context,
        ConflictException exception)
    {
        context.Response.StatusCode =
            StatusCodes.Status409Conflict;

        await context.Response.WriteAsJsonAsync(
            new
            {
                type = "https://api.ai-engineering-manager/errors/conflict",
                title = "Conflict",
                status = 409,
                detail = exception.Message,
                traceId = context.TraceIdentifier
            });
    }

    private static async Task HandleUnexpectedExceptionAsync(
        HttpContext context)
    {
        context.Response.StatusCode =
            StatusCodes.Status500InternalServerError;

        await context.Response.WriteAsJsonAsync(
            new
            {
                type = "https://api.ai-engineering-manager/errors/internal",
                title = "Internal server error",
                status = 500,
                detail = "An unexpected error occurred.",
                traceId = context.TraceIdentifier
            });
    }
}