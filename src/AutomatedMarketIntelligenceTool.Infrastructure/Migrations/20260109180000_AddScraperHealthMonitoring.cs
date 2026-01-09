using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutomatedMarketIntelligenceTool.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddScraperHealthMonitoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ScraperHealth",
                columns: table => new
                {
                    ScraperHealthRecordId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SiteName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SuccessRate = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                    ListingsFound = table.Column<int>(type: "INTEGER", nullable: false),
                    ErrorCount = table.Column<int>(type: "INTEGER", nullable: false),
                    AverageResponseTime = table.Column<long>(type: "INTEGER", nullable: false),
                    LastError = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    MissingElementCount = table.Column<int>(type: "INTEGER", nullable: false),
                    MissingElementsJson = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScraperHealth", x => x.ScraperHealthRecordId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Health_Site",
                table: "ScraperHealth",
                column: "SiteName");

            migrationBuilder.CreateIndex(
                name: "IX_Health_RecordedAt",
                table: "ScraperHealth",
                column: "RecordedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScraperHealth");
        }
    }
}
