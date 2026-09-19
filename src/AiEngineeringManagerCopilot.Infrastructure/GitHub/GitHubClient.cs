using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Application.GitHub;

namespace AiEngineeringManagerCopilot.Infrastructure.GitHub;

public sealed class GitHubClient(
    HttpClient httpClient)
    : IGitHubClient
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    public async Task<GitHubOrganization?> GetOrganizationAsync(
        string organization,
        string accessToken,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"orgs/{Uri.EscapeDataString(organization)}");

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);

        request.Headers.UserAgent.ParseAdd(
            "AiEngineeringManagerCopilot/1.0");

        request.Headers.Accept.Add(
            new MediaTypeWithQualityHeaderValue(
                "application/vnd.github+json"));

        using var response = await httpClient.SendAsync(
            request,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        await using var stream =
            await response.Content.ReadAsStreamAsync(
                cancellationToken);

        var githubOrganization =
            await JsonSerializer.DeserializeAsync<GitHubOrganizationDto>(
                stream,
                JsonOptions,
                cancellationToken);

        if (githubOrganization is null)
        {
            throw new InvalidOperationException(
                "GitHub returned an empty organization response.");
        }

        return new GitHubOrganization(
            githubOrganization.Id,
            githubOrganization.Login,
            githubOrganization.Name ?? string.Empty,
            githubOrganization.HtmlUrl);
    }

    public async Task<IReadOnlyList<GitHubRepository>>
        GetRepositoriesAsync(
            string organization,
            string accessToken,
            CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"orgs/{Uri.EscapeDataString(organization)}/repos?per_page=100");

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);

        request.Headers.UserAgent.ParseAdd(
            "AiEngineeringManagerCopilot/1.0");

        request.Headers.Accept.Add(
            new MediaTypeWithQualityHeaderValue(
                "application/vnd.github+json"));

        using var response = await httpClient.SendAsync(
            request,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        await using var stream =
            await response.Content.ReadAsStreamAsync(
                cancellationToken);

        var repositories =
            await JsonSerializer.DeserializeAsync<
                List<GitHubRepositoryDto>>(
                stream,
                JsonOptions,
                cancellationToken);

        if (repositories is null)
        {
            return [];
        }

        return repositories
            .Select(x => new GitHubRepository(
                x.Id,
                x.Name,
                x.FullName,
                x.HtmlUrl,
                x.DefaultBranch))
            .ToList();
    }

    public async Task<IReadOnlyList<GitHubPullRequest>>
        GetPullRequestsAsync(
            string accessToken,
            string owner,
            string repository,
            CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"repos/{Uri.EscapeDataString(owner)}/" +
            $"{Uri.EscapeDataString(repository)}/pulls" +
            "?state=all&per_page=100");

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);

        request.Headers.UserAgent.ParseAdd(
            "AiEngineeringManagerCopilot/1.0");

        request.Headers.Accept.Add(
            new MediaTypeWithQualityHeaderValue(
                "application/vnd.github+json"));

        using var response = await httpClient.SendAsync(
            request,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        await using var stream =
            await response.Content.ReadAsStreamAsync(
                cancellationToken);

        var pullRequests =
            await JsonSerializer.DeserializeAsync<
                List<GitHubPullRequestDto>>(
                stream,
                JsonOptions,
                cancellationToken);

        if (pullRequests is null)
        {
            return [];
        }

        return pullRequests
            .Select(x => new GitHubPullRequest(
                x.Id,
                x.Number,
                x.Title,
                x.User.Id.ToString(),
                x.State,
                x.CreatedAt,
                x.MergedAt,
                x.ClosedAt))
            .ToList();
    }

    public async Task<IReadOnlyList<GitHubPullRequestReview>>
        GetPullRequestReviewsAsync(
            string accessToken,
            string owner,
            string repository,
            int pullRequestNumber,
            CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"repos/{Uri.EscapeDataString(owner)}/" +
            $"{Uri.EscapeDataString(repository)}/pulls/" +
            $"{pullRequestNumber}/reviews?per_page=100");

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);

        request.Headers.UserAgent.ParseAdd(
            "AiEngineeringManagerCopilot/1.0");

        request.Headers.Accept.Add(
            new MediaTypeWithQualityHeaderValue(
                "application/vnd.github+json"));

        using var response = await httpClient.SendAsync(
            request,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        await using var stream =
            await response.Content.ReadAsStreamAsync(
                cancellationToken);

        var reviews =
            await JsonSerializer.DeserializeAsync<
                List<GitHubPullRequestReviewDto>>(
                stream,
                JsonOptions,
                cancellationToken);

        if (reviews is null)
        {
            return [];
        }

        return reviews
            .Select(x => new GitHubPullRequestReview(
                x.Id,
                x.User.Id.ToString(),
                x.State,
                x.SubmittedAt))
            .ToList();
    }

    public async Task<IReadOnlyList<GitHubDeployment>>
        GetDeploymentsAsync(
            string accessToken,
            string owner,
            string repository,
            CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"repos/{Uri.EscapeDataString(owner)}/" +
            $"{Uri.EscapeDataString(repository)}/deployments?per_page=100");

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);

        request.Headers.UserAgent.ParseAdd(
            "AiEngineeringManagerCopilot/1.0");

        request.Headers.Accept.Add(
            new MediaTypeWithQualityHeaderValue(
                "application/vnd.github+json"));

        using var response = await httpClient.SendAsync(
            request,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        await using var stream =
            await response.Content.ReadAsStreamAsync(
                cancellationToken);

        var deployments =
            await JsonSerializer.DeserializeAsync<
                List<GitHubDeploymentDto>>(
                stream,
                JsonOptions,
                cancellationToken);

        if (deployments is null)
        {
            return [];
        }

        return deployments
            .Select(x => new GitHubDeployment(
                x.Id,
                x.Environment,
                x.CreatedAt))
            .ToList();
    }

    public async Task<IReadOnlyList<GitHubDeploymentStatus>>
        GetDeploymentStatusesAsync(
            string accessToken,
            string owner,
            string repository,
            long deploymentId,
            CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"repos/{Uri.EscapeDataString(owner)}/" +
            $"{Uri.EscapeDataString(repository)}/deployments/" +
            $"{deploymentId}/statuses?per_page=100");

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);

        request.Headers.UserAgent.ParseAdd(
            "AiEngineeringManagerCopilot/1.0");

        request.Headers.Accept.Add(
            new MediaTypeWithQualityHeaderValue(
                "application/vnd.github+json"));

        using var response = await httpClient.SendAsync(
            request,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        await using var stream =
            await response.Content.ReadAsStreamAsync(
                cancellationToken);

        var statuses =
            await JsonSerializer.DeserializeAsync<
                List<GitHubDeploymentStatusDto>>(
                stream,
                JsonOptions,
                cancellationToken);

        if (statuses is null)
        {
            return [];
        }

        return statuses
            .Select(x => new GitHubDeploymentStatus(
                x.Id,
                x.State,
                x.CreatedAt))
            .ToList();
    }

    private sealed record GitHubOrganizationDto(
        long Id,
        string Login,
        string? Name,
        [property: JsonPropertyName("html_url")]
        string HtmlUrl);
    
    private sealed record GitHubRepositoryDto(
        long Id,
        string Name,
        [property: JsonPropertyName("full_name")]
        string FullName,
        [property: JsonPropertyName("html_url")]
        string HtmlUrl,
        [property: JsonPropertyName("default_branch")]
        string? DefaultBranch);
    
    private sealed record GitHubPullRequestDto(
        long Id,
        int Number,
        string Title,
        GitHubUserDto User,
        string State,
        [property: JsonPropertyName("created_at")]
        DateTimeOffset CreatedAt,
        [property: JsonPropertyName("merged_at")]
        DateTimeOffset? MergedAt,
        [property: JsonPropertyName("closed_at")]
        DateTimeOffset? ClosedAt);

    private sealed record GitHubUserDto(
        long Id);
    
    private sealed record GitHubPullRequestReviewDto(
        long Id,
        GitHubUserDto User,
        string State,
        [property: JsonPropertyName("submitted_at")]
        DateTimeOffset SubmittedAt);
    
    private sealed record GitHubDeploymentDto(
        long Id,
        string Environment,
        [property: JsonPropertyName("created_at")]
        DateTimeOffset CreatedAt);
    
    private sealed record GitHubDeploymentStatusDto(
        long Id,
        string State,
        [property: JsonPropertyName("created_at")]
        DateTimeOffset CreatedAt);
}

