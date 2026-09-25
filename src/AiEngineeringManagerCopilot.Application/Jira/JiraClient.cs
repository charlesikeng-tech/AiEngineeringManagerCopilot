using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;

namespace AiEngineeringManagerCopilot.Application.Jira;

public sealed class JiraClient(
    HttpClient httpClient,
    IJiraRetryDelay? retryDelay = null)
    : IJiraClient
{
    private readonly IJiraRetryDelay _retryDelay =
        retryDelay ?? new JiraRetryDelay();
    
    public async Task<JiraCurrentUser?> GetCurrentUserAsync(
        string baseUrl,
        string email,
        string apiToken,
        CancellationToken cancellationToken)
    {
        var url =
            $"{baseUrl.TrimEnd('/')}/rest/api/3/myself";

        using var response = await SendWithRetryAsync(
            () =>
            {
                var request = new HttpRequestMessage(
                    HttpMethod.Get,
                    url);

                SetAuthentication(
                    request,
                    email,
                    apiToken);

                return request;
            },
            cancellationToken);

        if (response.StatusCode is
            HttpStatusCode.Unauthorized or
            HttpStatusCode.Forbidden or
            HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        var jiraUser = await response.Content
            .ReadFromJsonAsync<JiraCurrentUserDto>(
                cancellationToken);

        if (jiraUser is null)
        {
            return null;
        }

        return new JiraCurrentUser(
            jiraUser.AccountId,
            jiraUser.DisplayName,
            jiraUser.EmailAddress ?? string.Empty);
    }

    public async Task<IReadOnlyList<JiraIssue>> GetIssuesAsync(
        string baseUrl,
        string email,
        string apiToken,
        string projectKey,
        CancellationToken cancellationToken)
    {
        var issues = new List<JiraIssue>();

        string? nextPageToken = null;

        do
        {
            var jql = $"project = \"{projectKey}\" ORDER BY created ASC";

            var query =
                $"jql={Uri.EscapeDataString(jql)}" +
                "&maxResults=100" +
                "&fields=summary,status,assignee,created";

            if (!string.IsNullOrWhiteSpace(nextPageToken))
            {
                query +=
                    $"&nextPageToken={Uri.EscapeDataString(nextPageToken)}";
            }

            var url =
                $"{baseUrl.TrimEnd('/')}/rest/api/3/search/jql?{query}";

            using var response = await SendWithRetryAsync(
                () =>
                {
                    var request = new HttpRequestMessage(
                        HttpMethod.Get,
                        url);

                    SetAuthentication(
                        request,
                        email,
                        apiToken);

                    return request;
                },
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var searchResult = await response.Content
                .ReadFromJsonAsync<JiraSearchResponseDto>(
                    cancellationToken);

            if (searchResult is null)
            {
                break;
            }

            foreach (var issue in searchResult.Issues)
            {
                issues.Add(
                    new JiraIssue(
                        issue.Id,
                        issue.Key,
                        issue.Fields.Summary,
                        issue.Fields.Status.Name,
                        issue.Fields.Assignee?.AccountId,
                        issue.Fields.Created,
                        issue.Fields.ResolutionDate,
                        IsBlocked(issue.Fields.Status.Name)));
            }

            nextPageToken = searchResult.IsLast
                ? null
                : searchResult.NextPageToken;

        } while (!string.IsNullOrWhiteSpace(nextPageToken));

        return issues;
    }

    private async Task<HttpResponseMessage> SendWithRetryAsync(
        Func<HttpRequestMessage> requestFactory,
        CancellationToken cancellationToken)
    {
        const int maxAttempts = 2;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            var request = requestFactory();

            try
            {
                var response = await httpClient.SendAsync(
                    request,
                    cancellationToken);

                var shouldRetry =
                    response.StatusCode == HttpStatusCode.TooManyRequests ||
                    (int)response.StatusCode >= 500;

                if (!shouldRetry ||
                    attempt == maxAttempts)
                {
                    request.Dispose();
                    return response;
                }

                var delay = GetRetryDelay(response);

                response.Dispose();
                request.Dispose();

                await _retryDelay.DelayAsync(
                    delay,
                    cancellationToken);
            }
            catch (HttpRequestException)
                when (attempt < maxAttempts)
            {
                request.Dispose();

                await _retryDelay.DelayAsync(
                    TimeSpan.FromSeconds(1),
                    cancellationToken);
            }
        }

        throw new InvalidOperationException(
            "Unexpected retry state.");
    }
    
    private static TimeSpan GetRetryDelay(
        HttpResponseMessage response)
    {
        var retryAfter = response.Headers.RetryAfter;

        if (retryAfter?.Delta is { } delta)
        {
            return delta;
        }

        if (retryAfter?.Date is { } date)
        {
            var delay = date - DateTimeOffset.UtcNow;

            return delay > TimeSpan.Zero
                ? delay
                : TimeSpan.Zero;
        }

        return TimeSpan.FromSeconds(1);
    }
    
    private sealed record JiraCurrentUserDto(
        [property: JsonPropertyName("accountId")]
        string AccountId,

        [property: JsonPropertyName("displayName")]
        string DisplayName,

        [property: JsonPropertyName("emailAddress")]
        string? EmailAddress);
    
    private sealed record JiraSearchResponseDto(
        [property: JsonPropertyName("issues")]
        IReadOnlyList<JiraIssueDto> Issues,

        [property: JsonPropertyName("nextPageToken")]
        string? NextPageToken,

        [property: JsonPropertyName("isLast")]
        bool IsLast);

    private sealed record JiraIssueDto(
        [property: JsonPropertyName("id")]
        string Id,

        [property: JsonPropertyName("key")]
        string Key,

        [property: JsonPropertyName("fields")]
        JiraIssueFieldsDto Fields);

    private sealed record JiraIssueFieldsDto(
        [property: JsonPropertyName("summary")]
        string Summary,

        [property: JsonPropertyName("status")]
        JiraStatusDto Status,

        [property: JsonPropertyName("assignee")]
        JiraAssigneeDto? Assignee,

        [property: JsonPropertyName("created")]
        DateTimeOffset Created,

        [property: JsonPropertyName("resolutiondate")]
        DateTimeOffset? ResolutionDate);
    

    private sealed record JiraStatusDto(
        [property: JsonPropertyName("name")]
        string Name);

    private sealed record JiraAssigneeDto(
        [property: JsonPropertyName("accountId")]
        string AccountId);
    
    private static void SetAuthentication(
        HttpRequestMessage request,
        string email,
        string apiToken)
    {
        var credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes(
                $"{email}:{apiToken}"));

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Basic",
                credentials);

        request.Headers.Accept.Add(
            new MediaTypeWithQualityHeaderValue(
                "application/json"));
    }
    
    private static bool IsBlocked(string status)
    {
        return string.Equals(
            status,
            "Blocked",
            StringComparison.OrdinalIgnoreCase);
    }
}