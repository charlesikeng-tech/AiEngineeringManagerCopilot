using AiEngineeringManagerCopilot.Application.AI;

namespace AiEngineeringManagerCopilot.Api.Endpoints;

public static class AIAnalysisEndpoints
{
    public static void MapAIAnalysisEndpoints(
        this IEndpointRouteBuilder app)
    {
        app.MapPost(
            "/teams/{teamId:guid}/reports/{reportId:guid}/analyze",
            async (
                Guid teamId,
                Guid reportId,
                IAIAnalysisService service,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var result = await service.AnalyzeAsync(
                        teamId,
                        reportId,
                        cancellationToken);

                    return Results.Ok(result);
                }
                catch (KeyNotFoundException)
                {
                    return Results.NotFound();
                }
            });
    }
}