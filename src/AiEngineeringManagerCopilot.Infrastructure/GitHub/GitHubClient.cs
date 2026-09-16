using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using AiEngineeringManagerCopilot.Application.Abstractions;

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
}