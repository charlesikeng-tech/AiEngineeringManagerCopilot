using AiEngineeringManagerCopilot.Application.Risks;

namespace AiEngineeringManagerCopilot.Api.Endpoints;

public static class EngineeringRiskEndpoints
{
    public static IEndpointRouteBuilder MapEngineeringRiskEndpoints(
        this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(
                "/teams/{teamId:guid}/risks")
            .WithTags("Engineering Risks")
            .RequireAuthorization();

        group.MapGet(
            "",
            async (
                Guid teamId,
                IEngineeringRiskService service,
                CancellationToken cancellationToken) =>
            {
                var result = await service.GetCurrentAsync(
                    teamId,
                    cancellationToken);

                return result is null
                    ? Results.NotFound()
                    : Results.Ok(result);
            });

        return app;
    }
}