using AiEngineeringManagerCopilot.Application.GitHub;
using AiEngineeringManagerCopilot.Application.Common;
using FluentValidation;

namespace AiEngineeringManagerCopilot.Api.Endpoints;

public static class GitHubEndpoints
{
    public static IEndpointRouteBuilder MapGitHubEndpoints(
        this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(
            "/teams/{teamId:guid}/github").WithTags("GitHub");;

        group.MapPost(
            "/",
            async (
                Guid teamId,
                CreateGitHubConnectionRequest request,
                IGitHubConnectionService service,
                IValidator<CreateGitHubConnectionRequest> validator,
                CancellationToken cancellationToken) =>
            {
                await RequestValidator.ValidateAndThrowAsync(
                    request,
                    validator,
                    cancellationToken);

                var result = await service.CreateAsync(
                    teamId,
                    request,
                    cancellationToken);

                return result is null
                    ? Results.NotFound()
                    : Results.Created(
                        $"/teams/{teamId}/github",
                        result);
            });
        
        group.MapGet(
            "/",
            async (
                Guid teamId,
                IGitHubConnectionService service,
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
            "/",
            async (
                Guid teamId,
                IGitHubConnectionService service,
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
                IGitHubConnectionService service,
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
                IGitHubSyncService service,
                CancellationToken cancellationToken) =>
            {
                var result = await service.SyncAsync(
                    teamId,
                    cancellationToken);

                if (result is null)
                {
                    return Results.NotFound();
                }

                return Results.Ok(result);
            });
        
        return app;
    }
}