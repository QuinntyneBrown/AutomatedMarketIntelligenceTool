using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutomatedMarketIntelligenceTool.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase5_CacheAndPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create ResponseCache table for Phase 5 caching
            migrationBuilder.CreateTable(
                name: "ResponseCache",
                columns: table => new
                {
                    CacheEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CacheKey = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Url = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ResponseData = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CachedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    HitCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    ResponseSizeBytes = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResponseCache", x => x.CacheEntryId);
                });

            // Create indexes for ResponseCache
            migrationBuilder.CreateIndex(
                name: "UQ_ResponseCache_CacheKey",
                table: "ResponseCache",
                column: "CacheKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResponseCache_ExpiresAt",
                table: "ResponseCache",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_ResponseCache_Url",
                table: "ResponseCache",
                column: "Url");

            // Phase 5 Performance Indexes for Listings table
            // AC-010.1: VIN lookup uses indexed query (sub-100ms response)
            // Note: VIN index already exists in initial migration

            // AC-010.2: Fuzzy matching optimized with blocking/bucketing strategy
            // Covering index for fuzzy matching candidate lookup
            // This supports queries filtering by Make, Year, and Price with included columns
            migrationBuilder.CreateIndex(
                name: "IX_Listing_FuzzyCandidate",
                table: "Listings",
                columns: new[] { "Make", "Year", "Price", "TenantId" });

            // Additional index for time-based queries with deduplication
            migrationBuilder.CreateIndex(
                name: "IX_Listing_CreatedAt_TenantId",
                table: "Listings",
                columns: new[] { "CreatedAt", "TenantId" });

            // Composite index for make/model/year queries - commonly used in fuzzy matching
            migrationBuilder.CreateIndex(
                name: "IX_Listing_Make_Model_Year_TenantId",
                table: "Listings",
                columns: new[] { "Make", "Model", "Year", "TenantId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop performance indexes
            migrationBuilder.DropIndex(
                name: "IX_Listing_Make_Model_Year_TenantId",
                table: "Listings");

            migrationBuilder.DropIndex(
                name: "IX_Listing_CreatedAt_TenantId",
                table: "Listings");

            migrationBuilder.DropIndex(
                name: "IX_Listing_FuzzyCandidate",
                table: "Listings");

            // Drop ResponseCache table and indexes
            migrationBuilder.DropTable(
                name: "ResponseCache");
        }
    }
}
