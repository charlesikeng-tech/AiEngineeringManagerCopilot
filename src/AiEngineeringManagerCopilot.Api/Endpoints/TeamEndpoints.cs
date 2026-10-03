using AiEngineeringManagerCopilot.Application.Teams;
using AiEngineeringManagerCopilot.Application.Common;
using FluentValidation;

namespace AiEngineeringManagerCopilot.Api.Endpoints;

public static class TeamEndpoints
{
    public static IEndpointRouteBuilder MapTeamEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/teams")
            .WithTags("Teams")
            .RequireAuthorization();

        group.MapPost(
                "/",
                async (
                    CreateTeamRequest request,
                    ITeamService teamService,
                    CancellationToken cancellationToken) =>
                {
                    var team = await teamService.CreateAsync(
                        request,
                        cancellationToken);

                    return Results.Created(
                        $"/teams/{team.Id}",
                        team);
                })
            .WithName("CreateTeam")
            .WithSummary("Create a team")
            .WithDescription("Creates a new engineering team for the current user.");

        group.MapGet(
                "/",
                async (
                    ITeamService teamService,
                    CancellationToken cancellationToken) =>
                {
                    var teams = await teamService.GetAllAsync(
                        cancellationToken);

                    return Results.Ok(teams);
                })
            .WithName("GetTeams")
            .WithSummary("Get current user's teams")
            .WithDescription("Returns all teams owned by the current user.");

        group.MapGet(
                "/paged",
                async (
                    int? pageNumber,
                    int? pageSize,
                    string? search,
                    ITeamService teamService,
                    IValidator<GetTeamsPageRequest> validator,
                    CancellationToken cancellationToken) =>
                {
                    var request = new GetTeamsPageRequest(pageNumber ?? 1, pageSize ?? 10, search);
                    await RequestValidator.ValidateAndThrowAsync(request, validator, cancellationToken);
                    var result = await teamService.GetPageAsync(request, cancellationToken);
                    return Results.Ok(result);
                })
            .WithName("GetTeamsPage")
            .WithSummary("Get a page of current user's teams")
            .WithDescription("Returns teams ordered by name and ID, with an optional name search.");

        group.MapGet(
                "/{teamId:guid}",
                async (
                    Guid teamId,
                    ITeamService teamService,
                    CancellationToken cancellationToken) =>
                {
                    var team = await teamService.GetByIdAsync(
                        teamId,
                        cancellationToken);

                    return team is null
                        ? Results.NotFound()
                        : Results.Ok(team);
                })
            .WithName("GetTeam")
            .WithSummary("Get a team")
            .WithDescription("Returns a team owned by the current user.");

        group.MapPut(
                "/{teamId:guid}",
                async (
                    Guid teamId,
                    UpdateTeamRequest request,
                    ITeamService teamService,
                    CancellationToken cancellationToken) =>
                {
                    var team = await teamService.UpdateAsync(
                        teamId,
                        request,
                        cancellationToken);

                    return team is null
                        ? Results.NotFound()
                        : Results.Ok(team);
                })
            .WithName("UpdateTeam")
            .WithSummary("Update a team")
            .WithDescription("Updates a team owned by the current user.");

        group.MapDelete(
                "/{teamId:guid}",
                async (
                    Guid teamId,
                    ITeamService teamService,
                    CancellationToken cancellationToken) =>
                {
                    var deleted = await teamService.DeleteAsync(
                        teamId,
                        cancellationToken);

                    return deleted
                        ? Results.NoContent()
                        : Results.NotFound();
                })
            .WithName("DeleteTeam")
            .WithSummary("Delete a team")
            .WithDescription("Deletes a team owned by the current user.");

        return endpoints;
    }
}