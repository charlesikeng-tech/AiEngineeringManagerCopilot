using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiEngineeringManagerCopilot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAIAnalysisEvidence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ai_analysis_evidences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AIAnalysisId = table.Column<Guid>(type: "uuid", nullable: false),
                    MetricType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Value = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Confidence = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_analysis_evidences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ai_analysis_evidences_ai_analyses_AIAnalysisId",
                        column: x => x.AIAnalysisId,
                        principalTable: "ai_analyses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ai_analysis_evidences_AIAnalysisId",
                table: "ai_analysis_evidences",
                column: "AIAnalysisId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ai_analysis_evidences");
        }
    }
}
