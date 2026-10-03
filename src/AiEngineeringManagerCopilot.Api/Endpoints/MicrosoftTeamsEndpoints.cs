using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Application.Common;
using AiEngineeringManagerCopilot.Application.MicrosoftTeams;
using FluentValidation;

namespace AiEngineeringManagerCopilot.Api.Endpoints;

public static class MicrosoftTeamsEndpoints
{
    public static IEndpointRouteBuilder MapMicrosoftTeamsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/teams/{teamId:guid}/microsoft-teams")
            .WithTags("Microsoft Teams")
            .RequireAuthorization();

        group.MapPost("", async (
            Guid teamId,
            CreateMicrosoftTeamsWebhookRequest request,
            IMicrosoftTeamsWebhookConnectionService service,
            IValidator<CreateMicrosoftTeamsWebhookRequest> validator,
            CancellationToken cancellationToken) =>
        {
            await RequestValidator.ValidateAndThrowAsync(request, validator, cancellationToken);
            try
            {
                var result = await service.CreateAsync(teamId, request, cancellationToken);
                return result is null
                    ? Results.NotFound()
                    : Results.Created($"/teams/{teamId}/microsoft-teams", result);
            }
            catch (ConflictException exception)
            {
                return Results.Conflict(new { message = exception.Message });
            }
        });

        group.MapGet("", async (
            Guid teamId,
            IMicrosoftTeamsWebhookConnectionService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.GetAsync(teamId, cancellationToken);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        group.MapDelete("", async (
            Guid teamId,
            IMicrosoftTeamsWebhookConnectionService service,
            CancellationToken cancellationToken) =>
        {
            var deleted = await service.DeleteAsync(teamId, cancellationToken);
            return deleted ? Results.NoContent() : Results.NotFound();
        });

        group.MapPost("/test", async (
            Guid teamId,
            IMicrosoftTeamsWebhookConnectionService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.TestAsync(teamId, cancellationToken);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        return app;
    }
}
