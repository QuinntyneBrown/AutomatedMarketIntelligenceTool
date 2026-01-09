using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutomatedMarketIntelligenceTool.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase4ImageAnalysisReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add new columns to Listings table for Phase 4
            migrationBuilder.AddColumn<string>(
                name: "ImageHashes",
                table: "Listings",
                type: "TEXT",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RelistedCount",
                table: "Listings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "PreviousListingId",
                table: "Listings",
                type: "TEXT",
                nullable: true);

            // Create ReviewQueue table
            migrationBuilder.CreateTable(
                name: "ReviewQueue",
                columns: table => new
                {
                    ReviewItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Listing1Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Listing2Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ConfidenceScore = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                    MatchMethod = table.Column<int>(type: "INTEGER", nullable: false),
                    FieldScores = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    Resolution = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    ResolvedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ResolvedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReviewQueue", x => x.ReviewItemId);
                });

            // Create indexes for ReviewQueue
            migrationBuilder.CreateIndex(
                name: "IX_Review_TenantId",
                table: "ReviewQueue",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Review_Status",
                table: "ReviewQueue",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Review_Listings",
                table: "ReviewQueue",
                columns: new[] { "Listing1Id", "Listing2Id" });

            // Create index for relisted listings
            migrationBuilder.CreateIndex(
                name: "IX_Listing_RelistedCount",
                table: "Listings",
                column: "RelistedCount",
                filter: "[RelistedCount] > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop index for relisted listings
            migrationBuilder.DropIndex(
                name: "IX_Listing_RelistedCount",
                table: "Listings");

            // Drop indexes for ReviewQueue
            migrationBuilder.DropIndex(
                name: "IX_Review_Listings",
                table: "ReviewQueue");

            migrationBuilder.DropIndex(
                name: "IX_Review_Status",
                table: "ReviewQueue");

            migrationBuilder.DropIndex(
                name: "IX_Review_TenantId",
                table: "ReviewQueue");

            // Drop ReviewQueue table
            migrationBuilder.DropTable(
                name: "ReviewQueue");

            // Drop new columns from Listings
            migrationBuilder.DropColumn(
                name: "PreviousListingId",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "RelistedCount",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "ImageHashes",
                table: "Listings");
        }
    }
}
