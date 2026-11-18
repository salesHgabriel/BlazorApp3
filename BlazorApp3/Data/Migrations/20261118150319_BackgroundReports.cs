using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorApp3.Data.Migrations;

/// <inheritdoc />
public partial class _20261118150319_BackgroundReports : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ReportExports",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                UserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                Source = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                Title = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                Format = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                EstimatedRows = table.Column<int>(type: "INTEGER", nullable: false),
                RowCount = table.Column<int>(type: "INTEGER", nullable: true),
                FileName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                ContentType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                StorageName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                SizeBytes = table.Column<long>(type: "INTEGER", nullable: true),
                Error = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ReportExports", x => x.Id);
                table.ForeignKey(
                    name: "FK_ReportExports_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ReportExports_ExpiresAt",
            table: "ReportExports",
            column: "ExpiresAt");

        migrationBuilder.CreateIndex(
            name: "IX_ReportExports_UserId_CreatedAt",
            table: "ReportExports",
            columns: new[] { "UserId", "CreatedAt" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "ReportExports");
    }
}
