using System.Net;
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

    private sealed class FakeHttpMessageHandler(
        HttpStatusCode statusCode,
        string responseBody)
        : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequest = request;

            var response = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseBody)
            };

            return Task.FromResult(response);
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
}