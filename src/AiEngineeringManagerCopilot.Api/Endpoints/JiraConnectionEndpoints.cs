using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Application.Common;
using AiEngineeringManagerCopilot.Application.Jira;

namespace AiEngineeringManagerCopilot.Api.Endpoints;

public static class JiraConnectionEndpoints
{
    public static IEndpointRouteBuilder MapJiraConnectionEndpoints(
        this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(
                "/teams/{teamId:guid}/jira")
            .WithTags("Jira");

        group.MapPost(
            "",
            async (
                Guid teamId,
                CreateJiraConnectionRequest request,
                IJiraConnectionService service,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var result = await service.CreateAsync(
                        teamId,
                        request,
                        cancellationToken);

                    return result is null
                        ? Results.NotFound()
                        : Results.Created(
                            $"/teams/{teamId}/jira",
                            result);
                }
                catch (ConflictException exception)
                {
                    return Results.Conflict(new
                    {
                        message = exception.Message
                    });
                }
            });

        group.MapGet(
            "",
            async (
                Guid teamId,
                IJiraConnectionService service,
                CancellationToken cancellationToken) =>
            {
                var result = await service.GetAsync(
                    teamId,
                    cancellationToken);

                return result is null
                    ? Results.NotFound()
                    : Results.Ok(result);
            });

        group.MapDelete(
            "",
            async (
                Guid teamId,
                IJiraConnectionService service,
                CancellationToken cancellationToken) =>
            {
                var deleted = await service.DeleteAsync(
                    teamId,
                    cancellationToken);

                return deleted
                    ? Results.NoContent()
                    : Results.NotFound();
            });
        
        group.MapPost(
            "/test",
            async (
                Guid teamId,
                IJiraConnectionService service,
                CancellationToken cancellationToken) =>
            {
                var result = await service.TestAsync(
                    teamId,
                    cancellationToken);

                return result is null
                    ? Results.NotFound()
                    : Results.Ok(result);
            });
        
        group.MapPost(
            "/sync",
            async (
                Guid teamId,
                ICurrentUser currentUser,
                ITeamRepository teamRepository,
                IJiraSyncService jiraSyncService,
                CancellationToken cancellationToken) =>
            {
                var team = await teamRepository.GetByIdAsync(
                    teamId,
                    currentUser.UserId,
                    cancellationToken);

                if (team is null)
                {
                    return Results.NotFound();
                }

                var result = await jiraSyncService.SyncAsync(
                    teamId,
                    cancellationToken);

                return Results.Ok(result);
            });

        return app;
    }
    
}