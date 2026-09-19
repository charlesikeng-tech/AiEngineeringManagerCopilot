using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiEngineeringManagerCopilot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPullRequestReviewExternalId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_pull_request_reviews_PullRequestId",
                table: "pull_request_reviews");

            migrationBuilder.AddColumn<long>(
                name: "ExternalId",
                table: "pull_request_reviews",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_pull_request_reviews_PullRequestId_ExternalId",
                table: "pull_request_reviews",
                columns: new[] { "PullRequestId", "ExternalId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_pull_request_reviews_PullRequestId_ExternalId",
                table: "pull_request_reviews");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                table: "pull_request_reviews");

            migrationBuilder.CreateIndex(
                name: "IX_pull_request_reviews_PullRequestId",
                table: "pull_request_reviews",
                column: "PullRequestId");
        }
    }
}
