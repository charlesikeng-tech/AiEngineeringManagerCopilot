using AiEngineeringManagerCopilot.Application.Health;

namespace AiEngineeringManagerCopilot.Api.Endpoints;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(
        this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(
                "/teams/{teamId:guid}/health")
            .WithTags("Health");

        group.MapGet(
            "/score",
            async (
                Guid teamId,
                DateOnly periodStart,
                DateOnly periodEnd,
                IEngineeringHealthScoreService service,
                CancellationToken cancellationToken) =>
            {
                var result = await service.CalculateAsync(
                    teamId,
                    periodStart,
                    periodEnd,
                    cancellationToken);

                return result is null
                    ? Results.NotFound()
                    : Results.Ok(result);
            });

        return app;
    }
}