using AiEngineeringManagerCopilot.Application.Actions;

namespace AiEngineeringManagerCopilot.Api.Endpoints;

public static class EngineeringActionEndpoints
{
    public static IEndpointRouteBuilder MapEngineeringActionEndpoints(
        this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(
                "/teams/{teamId:guid}/actions")
            .WithTags("Engineering Actions")
            .RequireAuthorization();

        group.MapGet(
            "",
            async (
                Guid teamId,
                IEngineeringActionService service,
                CancellationToken cancellationToken) =>
            {
                var result = await service.GetCurrentAsync(
                    teamId,
                    cancellationToken);

                return result is null
                    ? Results.NotFound()
                    : Results.Ok(result);
            });
        
        group.MapPatch(
            "/{actionId:guid}",
            async (
                Guid teamId,
                Guid actionId,
                UpdateEngineeringActionRequest request,
                IEngineeringActionService service,
                CancellationToken cancellationToken) =>
            {
                var result = await service.UpdateAsync(
                    teamId,
                    actionId,
                    request,
                    cancellationToken);

                return result is null
                    ? Results.NotFound()
                    : Results.Ok(result);
            });

        return app;
    }
}