using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorApp3.Data.Migrations;

/// <inheritdoc />
public partial class _20261117201928_ApiRefreshTokens : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "RefreshTokenExpiresAt",
            table: "UserSessions",
            type: "TEXT",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "RefreshTokenHash",
            table: "UserSessions",
            type: "TEXT",
            maxLength: 64,
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "RefreshTokenExpiresAt",
            table: "UserSessions");

        migrationBuilder.DropColumn(
            name: "RefreshTokenHash",
            table: "UserSessions");
    }
}
