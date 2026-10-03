using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiEngineeringManagerCopilot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSsoProviderDrafts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SsoProviders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Authority = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ClientId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ProtectedSecret = table.Column<string>(type: "text", nullable: true),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    TestedRevision = table.Column<int>(type: "integer", nullable: true),
                    TestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastTestError = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CurrentTestId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActiveConfiguration = table.Column<string>(type: "text", nullable: true),
                    ActiveRevision = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SsoProviders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SsoConnectionTests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    SessionHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Consumed = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SsoConnectionTests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SsoConnectionTests_AdministratorSessions_SessionHash",
                        column: x => x.SessionHash,
                        principalTable: "AdministratorSessions",
                        principalColumn: "TokenHash",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SsoConnectionTests_SsoProviders_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "SsoProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SsoConnectionTests_ExpiresAt",
                table: "SsoConnectionTests",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_SsoConnectionTests_ProviderId",
                table: "SsoConnectionTests",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_SsoConnectionTests_SessionHash",
                table: "SsoConnectionTests",
                column: "SessionHash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SsoConnectionTests");

            migrationBuilder.DropTable(
                name: "SsoProviders");
        }
    }
}
