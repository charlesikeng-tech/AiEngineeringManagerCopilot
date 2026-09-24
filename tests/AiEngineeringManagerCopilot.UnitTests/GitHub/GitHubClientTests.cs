using System.Net;
using System.Text;
using System.Text.Json;
using AiEngineeringManagerCopilot.Infrastructure.GitHub;
using FluentAssertions;

namespace AiEngineeringManagerCopilot.UnitTests.GitHub;

public sealed class GitHubClientTests
{
    [Fact]
    public async Task GetOrganizationAsync_ShouldReturnOrganization()
    {
        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.OK,
            """
            {
                "id": 123456,
                "login": "my-company",
                "name": "My Company",
                "html_url": "https://github.com/my-company"
            }
            """);

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(
                "https://api.github.com/")
        };

        var client = new GitHubClient(httpClient);

        var result = await client.GetOrganizationAsync(
            "my-company",
            "secret-token",
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(123456);
        result.Login.Should().Be("my-company");
        result.Name.Should().Be("My Company");
        result.HtmlUrl.Should()
            .Be("https://github.com/my-company");
    }

    [Fact]
    public async Task GetOrganizationAsync_WhenNotFound_ShouldReturnNull()
    {
        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.NotFound,
            "");

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(
                "https://api.github.com/")
        };

        var client = new GitHubClient(httpClient);

        var result = await client.GetOrganizationAsync(
            "unknown-company",
            "secret-token",
            CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetOrganizationAsync_ShouldSendBearerToken()
    {
        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.OK,
            """
            {
                "id": 123456,
                "login": "my-company",
                "name": "My Company",
                "html_url": "https://github.com/my-company"
            }
            """);

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(
                "https://api.github.com/")
        };

        var client = new GitHubClient(httpClient);

        await client.GetOrganizationAsync(
            "my-company",
            "secret-token",
            CancellationToken.None);

        handler.LastRequest
            .Should()
            .NotBeNull();

        handler.LastRequest!
            .Headers.Authorization!
            .Scheme
            .Should()
            .Be("Bearer");

        handler.LastRequest!
            .Headers.Authorization!
            .Parameter
            .Should()
            .Be("secret-token");
    }
    
    [Fact]
    public async Task GetPullRequestsAsync_ShouldReturnPullRequests()
    {
        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.OK,
            """
            [
                {
                    "id": 1001,
                    "number": 42,
                    "title": "Add engineering health score",
                    "user": {
                        "id": 98765
                    },
                    "state": "closed",
                    "created_at": "2026-09-01T10:00:00Z",
                    "merged_at": "2026-09-03T14:30:00Z",
                    "closed_at": "2026-09-03T14:30:00Z"
                },
                {
                    "id": 1002,
                    "number": 43,
                    "title": "Improve GitHub synchronization",
                    "user": {
                        "id": 12345
                    },
                    "state": "open",
                    "created_at": "2026-09-04T08:00:00Z",
                    "merged_at": null,
                    "closed_at": null
                }
            ]
            """);

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(
                "https://api.github.com/")
        };

        var client = new GitHubClient(httpClient);

        var result = await client.GetPullRequestsAsync(
            "secret-token",
            "my-company",
            "backend",
            CancellationToken.None);

        result.Should().HaveCount(2);

        result[0].Id.Should().Be(1001);
        result[0].Number.Should().Be(42);
        result[0].Title.Should()
            .Be("Add engineering health score");
        result[0].AuthorExternalId.Should()
            .Be("98765");
        result[0].State.Should().Be("closed");
        result[0].CreatedAt.Should()
            .Be(DateTimeOffset.Parse(
                "2026-09-01T10:00:00Z"));
        result[0].MergedAt.Should()
            .Be(DateTimeOffset.Parse(
                "2026-09-03T14:30:00Z"));
        result[0].ClosedAt.Should()
            .Be(DateTimeOffset.Parse(
                "2026-09-03T14:30:00Z"));

        result[1].Id.Should().Be(1002);
        result[1].Number.Should().Be(43);
        result[1].State.Should().Be("open");
        result[1].MergedAt.Should().BeNull();
        result[1].ClosedAt.Should().BeNull();
    }

    [Fact]
    public async Task GetPullRequestsAsync_ShouldSendExpectedRequest()
    {
        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.OK,
            "[]");

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(
                "https://api.github.com/")
        };

        var client = new GitHubClient(httpClient);

        await client.GetPullRequestsAsync(
            "secret-token",
            "my-company",
            "backend",
            CancellationToken.None);

        handler.LastRequest.Should().NotBeNull();

        handler.LastRequest!
            .Method
            .Should()
            .Be(HttpMethod.Get);

        handler.LastRequest!
            .RequestUri!
            .PathAndQuery
            .Should()
            .Be(
                "/repos/my-company/backend/pulls" +
                "?state=all&per_page=100&page=1");

        handler.LastRequest!
            .Headers.Authorization!
            .Scheme
            .Should()
            .Be("Bearer");

        handler.LastRequest!
            .Headers.Authorization!
            .Parameter
            .Should()
            .Be("secret-token");

        handler.LastRequest!
            .RequestUri!
            .ToString()
            .Should()
            .NotContain("secret-token");
    }

    [Fact]
    public async Task GetPullRequestsAsync_ShouldReturnAllPages()
    {
        var firstPage = Enumerable.Range(1, 100)
            .Select(i => new
            {
                id = (long)i,
                number = i,
                title = $"PR {i}",
                user = new { id = 1000L + i },
                state = "open",
                created_at = DateTimeOffset.UtcNow,
                merged_at = (DateTimeOffset?)null,
                closed_at = (DateTimeOffset?)null
            })
            .ToArray();

        var secondPage = Enumerable.Range(101, 25)
            .Select(i => new
            {
                id = (long)i,
                number = i,
                title = $"PR {i}",
                user = new { id = 1000L + i },
                state = "open",
                created_at = DateTimeOffset.UtcNow,
                merged_at = (DateTimeOffset?)null,
                closed_at = (DateTimeOffset?)null
            })
            .ToArray();

        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.OK,
            request =>
            {
                var query = request.RequestUri!.Query;

                return query.Contains("page=2")
                    ? JsonSerializer.Serialize(secondPage)
                    : JsonSerializer.Serialize(firstPage);
            });

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(
                "https://api.github.com/")
        };

        var client = new GitHubClient(httpClient);

        var result = await client.GetPullRequestsAsync(
            "secret-token",
            "my-company",
            "backend",
            CancellationToken.None);

        result.Should().HaveCount(125);

        result.First().Number.Should().Be(1);
        result.Last().Number.Should().Be(125);

        handler.Requests.Should().HaveCount(2);

        handler.Requests[0]
            .RequestUri!
            .PathAndQuery
            .Should()
            .EndWith("per_page=100&page=1");

        handler.Requests[1]
            .RequestUri!
            .PathAndQuery
            .Should()
            .EndWith("per_page=100&page=2");
    }
    
    [Fact]
    public async Task GetRepositoriesAsync_ShouldReturnAllPages()
    {
        var firstPage = Enumerable.Range(1, 100)
            .Select(i => new
            {
                id = (long)i,
                name = $"repo-{i}",
                full_name = $"my-company/repo-{i}",
                html_url = $"https://github.com/my-company/repo-{i}",
                default_branch = "main"
            })
            .ToArray();

        var secondPage = Enumerable.Range(101, 25)
            .Select(i => new
            {
                id = (long)i,
                name = $"repo-{i}",
                full_name = $"my-company/repo-{i}",
                html_url = $"https://github.com/my-company/repo-{i}",
                default_branch = "main"
            })
            .ToArray();

        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.OK,
            request =>
            {
                var query = request.RequestUri!.Query;

                return query.Contains("page=2")
                    ? JsonSerializer.Serialize(secondPage)
                    : JsonSerializer.Serialize(firstPage);
            });

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(
                "https://api.github.com/")
        };

        var client = new GitHubClient(httpClient);

        var result = await client.GetRepositoriesAsync(
            "my-company",
            "secret-token",
            CancellationToken.None);

        result.Should().HaveCount(125);

        result.First().Name.Should().Be("repo-1");
        result.Last().Name.Should().Be("repo-125");

        handler.Requests.Should().HaveCount(2);

        handler.Requests[0]
            .RequestUri!
            .PathAndQuery
            .Should()
            .Be("/orgs/my-company/repos?per_page=100&page=1");

        handler.Requests[1]
            .RequestUri!
            .PathAndQuery
            .Should()
            .Be("/orgs/my-company/repos?per_page=100&page=2");
    }
    
    private sealed class FakeHttpMessageHandler
        : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string? _responseBody;
        private readonly Func<HttpRequestMessage, string>? _responseFactory;
        private readonly Queue<HttpResponseMessage>? _responses;

        public HttpRequestMessage? LastRequest { get; private set; }

        public List<HttpRequestMessage> Requests { get; } = [];

        public FakeHttpMessageHandler(
            HttpStatusCode statusCode,
            string responseBody)
        {
            _statusCode = statusCode;
            _responseBody = responseBody;
        }

        public FakeHttpMessageHandler(
            HttpStatusCode statusCode,
            Func<HttpRequestMessage, string> responseFactory)
        {
            _statusCode = statusCode;
            _responseFactory = responseFactory;
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

            if (_responses is not null)
            {
                if (_responses.Count == 0)
                {
                    throw new InvalidOperationException(
                        "No fake HTTP response configured.");
                }

                return Task.FromResult(
                    _responses.Dequeue());
            }

            var responseBody = _responseFactory is not null
                ? _responseFactory(request)
                : _responseBody!;

            return Task.FromResult(
                new HttpResponseMessage(_statusCode)
                {
                    Content = new StringContent(responseBody)
                });
        }
    }
    
    [Fact]
    public async Task GetOrganizationAsync_ShouldNotSendTokenInUrl()
    {
        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.OK,
            """
            {
                "id": 123456,
                "login": "my-company",
                "name": "My Company",
                "html_url": "https://github.com/my-company"
            }
            """);

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(
                "https://api.github.com/")
        };

        var client = new GitHubClient(httpClient);

        await client.GetOrganizationAsync(
            "my-company",
            "secret-token",
            CancellationToken.None);

        handler.LastRequest!
            .RequestUri!
            .ToString()
            .Should()
            .NotContain("secret-token");
    }
    
    [Fact]
    public async Task GetRepositoriesAsync_ShouldReturnRepositories()
    {
        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.OK,
            """
            [
                {
                    "id": 123,
                    "name": "backend",
                    "full_name": "my-company/backend",
                    "html_url": "https://github.com/my-company/backend",
                    "default_branch": "main"
                },
                {
                    "id": 456,
                    "name": "frontend",
                    "full_name": "my-company/frontend",
                    "html_url": "https://github.com/my-company/frontend",
                    "default_branch": "main"
                }
            ]
            """);

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(
                "https://api.github.com/")
        };

        var client = new GitHubClient(httpClient);

        var result = await client.GetRepositoriesAsync(
            "my-company",
            "secret-token",
            CancellationToken.None);

        result.Should().HaveCount(2);

        result[0].Id.Should().Be(123);
        result[0].Name.Should().Be("backend");
        result[0].FullName.Should()
            .Be("my-company/backend");
        result[0].HtmlUrl.Should()
            .Be("https://github.com/my-company/backend");
        result[0].DefaultBranch.Should()
            .Be("main");
    }
    
    [Fact]
    public async Task GetRepositoriesAsync_ShouldSendBearerToken()
    {
        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.OK,
            "[]");

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(
                "https://api.github.com/")
        };

        var client = new GitHubClient(httpClient);

        await client.GetRepositoriesAsync(
            "my-company",
            "secret-token",
            CancellationToken.None);

        handler.LastRequest.Should().NotBeNull();

        handler.LastRequest!
            .Headers.Authorization!
            .Scheme
            .Should()
            .Be("Bearer");

        handler.LastRequest!
            .Headers.Authorization!
            .Parameter
            .Should()
            .Be("secret-token");
    }
    
    [Fact]
    public async Task GetRepositoriesAsync_ShouldNotSendTokenInUrl()
    {
        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.OK,
            "[]");

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(
                "https://api.github.com/")
        };

        var client = new GitHubClient(httpClient);

        await client.GetRepositoriesAsync(
            "my-company",
            "secret-token",
            CancellationToken.None);

        handler.LastRequest!
            .RequestUri!
            .ToString()
            .Should()
            .NotContain("secret-token");
    }
    
    [Fact]
    public async Task GetPullRequestReviewsAsync_ShouldReturnReviews()
    {
        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.OK,
            """
            [
                {
                    "id": 7001,
                    "user": {
                        "id": 12345
                    },
                    "state": "APPROVED",
                    "submitted_at": "2026-09-02T14:00:00Z"
                },
                {
                    "id": 7002,
                    "user": {
                        "id": 67890
                    },
                    "state": "CHANGES_REQUESTED",
                    "submitted_at": "2026-09-02T16:30:00Z"
                }
            ]
            """);

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(
                "https://api.github.com/")
        };

        var client = new GitHubClient(httpClient);

        var result = await client.GetPullRequestReviewsAsync(
            "secret-token",
            "my-company",
            "backend",
            42,
            CancellationToken.None);

        result.Should().HaveCount(2);

        result[0].Id.Should().Be(7001);
        result[0].ReviewerExternalId.Should().Be("12345");
        result[0].State.Should().Be("APPROVED");
        result[0].SubmittedAt.Should().Be(
            DateTimeOffset.Parse("2026-09-02T14:00:00Z"));

        result[1].Id.Should().Be(7002);
        result[1].ReviewerExternalId.Should().Be("67890");
        result[1].State.Should().Be("CHANGES_REQUESTED");
        result[1].SubmittedAt.Should().Be(
            DateTimeOffset.Parse("2026-09-02T16:30:00Z"));
    }
    
    [Fact]
    public async Task GetPullRequestReviewsAsync_ShouldSendExpectedRequest()
    {
        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.OK,
            "[]");

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(
                "https://api.github.com/")
        };

        var client = new GitHubClient(httpClient);

        await client.GetPullRequestReviewsAsync(
            "secret-token",
            "my-company",
            "backend",
            42,
            CancellationToken.None);

        handler.LastRequest.Should().NotBeNull();

        handler.LastRequest!
            .Method
            .Should()
            .Be(HttpMethod.Get);

        handler.LastRequest!
            .RequestUri!
            .PathAndQuery
            .Should()
            .Be(
                "/repos/my-company/backend/pulls/" +
                "42/reviews?per_page=100&page=1");

        handler.LastRequest!
            .Headers.Authorization!
            .Scheme
            .Should()
            .Be("Bearer");

        handler.LastRequest!
            .Headers.Authorization!
            .Parameter
            .Should()
            .Be("secret-token");

        handler.LastRequest!
            .RequestUri!
            .ToString()
            .Should()
            .NotContain("secret-token");
    }
    
    [Fact]
    public async Task GetDeploymentsAsync_ShouldReturnDeployments()
    {
        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.OK,
            """
            [
                {
                    "id": 8001,
                    "environment": "production",
                    "created_at": "2026-09-10T14:30:00Z"
                },
                {
                    "id": 8002,
                    "environment": "staging",
                    "created_at": "2026-09-11T09:15:00Z"
                }
            ]
            """);

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(
                "https://api.github.com/")
        };

        var client = new GitHubClient(httpClient);

        var result = await client.GetDeploymentsAsync(
            "secret-token",
            "my-company",
            "backend",
            CancellationToken.None);

        result.Should().HaveCount(2);

        result[0].Id.Should().Be(8001);
        result[0].Environment.Should().Be("production");
        result[0].CreatedAt.Should().Be(
            DateTimeOffset.Parse(
                "2026-09-10T14:30:00Z"));

        result[1].Id.Should().Be(8002);
        result[1].Environment.Should().Be("staging");
        result[1].CreatedAt.Should().Be(
            DateTimeOffset.Parse(
                "2026-09-11T09:15:00Z"));
    }
    
    [Fact]
    public async Task GetDeploymentsAsync_ShouldSendExpectedRequest()
    {
        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.OK,
            "[]");

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(
                "https://api.github.com/")
        };

        var client = new GitHubClient(httpClient);

        await client.GetDeploymentsAsync(
            "secret-token",
            "my-company",
            "backend",
            CancellationToken.None);

        handler.LastRequest.Should().NotBeNull();

        handler.LastRequest!
            .Method
            .Should()
            .Be(HttpMethod.Get);

        handler.LastRequest!
            .RequestUri!
            .PathAndQuery
            .Should()
            .Be(
                "/repos/my-company/backend/deployments?per_page=100&page=1");

        handler.LastRequest!
            .Headers.Authorization!
            .Scheme
            .Should()
            .Be("Bearer");

        handler.LastRequest!
            .Headers.Authorization!
            .Parameter
            .Should()
            .Be("secret-token");

        handler.LastRequest!
            .RequestUri!
            .ToString()
            .Should()
            .NotContain("secret-token");
    }
    
    [Fact]
    public async Task GetDeploymentStatusesAsync_ShouldReturnStatuses()
    {
        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.OK,
            """
            [
                {
                    "id": 9001,
                    "state": "in_progress",
                    "created_at": "2026-09-10T14:30:00Z"
                },
                {
                    "id": 9002,
                    "state": "success",
                    "created_at": "2026-09-10T14:35:00Z"
                }
            ]
            """);

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(
                "https://api.github.com/")
        };

        var client = new GitHubClient(httpClient);

        var result =
            await client.GetDeploymentStatusesAsync(
                "secret-token",
                "my-company",
                "backend",
                8001,
                CancellationToken.None);

        result.Should().HaveCount(2);

        result[0].Id.Should().Be(9001);
        result[0].State.Should().Be("in_progress");
        result[0].CreatedAt.Should().Be(
            DateTimeOffset.Parse(
                "2026-09-10T14:30:00Z"));

        result[1].Id.Should().Be(9002);
        result[1].State.Should().Be("success");
        result[1].CreatedAt.Should().Be(
            DateTimeOffset.Parse(
                "2026-09-10T14:35:00Z"));
    }
    
    [Fact]
    public async Task GetDeploymentStatusesAsync_ShouldSendExpectedRequest()
    {
        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.OK,
            "[]");

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(
                "https://api.github.com/")
        };

        var client = new GitHubClient(httpClient);

        await client.GetDeploymentStatusesAsync(
            "secret-token",
            "my-company",
            "backend",
            8001,
            CancellationToken.None);

        handler.LastRequest.Should().NotBeNull();

        handler.LastRequest!
            .Method
            .Should()
            .Be(HttpMethod.Get);

        handler.LastRequest!
            .RequestUri!
            .PathAndQuery
            .Should()
            .Be(
                "/repos/my-company/backend/deployments/" +
                "8001/statuses?per_page=100&page=1");

        handler.LastRequest!
            .Headers.Authorization!
            .Scheme
            .Should()
            .Be("Bearer");

        handler.LastRequest!
            .Headers.Authorization!
            .Parameter
            .Should()
            .Be("secret-token");

        handler.LastRequest!
            .RequestUri!
            .ToString()
            .Should()
            .NotContain("secret-token");
    }
    
    [Fact]
    public async Task GetPullRequestReviewsAsync_ShouldReturnAllPages()
    {
        var firstPage = Enumerable.Range(1, 100)
            .Select(i => new
            {
                id = (long)i,
                user = new { id = 1000L + i },
                state = "APPROVED",
                submitted_at = DateTimeOffset.UtcNow
            })
            .ToArray();

        var secondPage = Enumerable.Range(101, 25)
            .Select(i => new
            {
                id = (long)i,
                user = new { id = 1000L + i },
                state = "APPROVED",
                submitted_at = DateTimeOffset.UtcNow
            })
            .ToArray();

        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.OK,
            request =>
            {
                var query = request.RequestUri!.Query;

                return query.Contains("page=2")
                    ? JsonSerializer.Serialize(secondPage)
                    : JsonSerializer.Serialize(firstPage);
            });

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(
                "https://api.github.com/")
        };

        var client = new GitHubClient(httpClient);

        var result = await client.GetPullRequestReviewsAsync(
            "secret-token",
            "my-company",
            "backend",
            42,
            CancellationToken.None);

        result.Should().HaveCount(125);

        result.First().Id.Should().Be(1);
        result.Last().Id.Should().Be(125);

        handler.Requests.Should().HaveCount(2);

        handler.Requests[0]
            .RequestUri!
            .PathAndQuery
            .Should()
            .Be(
                "/repos/my-company/backend/pulls/42/reviews" +
                "?per_page=100&page=1");

        handler.Requests[1]
            .RequestUri!
            .PathAndQuery
            .Should()
            .Be(
                "/repos/my-company/backend/pulls/42/reviews" +
                "?per_page=100&page=2");
    }
    
    [Fact]
    public async Task GetDeploymentsAsync_ShouldReturnAllPages()
    {
        var firstPage = Enumerable.Range(1, 100)
            .Select(i => new
            {
                id = (long)i,
                environment = "production",
                created_at = DateTimeOffset.UtcNow
            })
            .ToArray();

        var secondPage = Enumerable.Range(101, 25)
            .Select(i => new
            {
                id = (long)i,
                environment = "production",
                created_at = DateTimeOffset.UtcNow
            })
            .ToArray();

        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.OK,
            request =>
            {
                var query = request.RequestUri!.Query;

                return query.Contains("page=2")
                    ? JsonSerializer.Serialize(secondPage)
                    : JsonSerializer.Serialize(firstPage);
            });

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(
                "https://api.github.com/")
        };

        var client = new GitHubClient(httpClient);

        var result = await client.GetDeploymentsAsync(
            "secret-token",
            "my-company",
            "backend",
            CancellationToken.None);

        result.Should().HaveCount(125);

        result.First().Id.Should().Be(1);
        result.Last().Id.Should().Be(125);

        handler.Requests.Should().HaveCount(2);

        handler.Requests[0]
            .RequestUri!
            .PathAndQuery
            .Should()
            .Be(
                "/repos/my-company/backend/deployments" +
                "?per_page=100&page=1");

        handler.Requests[1]
            .RequestUri!
            .PathAndQuery
            .Should()
            .Be(
                "/repos/my-company/backend/deployments" +
                "?per_page=100&page=2");
    }
    
    [Fact]
    public async Task GetDeploymentStatusesAsync_ShouldReturnAllPages()
    {
        var firstPage = Enumerable.Range(1, 100)
            .Select(i => new
            {
                id = (long)i,
                state = "success",
                created_at = DateTimeOffset.UtcNow
            })
            .ToArray();

        var secondPage = Enumerable.Range(101, 25)
            .Select(i => new
            {
                id = (long)i,
                state = "success",
                created_at = DateTimeOffset.UtcNow
            })
            .ToArray();

        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.OK,
            request =>
            {
                var query = request.RequestUri!.Query;

                return query.Contains("page=2")
                    ? JsonSerializer.Serialize(secondPage)
                    : JsonSerializer.Serialize(firstPage);
            });

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(
                "https://api.github.com/")
        };

        var client = new GitHubClient(httpClient);

        var result = await client.GetDeploymentStatusesAsync(
            "secret-token",
            "my-company",
            "backend",
            123,
            CancellationToken.None);

        result.Should().HaveCount(125);

        result.First().Id.Should().Be(1);
        result.Last().Id.Should().Be(125);

        handler.Requests.Should().HaveCount(2);

        handler.Requests[0]
            .RequestUri!
            .PathAndQuery
            .Should()
            .Be(
                "/repos/my-company/backend/deployments/123/statuses" +
                "?per_page=100&page=1");

        handler.Requests[1]
            .RequestUri!
            .PathAndQuery
            .Should()
            .Be(
                "/repos/my-company/backend/deployments/123/statuses" +
                "?per_page=100&page=2");
    }
    
    [Fact]
    public async Task GetOrganizationAsync_WhenServerError_ShouldRetry()
    {
        var firstResponse =
            new HttpResponseMessage(
                HttpStatusCode.InternalServerError);

        var secondResponse =
            new HttpResponseMessage(
                HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {
                        "id": 123456,
                        "login": "my-company",
                        "name": "My Company",
                        "html_url": "https://github.com/my-company"
                    }
                    """)
            };

        var handler = new FakeHttpMessageHandler(
            firstResponse,
            secondResponse);

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(
                "https://api.github.com/")
        };

        var client = new GitHubClient(httpClient);

        var result = await client.GetOrganizationAsync(
            "my-company",
            "secret-token",
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.Login.Should().Be("my-company");

        handler.Requests.Should().HaveCount(2);
    }
    
    [Fact]
    public async Task GetPullRequestsAsync_WhenServerError_ShouldRetry()
    {
        var firstResponse =
            new HttpResponseMessage(
                HttpStatusCode.InternalServerError);

        var secondResponse =
            new HttpResponseMessage(
                HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    [
                        {
                            "id": 1001,
                            "number": 42,
                            "title": "Retry successful",
                            "user": {
                                "id": 98765
                            },
                            "state": "open",
                            "created_at": "2026-09-24T08:00:00Z",
                            "merged_at": null,
                            "closed_at": null
                        }
                    ]
                    """)
            };

        var handler = new FakeHttpMessageHandler(
            firstResponse,
            secondResponse);

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(
                "https://api.github.com/")
        };

        var client = new GitHubClient(httpClient);

        var result = await client.GetPullRequestsAsync(
            "secret-token",
            "my-company",
            "backend",
            CancellationToken.None);

        result.Should().ContainSingle();

        result.Single().Number.Should().Be(42);

        handler.Requests.Should().HaveCount(2);

        handler.Requests[0]
            .RequestUri!
            .PathAndQuery
            .Should()
            .Be(
                "/repos/my-company/backend/pulls" +
                "?state=all&per_page=100&page=1");

        handler.Requests[1]
            .RequestUri!
            .PathAndQuery
            .Should()
            .Be(
                "/repos/my-company/backend/pulls" +
                "?state=all&per_page=100&page=1");
    }
    
    [Fact]
    public async Task GetOrganizationAsync_WhenTooManyRequests_ShouldRetry()
    {
        var firstResponse =
            new HttpResponseMessage(
                HttpStatusCode.TooManyRequests);

        var secondResponse =
            new HttpResponseMessage(
                HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {
                        "id": 123456,
                        "login": "my-company",
                        "name": "My Company",
                        "html_url": "https://github.com/my-company"
                    }
                    """)
            };

        var handler = new FakeHttpMessageHandler(
            firstResponse,
            secondResponse);

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(
                "https://api.github.com/")
        };

        var client = new GitHubClient(httpClient);

        var result = await client.GetOrganizationAsync(
            "my-company",
            "secret-token",
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.Login.Should().Be("my-company");

        handler.Requests.Should().HaveCount(2);
    }
    
    [Fact]
    public async Task GetOrganizationAsync_WhenForbidden_ShouldNotRetry()
    {
        var response =
            new HttpResponseMessage(
                HttpStatusCode.Forbidden);

        var handler = new FakeHttpMessageHandler(
            response);

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(
                "https://api.github.com/")
        };

        var client = new GitHubClient(httpClient);

        var act = async () =>
            await client.GetOrganizationAsync(
                "my-company",
                "secret-token",
                CancellationToken.None);

        await act.Should()
            .ThrowAsync<HttpRequestException>();

        handler.Requests.Should().ContainSingle();
    }
    
    [Fact]
    public async Task GetOrganizationAsync_WhenRateLimitExceeded_ShouldRetry()
    {
        var rateLimitResponse =
            new HttpResponseMessage(
                HttpStatusCode.Forbidden);

        rateLimitResponse.Headers.Add(
            "X-RateLimit-Remaining",
            "0");

        var successResponse =
            new HttpResponseMessage(
                HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {
                        "id": 123456,
                        "login": "my-company",
                        "name": "My Company",
                        "html_url": "https://github.com/my-company"
                    }
                    """)
            };

        var handler = new FakeHttpMessageHandler(
            rateLimitResponse,
            successResponse);

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(
                "https://api.github.com/")
        };

        var client = new GitHubClient(httpClient);

        var result = await client.GetOrganizationAsync(
            "my-company",
            "secret-token",
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.Login.Should().Be("my-company");

        handler.Requests.Should().HaveCount(2);
    }
}