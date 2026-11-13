using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorApp3.Data.Migrations;

/// <inheritdoc />
public partial class _20261113152747_SessionsAndMfa : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "CurrentSessionId",
            table: "AspNetUsers");

        migrationBuilder.AddColumn<bool>(
            name: "NewLoginAlertsEnabled",
            table: "SystemSettings",
            type: "INTEGER",
            nullable: false,
            defaultValue: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "MfaEnabledAt",
            table: "AspNetUsers",
            type: "TEXT",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "UserSessions",
            columns: table => new
            {
                Id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                UserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                LastSeenAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                Persistent = table.Column<bool>(type: "INTEGER", nullable: false),
                Mfa = table.Column<bool>(type: "INTEGER", nullable: false),
                IpAddress = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                UserAgent = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                Browser = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                Os = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                DeviceType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                RevokedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                RevokedReason = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                RevokedBySessionId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserSessions", x => x.Id);
                table.ForeignKey(
                    name: "FK_UserSessions_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "UserConnections",
            columns: table => new
            {
                ConnectionId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                SessionId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                UserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                ConnectedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserConnections", x => x.ConnectionId);
                table.ForeignKey(
                    name: "FK_UserConnections_UserSessions_SessionId",
                    column: x => x.SessionId,
                    principalTable: "UserSessions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_UserConnections_SessionId",
            table: "UserConnections",
            column: "SessionId");

        migrationBuilder.CreateIndex(
            name: "IX_UserSessions_LastSeenAt",
            table: "UserSessions",
            column: "LastSeenAt");

        migrationBuilder.CreateIndex(
            name: "IX_UserSessions_UserId_RevokedAt",
            table: "UserSessions",
            columns: new[] { "UserId", "RevokedAt" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "UserConnections");

        migrationBuilder.DropTable(
            name: "UserSessions");

        migrationBuilder.DropColumn(
            name: "NewLoginAlertsEnabled",
            table: "SystemSettings");

        migrationBuilder.DropColumn(
            name: "MfaEnabledAt",
            table: "AspNetUsers");

        migrationBuilder.AddColumn<string>(
            name: "CurrentSessionId",
            table: "AspNetUsers",
            type: "TEXT",
            maxLength: 64,
            nullable: true);
    }
}
