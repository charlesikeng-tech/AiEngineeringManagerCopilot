using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiEngineeringManagerCopilot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLocalAdministratorAuthentication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Installations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    InitializedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Installations", x => x.Id);
                    table.CheckConstraint("CK_Installation_Singleton", "\"Id\" = 1");
                });

            migrationBuilder.CreateTable(
                name: "LocalAdministrators",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    NormalizedEmail = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Role = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    FailedLoginCount = table.Column<int>(type: "integer", nullable: false),
                    LockoutUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalAdministrators", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_LocalAdministrators_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AdministratorSessions",
                columns: table => new
                {
                    TokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdministratorSessions", x => x.TokenHash);
                    table.ForeignKey(
                        name: "FK_AdministratorSessions_LocalAdministrators_UserId",
                        column: x => x.UserId,
                        principalTable: "LocalAdministrators",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdministratorSessions_ExpiresAt",
                table: "AdministratorSessions",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_AdministratorSessions_UserId",
                table: "AdministratorSessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_LocalAdministrators_NormalizedEmail",
                table: "LocalAdministrators",
                column: "NormalizedEmail",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdministratorSessions");

            migrationBuilder.DropTable(
                name: "Installations");

            migrationBuilder.DropTable(
                name: "LocalAdministrators");
        }
    }
}
