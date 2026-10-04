using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiEngineeringManagerCopilot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExplicitAdministratorAccountLinking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SsoConnectionTests_AdministratorSessions_SessionHash",
                table: "SsoConnectionTests");

            migrationBuilder.AddColumn<string>(
                name: "LinkSourceSessionHash",
                table: "SsoLoginAttempts",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LinkTargetUserId",
                table: "SsoLoginAttempts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AdministratorAccessApproved",
                table: "ExternalIdentities",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_SsoLoginAttempts_LinkTargetUserId",
                table: "SsoLoginAttempts",
                column: "LinkTargetUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_SsoLoginAttempts_users_LinkTargetUserId",
                table: "SsoLoginAttempts",
                column: "LinkTargetUserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Ephemeral link/test proofs cannot be interpreted by the older schema.
            migrationBuilder.Sql("""
                DELETE FROM "SsoLoginAttempts" WHERE "LinkTargetUserId" IS NOT NULL;
                DELETE FROM "SsoConnectionTests" t WHERE NOT EXISTS (
                    SELECT 1 FROM "AdministratorSessions" s WHERE s."TokenHash" = t."SessionHash");
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_SsoLoginAttempts_users_LinkTargetUserId",
                table: "SsoLoginAttempts");

            migrationBuilder.DropIndex(
                name: "IX_SsoLoginAttempts_LinkTargetUserId",
                table: "SsoLoginAttempts");

            migrationBuilder.DropColumn(
                name: "LinkSourceSessionHash",
                table: "SsoLoginAttempts");

            migrationBuilder.DropColumn(
                name: "LinkTargetUserId",
                table: "SsoLoginAttempts");

            migrationBuilder.DropColumn(
                name: "AdministratorAccessApproved",
                table: "ExternalIdentities");

            migrationBuilder.AddForeignKey(
                name: "FK_SsoConnectionTests_AdministratorSessions_SessionHash",
                table: "SsoConnectionTests",
                column: "SessionHash",
                principalTable: "AdministratorSessions",
                principalColumn: "TokenHash",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
