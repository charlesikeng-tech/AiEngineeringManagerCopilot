using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiEngineeringManagerCopilot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HardenAnalysisAndSnapshotReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM ai_analyses GROUP BY "ReportId" HAVING COUNT(*) > 1) THEN
                        RAISE EXCEPTION 'Duplicate AI analyses exist. Review and resolve duplicate ReportId values before applying this migration.';
                    END IF;
                END $$;
                """);

            migrationBuilder.DropIndex(
                name: "IX_ai_analyses_ReportId",
                table: "ai_analyses");

            migrationBuilder.AddColumn<string>(
                name: "SnapshotJson",
                table: "engineering_reports",
                type: "jsonb",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ai_analyses_ReportId",
                table: "ai_analyses",
                column: "ReportId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ai_analyses_ReportId",
                table: "ai_analyses");

            migrationBuilder.DropColumn(
                name: "SnapshotJson",
                table: "engineering_reports");

            migrationBuilder.CreateIndex(
                name: "IX_ai_analyses_ReportId",
                table: "ai_analyses",
                column: "ReportId");
        }
    }
}
