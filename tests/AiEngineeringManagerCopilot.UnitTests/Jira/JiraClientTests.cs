using System.Net;
using System.Text;
using AiEngineeringManagerCopilot.Infrastructure.Jira;
using FluentAssertions;

namespace AiEngineeringManagerCopilot.UnitTests.Jira;

public sealed class JiraClientTests
{
    [Fact]
    public async Task GetCurrentUserAsync_ShouldReturnCurrentUser()
    {
        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.OK,
            """
            {
                "accountId": "jira-account-123",
                "displayName": "Charles Ikeng",
                "emailAddress": "charles@example.com"
            }
            """);

        using var httpClient = new HttpClient(handler);

        var client = new JiraClient(httpClient);

        var result = await client.GetCurrentUserAsync(
            "https://my-company.atlassian.net",
            "charles@example.com",
            "secret-token",
            CancellationToken.None);

        result.Should().NotBeNull();

        result!.AccountId.Should()
            .Be("jira-account-123");

        result.DisplayName.Should()
            .Be("Charles Ikeng");

        result.EmailAddress.Should()
            .Be("charles@example.com");
    }

    [Fact]
    public async Task GetCurrentUserAsync_ShouldSendExpectedRequest()
    {
        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.OK,
            """
            {
                "accountId": "jira-account-123",
                "displayName": "Charles Ikeng",
                "emailAddress": "charles@example.com"
            }
            """);

        using var httpClient = new HttpClient(handler);

        var client = new JiraClient(httpClient);

        await client.GetCurrentUserAsync(
            "https://my-company.atlassian.net",
            "charles@example.com",
            "secret-token",
            CancellationToken.None);

        handler.LastRequest.Should().NotBeNull();

        handler.LastRequest!
            .Method
            .Should()
            .Be(HttpMethod.Get);

        handler.LastRequest!
            .RequestUri!
            .ToString()
            .Should()
            .Be(
                "https://my-company.atlassian.net/rest/api/3/myself");

        handler.LastRequest!
            .Headers.Authorization!
            .Scheme
            .Should()
            .Be("Basic");

        var expectedCredentials =
            Convert.ToBase64String(
                Encoding.UTF8.GetBytes(
                    "charles@example.com:secret-token"));

        handler.LastRequest!
            .Headers.Authorization!
            .Parameter
            .Should()
            .Be(expectedCredentials);

        handler.LastRequest!
            .RequestUri!
            .ToString()
            .Should()
            .NotContain("secret-token");
    }

    [Fact]
    public async Task GetCurrentUserAsync_WhenUnauthorized_ShouldReturnNull()
    {
        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.Unauthorized,
            "");

        using var httpClient = new HttpClient(handler);

        var client = new JiraClient(httpClient);

        var result = await client.GetCurrentUserAsync(
            "https://my-company.atlassian.net",
            "charles@example.com",
            "invalid-token",
            CancellationToken.None);

        result.Should().BeNull();
    }
    
    [Fact]
    public async Task GetIssuesAsync_ShouldReturnIssues()
    {
        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.OK,
            """
            {
                "issues": [
                    {
                        "id": "10001",
                        "key": "REC-123",
                        "fields": {
                            "summary": "Improve recommendation ranking",
                            "status": {
                                "name": "In Progress"
                            },
                            "assignee": {
                                "accountId": "jira-user-123"
                            },
                            "created": "2026-09-10T10:30:00Z"
                        }
                    },
                    {
                        "id": "10002",
                        "key": "REC-124",
                        "fields": {
                            "summary": "Fix similar ads API",
                            "status": {
                                "name": "To Do"
                            },
                            "assignee": null,
                            "created": "2026-09-11T08:00:00Z"
                        }
                    }
                ],
                "nextPageToken": null,
                "isLast": true
            }
            """);

        using var httpClient = new HttpClient(handler);

        var client = new JiraClient(httpClient);

        var result = await client.GetIssuesAsync(
            "https://my-company.atlassian.net",
            "charles@example.com",
            "secret-token",
            "REC",
            CancellationToken.None);

        result.Should().HaveCount(2);

        result[0].Id.Should().Be("10001");
        result[0].Key.Should().Be("REC-123");
        result[0].Summary.Should()
            .Be("Improve recommendation ranking");
        result[0].Status.Should()
            .Be("In Progress");
        result[0].AssigneeAccountId.Should()
            .Be("jira-user-123");
        result[0].CreatedAt.Should()
            .Be(DateTimeOffset.Parse(
                "2026-09-10T10:30:00Z"));

        result[1].Id.Should().Be("10002");
        result[1].Key.Should().Be("REC-124");
        result[1].Summary.Should()
            .Be("Fix similar ads API");
        result[1].Status.Should()
            .Be("To Do");
        result[1].AssigneeAccountId.Should()
            .BeNull();
    }
    
    [Fact]
    public async Task GetIssuesAsync_ShouldSendExpectedRequest()
    {
        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.OK,
            """
            {
                "issues": [],
                "nextPageToken": null,
                "isLast": true
            }
            """);

        using var httpClient = new HttpClient(handler);

        var client = new JiraClient(httpClient);

        await client.GetIssuesAsync(
            "https://my-company.atlassian.net",
            "charles@example.com",
            "secret-token",
            "REC",
            CancellationToken.None);

        handler.LastRequest.Should().NotBeNull();

        handler.LastRequest!
            .Method
            .Should()
            .Be(HttpMethod.Get);

        handler.LastRequest!
            .RequestUri!
            .AbsolutePath
            .Should()
            .Be("/rest/api/3/search/jql");

        var query = Uri.UnescapeDataString(
            handler.LastRequest!
                .RequestUri!
                .Query);

        query.Should()
            .Contain("project = \"REC\" ORDER BY created ASC");

        query.Should()
            .Contain("maxResults=100");

        query.Should()
            .Contain("fields=summary,status,assignee,created");

        handler.LastRequest!
            .Headers.Authorization!
            .Scheme
            .Should()
            .Be("Basic");

        handler.LastRequest!
            .RequestUri!
            .ToString()
            .Should()
            .NotContain("secret-token");
    }
    
    [Fact]
    public async Task GetIssuesAsync_ShouldFollowNextPageToken()
    {
        var firstResponse = new HttpResponseMessage(
            HttpStatusCode.OK)
        {
            Content = new StringContent(
                """
                {
                    "issues": [
                        {
                            "id": "10001",
                            "key": "REC-123",
                            "fields": {
                                "summary": "First issue",
                                "status": {
                                    "name": "In Progress"
                                },
                                "assignee": null,
                                "created": "2026-09-10T10:00:00Z"
                            }
                        }
                    ],
                    "nextPageToken": "next-page-123",
                    "isLast": false
                }
                """)
        };

        var secondResponse = new HttpResponseMessage(
            HttpStatusCode.OK)
        {
            Content = new StringContent(
                """
                {
                    "issues": [
                        {
                            "id": "10002",
                            "key": "REC-124",
                            "fields": {
                                "summary": "Second issue",
                                "status": {
                                    "name": "Done"
                                },
                                "assignee": null,
                                "created": "2026-09-11T10:00:00Z"
                            }
                        }
                    ],
                    "nextPageToken": null,
                    "isLast": true
                }
                """)
        };

        var handler = new FakeHttpMessageHandler(
            firstResponse,
            secondResponse);

        using var httpClient = new HttpClient(handler);

        var client = new JiraClient(httpClient);

        var result = await client.GetIssuesAsync(
            "https://my-company.atlassian.net",
            "charles@example.com",
            "secret-token",
            "REC",
            CancellationToken.None);

        result.Should().HaveCount(2);

        result.Select(x => x.Key)
            .Should()
            .ContainInOrder(
                "REC-123",
                "REC-124");

        handler.Requests.Should()
            .HaveCount(2);

        var secondRequestQuery =
            Uri.UnescapeDataString(
                handler.Requests[1]
                    .RequestUri!
                    .Query);

        secondRequestQuery.Should()
            .Contain("nextPageToken=next-page-123");
    }
    
    [Fact]
    public async Task GetIssuesAsync_WhenStatusIsBlocked_ShouldMarkIssueAsBlocked()
    {
        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.OK,
            """
            {
                "issues": [
                    {
                        "id": "10001",
                        "key": "REC-123",
                        "fields": {
                            "summary": "Blocked recommendation issue",
                            "status": {
                                "name": "Blocked"
                            },
                            "assignee": null,
                            "created": "2026-09-10T10:00:00Z"
                        }
                    }
                ],
                "nextPageToken": null,
                "isLast": true
            }
            """);

        using var httpClient = new HttpClient(handler);

        var client = new JiraClient(httpClient);

        var result = await client.GetIssuesAsync(
            "https://my-company.atlassian.net",
            "charles@example.com",
            "secret-token",
            "REC",
            CancellationToken.None);

        result.Should().ContainSingle();

        var issue = result.Single();

        issue.Key.Should().Be("REC-123");
        issue.Status.Should().Be("Blocked");
        issue.IsBlocked.Should().BeTrue();
    }

    private sealed class FakeHttpMessageHandler
        : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses;

        public HttpRequestMessage? LastRequest { get; private set; }

        public List<HttpRequestMessage> Requests { get; } = [];

        public FakeHttpMessageHandler(
            HttpStatusCode statusCode,
            string responseBody)
        {
            _responses =
                new Queue<HttpResponseMessage>();

            _responses.Enqueue(
                new HttpResponseMessage(statusCode)
                {
                    Content = new StringContent(responseBody)
                });
        }

        public FakeHttpMessageHandler(
            params HttpResponseMessage[] responses)
        {
            _responses =
                new Queue<HttpResponseMessage>(responses);
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequest = request;
            Requests.Add(request);

            if (_responses.Count == 0)
            {
                throw new InvalidOperationException(
                    "No fake HTTP response configured.");
            }

            return Task.FromResult(
                _responses.Dequeue());
        }
    }
}