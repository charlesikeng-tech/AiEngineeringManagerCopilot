using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiEngineeringManagerCopilot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEngineeringReportDataCoverage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DataCoverage",
                table: "engineering_reports",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddCheckConstraint(
                name: "CK_engineering_reports_data_coverage",
                table: "engineering_reports",
                sql: "\"DataCoverage\" >= 0 AND \"DataCoverage\" <= 100");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_engineering_reports_data_coverage",
                table: "engineering_reports");

            migrationBuilder.DropColumn(
                name: "DataCoverage",
                table: "engineering_reports");
        }
    }
}
