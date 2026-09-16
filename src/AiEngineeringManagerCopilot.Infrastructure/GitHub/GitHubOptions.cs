namespace AiEngineeringManagerCopilot.Infrastructure.GitHub;

public sealed class GitHubOptions
{
    public const string SectionName = "GitHub";

    public string BaseUrl { get; set; } =
        "https://api.github.com/";
}