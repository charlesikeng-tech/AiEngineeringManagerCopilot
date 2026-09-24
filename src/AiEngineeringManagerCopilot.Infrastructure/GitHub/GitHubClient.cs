using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using AiEngineeringManagerCopilot.Application.Abstractions;
using AiEngineeringManagerCopilot.Application.GitHub;

namespace AiEngineeringManagerCopilot.Infrastructure.GitHub;

public sealed class GitHubClient(
    HttpClient httpClient,
    IRetryDelay? retryDelay = null)
    : IGitHubClient
{
    private readonly IRetryDelay _retryDelay =
        retryDelay ?? new RetryDelay();
    
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

        using var response = await SendWithRetryAsync(
            () => CreateRequest(
                $"orgs/{Uri.EscapeDataString(organization)}",
                accessToken),
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
        var repositories =
            await GetAllPagesAsync<GitHubRepositoryDto>(
                $"orgs/{Uri.EscapeDataString(organization)}/repos",
                accessToken,
                cancellationToken);

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
        var pullRequests =
            await GetAllPagesAsync<GitHubPullRequestDto>(
                $"repos/{Uri.EscapeDataString(owner)}/" +
                $"{Uri.EscapeDataString(repository)}/pulls" +
                "?state=all",
                accessToken,
                cancellationToken);

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
        var reviews =
            await GetAllPagesAsync<GitHubPullRequestReviewDto>(
                $"repos/{Uri.EscapeDataString(owner)}/" +
                $"{Uri.EscapeDataString(repository)}/pulls/" +
                $"{pullRequestNumber}/reviews",
                accessToken,
                cancellationToken);

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
        var deployments =
            await GetAllPagesAsync<GitHubDeploymentDto>(
                $"repos/{Uri.EscapeDataString(owner)}/" +
                $"{Uri.EscapeDataString(repository)}/deployments",
                accessToken,
                cancellationToken);

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
        var statuses =
            await GetAllPagesAsync<GitHubDeploymentStatusDto>(
                $"repos/{Uri.EscapeDataString(owner)}/" +
                $"{Uri.EscapeDataString(repository)}/deployments/" +
                $"{deploymentId}/statuses",
                accessToken,
                cancellationToken);

        return statuses
            .Select(x => new GitHubDeploymentStatus(
                x.Id,
                x.State,
                x.CreatedAt))
            .ToList();
    }
    
    private async Task<IReadOnlyList<T>> GetAllPagesAsync<T>(
        string relativeUrl,
        string accessToken,
        CancellationToken cancellationToken)
    {
        var results = new List<T>();
        var page = 1;

        while (true)
        {
            var separator = relativeUrl.Contains('?')
                ? "&"
                : "?";

            var url =
                $"{relativeUrl}{separator}per_page=100&page={page}";

            using var response = await SendWithRetryAsync(
                () => CreateRequest(
                    url,
                    accessToken),
                cancellationToken);

            response.EnsureSuccessStatusCode();

            await using var stream =
                await response.Content.ReadAsStreamAsync(
                    cancellationToken);

            var pageResults =
                await JsonSerializer.DeserializeAsync<List<T>>(
                    stream,
                    JsonOptions,
                    cancellationToken);

            if (pageResults is null ||
                pageResults.Count == 0)
            {
                break;
            }

            results.AddRange(pageResults);

            if (pageResults.Count < 100)
            {
                break;
            }

            page++;
        }

        return results;
    }
    
    private async Task<HttpResponseMessage> SendWithRetryAsync(
        Func<HttpRequestMessage> requestFactory,
        CancellationToken cancellationToken)
    {
        const int maxAttempts = 2;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            using var request = requestFactory();

            var response = await httpClient.SendAsync(
                request,
                cancellationToken);

            if (!ShouldRetry(response) ||
                attempt == maxAttempts)
            {
                return response;
            }
            
            var delay = GetRetryDelay(response);

            await _retryDelay.DelayAsync(
                delay,
                cancellationToken);
            
            response.Dispose();
        }

        throw new InvalidOperationException(
            "Unexpected retry state.");
    }
    
    private static HttpRequestMessage CreateRequest(
        string relativeUrl,
        string accessToken)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Get,
            relativeUrl);

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);

        request.Headers.UserAgent.ParseAdd(
            "AiEngineeringManagerCopilot/1.0");

        request.Headers.Accept.Add(
            new MediaTypeWithQualityHeaderValue(
                "application/vnd.github+json"));

        return request;
    }
    
    private static bool ShouldRetry(
        HttpResponseMessage response)
    {
        var statusCode = (int)response.StatusCode;

        if (response.StatusCode ==
            HttpStatusCode.TooManyRequests)
        {
            return true;
        }

        if (statusCode is >= 500 and <= 599)
        {
            return true;
        }

        if (response.StatusCode ==
            HttpStatusCode.Forbidden &&
            response.Headers.TryGetValues(
                "X-RateLimit-Remaining",
                out var values) &&
            values.Any(value => value == "0"))
        {
            return true;
        }

        return false;
    }
    
    private static TimeSpan GetRetryDelay(
        HttpResponseMessage response)
    {
        var retryAfter = response.Headers.RetryAfter;

        if (retryAfter?.Delta is not null)
        {
            return retryAfter.Delta.Value;
        }

        if (retryAfter?.Date is not null)
        {
            var delay =
                retryAfter.Date.Value -
                DateTimeOffset.UtcNow;

            return delay > TimeSpan.Zero
                ? delay
                : TimeSpan.Zero;
        }

        var githubRateLimitDelay =
            GetGitHubRateLimitDelay(response);

        if (githubRateLimitDelay is not null)
        {
            return githubRateLimitDelay.Value;
        }

        return TimeSpan.FromSeconds(1);
    }
    
    private static TimeSpan? GetGitHubRateLimitDelay(
        HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues(
                "X-RateLimit-Reset",
                out var values))
        {
            return null;
        }

        var value = values.FirstOrDefault();

        if (!long.TryParse(value, out var unixTimestamp))
        {
            return null;
        }

        var resetAt =
            DateTimeOffset.FromUnixTimeSeconds(
                unixTimestamp);

        var delay =
            resetAt - DateTimeOffset.UtcNow;

        return delay > TimeSpan.Zero
            ? delay
            : TimeSpan.Zero;
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

