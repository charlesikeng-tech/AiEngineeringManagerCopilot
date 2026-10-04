using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Application.Common;
using FluentValidation;

namespace AiEngineeringManagerCopilot.Api.Endpoints;

public static class TeamCollectionPageEndpoints
{
    public static IEndpointRouteBuilder MapTeamCollectionPageEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/teams/{teamId:guid}").RequireAuthorization();
        MapPage(group, "/reports/paged", "Engineering Reports",
            (reader, owner, team, request, ct) => reader.GetReportsAsync(owner, team, request, ct));
        MapPage(group, "/members/paged", "Team Members",
            (reader, owner, team, request, ct) => reader.GetMembersAsync(owner, team, request, ct));
        MapPage(group, "/risks/paged", "Engineering Risks",
            (reader, owner, team, request, ct) => reader.GetRisksAsync(owner, team, request, ct));
        MapPage(group, "/actions/paged", "Engineering Actions",
            (reader, owner, team, request, ct) => reader.GetActionsAsync(owner, team, request, ct));
        return endpoints;
    }

    private static void MapPage<T>(
        RouteGroupBuilder group, string route, string tag,
        Func<ITeamCollectionPageReader, Guid, Guid, PageRequest, CancellationToken, Task<T?>> read)
        where T : class
    {
        group.MapGet(route, async (
            Guid teamId, int? pageNumber, int? pageSize,
            ICurrentUser currentUser, ITeamCollectionPageReader reader,
            IValidator<PageRequest> validator, CancellationToken cancellationToken) =>
        {
            var request = new PageRequest(pageNumber ?? 1, pageSize ?? 10);
            await RequestValidator.ValidateAndThrowAsync(request, validator, cancellationToken);
            var result = await read(reader, currentUser.UserId, teamId, request, cancellationToken);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).WithTags(tag);
    }
}
