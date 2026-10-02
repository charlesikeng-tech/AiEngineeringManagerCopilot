using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiEngineeringManagerCopilot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEngineeringMetricUpdatedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "engineering_metrics",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE engineering_metrics
                SET "UpdatedAt" = "CreatedAt";
                """);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "engineering_metrics",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "engineering_metrics");
        }
    }
}
