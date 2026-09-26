using AiEngineeringManagerCopilot.Application.TeamMembers;

namespace AiEngineeringManagerCopilot.Api.Endpoints;

public static class TeamMemberEndpoints
{
    public static IEndpointRouteBuilder MapTeamMemberEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/teams/{teamId:guid}/members")
            .WithTags("Team Members")
            .RequireAuthorization();
            

        group.MapPost(
                "/",
                async (
                    Guid teamId,
                    CreateTeamMemberRequest request,
                    ITeamMemberService service,
                    CancellationToken cancellationToken) =>
                {
                    var member = await service.CreateAsync(
                        teamId,
                        request,
                        cancellationToken);

                    return member is null
                        ? Results.NotFound()
                        : Results.Created(
                            $"/teams/{teamId}/members/{member.Id}",
                            member);
                })
            .WithName("CreateTeamMember")
            .WithSummary("Add a member to a team");

        group.MapGet(
                "/",
                async (
                    Guid teamId,
                    ITeamMemberService service,
                    CancellationToken cancellationToken) =>
                {
                    var members = await service.GetAllAsync(
                        teamId,
                        cancellationToken);

                    return members is null
                        ? Results.NotFound()
                        : Results.Ok(members);
                })
            .WithName("GetTeamMembers")
            .WithSummary("Get team members");

        group.MapGet(
                "/{memberId:guid}",
                async (
                    Guid teamId,
                    Guid memberId,
                    ITeamMemberService service,
                    CancellationToken cancellationToken) =>
                {
                    var member = await service.GetByIdAsync(
                        teamId,
                        memberId,
                        cancellationToken);

                    return member is null
                        ? Results.NotFound()
                        : Results.Ok(member);
                })
            .WithName("GetTeamMember")
            .WithSummary("Get a team member");

        group.MapPut(
                "/{memberId:guid}",
                async (
                    Guid teamId,
                    Guid memberId,
                    UpdateTeamMemberRequest request,
                    ITeamMemberService service,
                    CancellationToken cancellationToken) =>
                {
                    var member = await service.UpdateAsync(
                        teamId,
                        memberId,
                        request,
                        cancellationToken);

                    return member is null
                        ? Results.NotFound()
                        : Results.Ok(member);
                })
            .WithName("UpdateTeamMember")
            .WithSummary("Update a team member");

        group.MapDelete(
                "/{memberId:guid}",
                async (
                    Guid teamId,
                    Guid memberId,
                    ITeamMemberService service,
                    CancellationToken cancellationToken) =>
                {
                    var deleted = await service.DeleteAsync(
                        teamId,
                        memberId,
                        cancellationToken);

                    return deleted
                        ? Results.NoContent()
                        : Results.NotFound();
                })
            .WithName("DeleteTeamMember")
            .WithSummary("Delete a team member");

        return endpoints;
    }
}