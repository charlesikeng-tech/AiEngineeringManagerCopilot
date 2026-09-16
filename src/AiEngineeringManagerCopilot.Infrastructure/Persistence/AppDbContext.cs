using AiEngineeringManagerCopilot.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AiEngineeringManagerCopilot.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Team> Teams => Set<Team>();

    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();

    public DbSet<GitHubConnection> GitHubConnections => Set<GitHubConnection>();

    public DbSet<Repository> Repositories => Set<Repository>();

    public DbSet<PullRequest> PullRequests => Set<PullRequest>();

    public DbSet<PullRequestReview> PullRequestReviews => Set<PullRequestReview>();

    public DbSet<EngineeringMetric> EngineeringMetrics => Set<EngineeringMetric>();

    public DbSet<EngineeringReport> EngineeringReports => Set<EngineeringReport>();

    public DbSet<EngineeringReportInsight> EngineeringReportInsights => Set<EngineeringReportInsight>();

    public DbSet<EngineeringRisk> EngineeringRisks => Set<EngineeringRisk>();

    public DbSet<EngineeringAction> EngineeringActions => Set<EngineeringAction>();
    
    public DbSet<Deployment> Deployments => Set<Deployment>();
    
    public DbSet<AIAnalysis> AIAnalyses => Set<AIAnalysis>();
    
    public DbSet<AIAnalysisInsight> AIAnalysisInsights => Set<AIAnalysisInsight>();
    
    public DbSet<AIAnalysisAction> AIAnalysisActions => Set<AIAnalysisAction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}