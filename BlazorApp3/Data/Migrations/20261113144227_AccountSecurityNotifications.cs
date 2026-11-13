using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorApp3.Data.Migrations;

/// <inheritdoc />
public partial class _20261113144227_AccountSecurityNotifications : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // LockoutEnd passa de TEXT (DateTimeOffset) para INTEGER (DateTimeOffsetToBinaryConverter).
        // Bloqueios por tentativas em andamento são temporários: limpar evita valores TEXT na coluna nova.
        migrationBuilder.Sql("UPDATE AspNetUsers SET LockoutEnd = NULL;");

        migrationBuilder.AlterColumn<long>(
            name: "LockoutEnd",
            table: "AspNetUsers",
            type: "INTEGER",
            nullable: true,
            oldClrType: typeof(DateTimeOffset),
            oldType: "TEXT",
            oldNullable: true);

        migrationBuilder.AddColumn<string>(
            name: "BlockReason",
            table: "AspNetUsers",
            type: "TEXT",
            maxLength: 300,
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "BlockedAt",
            table: "AspNetUsers",
            type: "TEXT",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "BlockedBy",
            table: "AspNetUsers",
            type: "TEXT",
            maxLength: 150,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "CurrentSessionId",
            table: "AspNetUsers",
            type: "TEXT",
            maxLength: 64,
            nullable: true);

        migrationBuilder.CreateTable(
            name: "EmailConfirmationCodes",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                UserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                CodeHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                Attempts = table.Column<int>(type: "INTEGER", nullable: false),
                ConsumedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_EmailConfirmationCodes", x => x.Id);
                table.ForeignKey(
                    name: "FK_EmailConfirmationCodes_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Notifications",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                UserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                Title = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                Message = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                Icon = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                Level = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                Link = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                ReadAt = table.Column<DateTime>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Notifications", x => x.Id);
                table.ForeignKey(
                    name: "FK_Notifications_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "SystemSettings",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false),
                RegistrationEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                RequireEmailConfirmation = table.Column<bool>(type: "INTEGER", nullable: false),
                NotifyAdminsOnRegistration = table.Column<bool>(type: "INTEGER", nullable: false),
                SingleSessionEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                SecurityAlertsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                InAppNotificationsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                EmailNotificationsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SystemSettings", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_EmailConfirmationCodes_ExpiresAt",
            table: "EmailConfirmationCodes",
            column: "ExpiresAt");

        migrationBuilder.CreateIndex(
            name: "IX_EmailConfirmationCodes_UserId_CreatedAt",
            table: "EmailConfirmationCodes",
            columns: new[] { "UserId", "CreatedAt" });

        migrationBuilder.CreateIndex(
            name: "IX_Notifications_UserId_CreatedAt",
            table: "Notifications",
            columns: new[] { "UserId", "CreatedAt" });

        migrationBuilder.CreateIndex(
            name: "IX_Notifications_UserId_ReadAt",
            table: "Notifications",
            columns: new[] { "UserId", "ReadAt" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("UPDATE AspNetUsers SET LockoutEnd = NULL;");

        migrationBuilder.DropTable(
            name: "EmailConfirmationCodes");

        migrationBuilder.DropTable(
            name: "Notifications");

        migrationBuilder.DropTable(
            name: "SystemSettings");

        migrationBuilder.DropColumn(
            name: "BlockReason",
            table: "AspNetUsers");

        migrationBuilder.DropColumn(
            name: "BlockedAt",
            table: "AspNetUsers");

        migrationBuilder.DropColumn(
            name: "BlockedBy",
            table: "AspNetUsers");

        migrationBuilder.DropColumn(
            name: "CurrentSessionId",
            table: "AspNetUsers");

        migrationBuilder.AlterColumn<DateTimeOffset>(
            name: "LockoutEnd",
            table: "AspNetUsers",
            type: "TEXT",
            nullable: true,
            oldClrType: typeof(long),
            oldType: "INTEGER",
            oldNullable: true);
    }
}
