using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiEngineeringManagerCopilot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SupportGitHubUserAndOrganizationOwners : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Organization",
                table: "github_connections",
                newName: "Owner");

            migrationBuilder.AddColumn<string>(
                name: "OwnerType",
                table: "github_connections",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OwnerType",
                table: "github_connections");

            migrationBuilder.RenameColumn(
                name: "Owner",
                table: "github_connections",
                newName: "Organization");
        }
    }
}
