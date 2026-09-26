using AiEngineeringManagerCopilot.Application.Dashboard;

namespace AiEngineeringManagerCopilot.Api.Endpoints;

public static class EngineeringDashboardEndpoints
{
    public static void MapEngineeringDashboardEndpoints(
        this IEndpointRouteBuilder app)
    {
        app.MapGet(
                "/teams/{teamId:guid}/dashboard",
                async (
                    Guid teamId,
                    IEngineeringDashboardService service,
                    CancellationToken cancellationToken) =>
                {
                    var result = await service.GetAsync(
                        teamId,
                        cancellationToken);

                    return result is null
                        ? Results.NotFound()
                        : Results.Ok(result);
                })
            .WithTags("Engineering Dashboard")
            .RequireAuthorization();
    }
}