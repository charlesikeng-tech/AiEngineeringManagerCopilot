using AiEngineeringManagerCopilot.Application.Health;
using AiEngineeringManagerCopilot.Application.Metrics;

namespace AiEngineeringManagerCopilot.Api.Endpoints;

public static class MetricsEndpoints
{
    public static IEndpointRouteBuilder MapMetricsEndpoints(
        this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(
            "/teams/{teamId:guid}/metrics").WithTags("Metrics");

        group.MapPost(
            "/cycle-time",
            async (
                Guid teamId,
                DateOnly periodStart,
                DateOnly periodEnd,
                IEngineeringMetricsService service,
                CancellationToken cancellationToken) =>
            {
                if (periodStart > periodEnd)
                {
                    return Results.BadRequest(new
                    {
                        message =
                            "periodStart must be before or equal to periodEnd."
                    });
                }

                var result =
                    await service.CalculateCycleTimeAsync(
                        teamId,
                        periodStart,
                        periodEnd,
                        cancellationToken);

                if (result is null)
                {
                    return Results.NotFound();
                }

                return Results.Ok(result);
            });
        
        group.MapPost(
            "/pr-review-time",
            async (
                Guid teamId,
                DateOnly periodStart,
                DateOnly periodEnd,
                IEngineeringMetricsService service,
                CancellationToken cancellationToken) =>
            {
                if (periodStart > periodEnd)
                {
                    return Results.BadRequest(new
                    {
                        message =
                            "periodStart must be before or equal to periodEnd."
                    });
                }

                var result =
                    await service.CalculatePRReviewTimeAsync(
                        teamId,
                        periodStart,
                        periodEnd,
                        cancellationToken);

                if (result is null)
                {
                    return Results.NotFound();
                }

                return Results.Ok(result);
            });
        
        group.MapPost(
            "/deployment-frequency",
            async (
                Guid teamId,
                DateOnly periodStart,
                DateOnly periodEnd,
                IEngineeringMetricsService service,
                CancellationToken cancellationToken) =>
            {
                if (periodStart > periodEnd)
                {
                    return Results.BadRequest(new
                    {
                        message =
                            "periodStart must be before or equal to periodEnd."
                    });
                }

                var result =
                    await service.CalculateDeploymentFrequencyAsync(
                        teamId,
                        periodStart,
                        periodEnd,
                        cancellationToken);

                if (result is null)
                {
                    return Results.NotFound();
                }

                return Results.Ok(result);
            });
        
        group.MapPost(
            "/change-failure-rate",
            async (
                Guid teamId,
                DateOnly periodStart,
                DateOnly periodEnd,
                IEngineeringMetricsService service,
                CancellationToken cancellationToken) =>
            {
                if (periodStart > periodEnd)
                {
                    return Results.BadRequest(new
                    {
                        message =
                            "periodStart must be before or equal to periodEnd."
                    });
                }

                var result =
                    await service.CalculateChangeFailureRateAsync(
                        teamId,
                        periodStart,
                        periodEnd,
                        cancellationToken);

                if (result is null)
                {
                    return Results.NotFound();
                }

                return Results.Ok(result);
            });
        
        group.MapPost(
            "/lead-time",
            async (
                Guid teamId,
                DateOnly periodStart,
                DateOnly periodEnd,
                IEngineeringMetricsService service,
                CancellationToken cancellationToken) =>
            {
                var result = await service.CalculateLeadTimeAsync(
                    teamId,
                    periodStart,
                    periodEnd,
                    cancellationToken);

                return result is null
                    ? Results.NotFound()
                    : Results.Ok(result);
            });
        
        group.MapPost(
            "/open-prs",
            async (
                Guid teamId,
                DateOnly periodStart,
                DateOnly periodEnd,
                IEngineeringMetricsService service,
                CancellationToken cancellationToken) =>
            {
                var result = await service.CalculateOpenPullRequestsAsync(
                    teamId,
                    periodStart,
                    periodEnd,
                    cancellationToken);

                return result is null
                    ? Results.NotFound()
                    : Results.Ok(result);
            });
        
        group.MapPost(
            "/merged-prs",
            async (
                Guid teamId,
                DateOnly periodStart,
                DateOnly periodEnd,
                IEngineeringMetricsService service,
                CancellationToken cancellationToken) =>
            {
                var result = await service.CalculateMergedPullRequestsAsync(
                    teamId,
                    periodStart,
                    periodEnd,
                    cancellationToken);

                return result is null
                    ? Results.NotFound()
                    : Results.Ok(result);
            });
        
        group.MapPost(
            "/blocked-items",
            async (
                Guid teamId,
                DateOnly periodStart,
                DateOnly periodEnd,
                IEngineeringMetricsService service,
                CancellationToken cancellationToken) =>
            {
                var result = await service.CalculateBlockedItemsAsync(
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