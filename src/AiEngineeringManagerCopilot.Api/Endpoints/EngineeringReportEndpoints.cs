using AiEngineeringManagerCopilot.Application.Reports;

namespace AiEngineeringManagerCopilot.Api.Endpoints;

public static class EngineeringReportEndpoints
{
    public static IEndpointRouteBuilder MapEngineeringReportEndpoints(
        this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(
                "/teams/{teamId:guid}/reports")
            .WithTags("Engineering Reports");

        group.MapPost(
            "/",
            async (
                Guid teamId,
                DateOnly periodStart,
                DateOnly periodEnd,
                IEngineeringReportService service,
                CancellationToken cancellationToken) =>
            {
                var result = await service.GenerateAsync(
                    teamId,
                    periodStart,
                    periodEnd,
                    cancellationToken);

                return result is null
                    ? Results.NotFound()
                    : Results.Created(
                        $"/teams/{teamId}/reports/{result.Id}",
                        result);
            });

        group.MapGet(
            "/",
            async (
                Guid teamId,
                IEngineeringReportService service,
                CancellationToken cancellationToken) =>
            {
                var reports = await service.GetByTeamAsync(
                    teamId,
                    cancellationToken);

                return Results.Ok(reports);
            });

        group.MapGet(
            "/{reportId:guid}",
            async (
                Guid teamId,
                Guid reportId,
                IEngineeringReportService service,
                CancellationToken cancellationToken) =>
            {
                var result = await service.GetByIdAsync(
                    teamId,
                    reportId,
                    cancellationToken);

                return result is null
                    ? Results.NotFound()
                    : Results.Ok(result);
            });

        return app;
    }
}