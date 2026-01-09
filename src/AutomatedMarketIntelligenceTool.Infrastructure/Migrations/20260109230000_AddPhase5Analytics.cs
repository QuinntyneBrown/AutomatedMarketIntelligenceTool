using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutomatedMarketIntelligenceTool.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPhase5Analytics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add new columns to Dealers table for reliability metrics
            migrationBuilder.AddColumn<decimal>(
                name: "ReliabilityScore",
                table: "Dealers",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AvgDaysOnMarket",
                table: "Dealers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalListingsHistorical",
                table: "Dealers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "FrequentRelisterFlag",
                table: "Dealers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastAnalyzedAt",
                table: "Dealers",
                type: "datetime2",
                nullable: true);

            // Create DealerMetrics table
            migrationBuilder.CreateTable(
                name: "DealerMetrics",
                columns: table => new
                {
                    DealerMetricsId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DealerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReliabilityScore = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    TotalListingsHistorical = table.Column<int>(type: "int", nullable: false),
                    ActiveListingsCount = table.Column<int>(type: "int", nullable: false),
                    SoldListingsCount = table.Column<int>(type: "int", nullable: false),
                    ExpiredListingsCount = table.Column<int>(type: "int", nullable: false),
                    AverageDaysOnMarket = table.Column<double>(type: "float", nullable: false),
                    MedianDaysOnMarket = table.Column<double>(type: "float", nullable: false),
                    MinDaysOnMarket = table.Column<int>(type: "int", nullable: false),
                    MaxDaysOnMarket = table.Column<int>(type: "int", nullable: false),
                    AverageListingPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AveragePriceReduction = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AveragePriceReductionPercent = table.Column<double>(type: "float", nullable: false),
                    PriceReductionCount = table.Column<int>(type: "int", nullable: false),
                    RelistingCount = table.Column<int>(type: "int", nullable: false),
                    RelistingRate = table.Column<double>(type: "float", nullable: false),
                    IsFrequentRelister = table.Column<bool>(type: "bit", nullable: false),
                    VinProvidedRate = table.Column<double>(type: "float", nullable: false),
                    ImageProvidedRate = table.Column<double>(type: "float", nullable: false),
                    DescriptionQualityScore = table.Column<double>(type: "float", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastAnalyzedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NextScheduledAnalysis = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DealerMetrics", x => x.DealerMetricsId);
                    table.ForeignKey(
                        name: "FK_DealerMetrics_Dealers_DealerId",
                        column: x => x.DealerId,
                        principalTable: "Dealers",
                        principalColumn: "DealerId",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create RelistingPatterns table
            migrationBuilder.CreateTable(
                name: "RelistingPatterns",
                columns: table => new
                {
                    RelistingPatternId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrentListingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PreviousListingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DealerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MatchConfidence = table.Column<double>(type: "float", nullable: false),
                    MatchMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PreviousPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CurrentPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PriceChange = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PriceChangePercent = table.Column<double>(type: "float", nullable: true),
                    PreviousDeactivatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CurrentListedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DaysBetweenListings = table.Column<int>(type: "int", nullable: false),
                    PreviousDaysOnMarket = table.Column<int>(type: "int", nullable: false),
                    Vin = table.Column<string>(type: "nvarchar(17)", maxLength: 17, nullable: true),
                    Make = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Model = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Year = table.Column<int>(type: "int", nullable: true),
                    IsSuspiciousPattern = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    SuspiciousReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DetectedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RelistingPatterns", x => x.RelistingPatternId);
                });

            // Create DealerDeduplicationRules table
            migrationBuilder.CreateTable(
                name: "DealerDeduplicationRules",
                columns: table => new
                {
                    DealerDeduplicationRuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DealerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RuleName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Priority = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    AutoMatchThreshold = table.Column<double>(type: "float", nullable: true),
                    ReviewThreshold = table.Column<double>(type: "float", nullable: true),
                    MakeModelWeight = table.Column<double>(type: "float", nullable: true),
                    YearWeight = table.Column<double>(type: "float", nullable: true),
                    MileageWeight = table.Column<double>(type: "float", nullable: true),
                    PriceWeight = table.Column<double>(type: "float", nullable: true),
                    LocationWeight = table.Column<double>(type: "float", nullable: true),
                    ImageWeight = table.Column<double>(type: "float", nullable: true),
                    MileageTolerance = table.Column<int>(type: "int", nullable: true),
                    PriceTolerance = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    YearTolerance = table.Column<int>(type: "int", nullable: true),
                    EnableVinMatching = table.Column<bool>(type: "bit", nullable: true),
                    EnableFuzzyMatching = table.Column<bool>(type: "bit", nullable: true),
                    EnableImageMatching = table.Column<bool>(type: "bit", nullable: true),
                    StrictModeEnabled = table.Column<bool>(type: "bit", nullable: true),
                    Condition = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MinPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MaxPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MinYear = table.Column<int>(type: "int", nullable: true),
                    MaxYear = table.Column<int>(type: "int", nullable: true),
                    MakeFilter = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ModelFilter = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TimesApplied = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    LastAppliedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DealerDeduplicationRules", x => x.DealerDeduplicationRuleId);
                });

            // Create InventorySnapshots table
            migrationBuilder.CreateTable(
                name: "InventorySnapshots",
                columns: table => new
                {
                    InventorySnapshotId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DealerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SnapshotDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TotalListings = table.Column<int>(type: "int", nullable: false),
                    NewListingsAdded = table.Column<int>(type: "int", nullable: false),
                    ListingsRemoved = table.Column<int>(type: "int", nullable: false),
                    ListingsSold = table.Column<int>(type: "int", nullable: false),
                    ListingsExpired = table.Column<int>(type: "int", nullable: false),
                    ListingsRelisted = table.Column<int>(type: "int", nullable: false),
                    TotalInventoryValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AverageListingPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MedianListingPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MinListingPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MaxListingPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PriceIncreasesCount = table.Column<int>(type: "int", nullable: false),
                    PriceDecreasesCount = table.Column<int>(type: "int", nullable: false),
                    TotalPriceReduction = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AveragePriceChange = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ListingsUnder30Days = table.Column<int>(type: "int", nullable: false),
                    Listings30To60Days = table.Column<int>(type: "int", nullable: false),
                    Listings60To90Days = table.Column<int>(type: "int", nullable: false),
                    ListingsOver90Days = table.Column<int>(type: "int", nullable: false),
                    AverageDaysOnMarket = table.Column<double>(type: "float", nullable: false),
                    CountByMake = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CountByYear = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CountByCondition = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InventoryChangeFromPrevious = table.Column<int>(type: "int", nullable: true),
                    InventoryChangePercentFromPrevious = table.Column<double>(type: "float", nullable: true),
                    ValueChangeFromPrevious = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ValueChangePercentFromPrevious = table.Column<double>(type: "float", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventorySnapshots", x => x.InventorySnapshotId);
                });

            // Create indexes for DealerMetrics
            migrationBuilder.CreateIndex(
                name: "IX_DealerMetrics_TenantId",
                table: "DealerMetrics",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_DealerMetrics_DealerId",
                table: "DealerMetrics",
                column: "DealerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DealerMetrics_TenantReliability",
                table: "DealerMetrics",
                columns: new[] { "TenantId", "ReliabilityScore" });

            migrationBuilder.CreateIndex(
                name: "IX_DealerMetrics_TenantRelister",
                table: "DealerMetrics",
                columns: new[] { "TenantId", "IsFrequentRelister" });

            migrationBuilder.CreateIndex(
                name: "IX_DealerMetrics_LastAnalyzed",
                table: "DealerMetrics",
                column: "LastAnalyzedAt");

            // Create indexes for RelistingPatterns
            migrationBuilder.CreateIndex(
                name: "IX_RelistingPattern_TenantId",
                table: "RelistingPatterns",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_RelistingPattern_DealerId",
                table: "RelistingPatterns",
                column: "DealerId");

            migrationBuilder.CreateIndex(
                name: "IX_RelistingPattern_CurrentListing",
                table: "RelistingPatterns",
                column: "CurrentListingId");

            migrationBuilder.CreateIndex(
                name: "IX_RelistingPattern_PreviousListing",
                table: "RelistingPatterns",
                column: "PreviousListingId");

            migrationBuilder.CreateIndex(
                name: "IX_RelistingPattern_TenantSuspicious",
                table: "RelistingPatterns",
                columns: new[] { "TenantId", "IsSuspiciousPattern" });

            migrationBuilder.CreateIndex(
                name: "IX_RelistingPattern_DetectedAt",
                table: "RelistingPatterns",
                column: "DetectedAt");

            migrationBuilder.CreateIndex(
                name: "IX_RelistingPattern_Type",
                table: "RelistingPatterns",
                column: "Type");

            // Create indexes for DealerDeduplicationRules
            migrationBuilder.CreateIndex(
                name: "IX_DealerDedupRule_TenantId",
                table: "DealerDeduplicationRules",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_DealerDedupRule_DealerId",
                table: "DealerDeduplicationRules",
                column: "DealerId");

            migrationBuilder.CreateIndex(
                name: "IX_DealerDedupRule_TenantDealerActive",
                table: "DealerDeduplicationRules",
                columns: new[] { "TenantId", "DealerId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_DealerDedupRule_TenantPriority",
                table: "DealerDeduplicationRules",
                columns: new[] { "TenantId", "Priority" });

            migrationBuilder.CreateIndex(
                name: "IX_DealerDedupRule_IsActive",
                table: "DealerDeduplicationRules",
                column: "IsActive");

            // Create indexes for InventorySnapshots
            migrationBuilder.CreateIndex(
                name: "IX_InventorySnapshot_TenantId",
                table: "InventorySnapshots",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_InventorySnapshot_DealerId",
                table: "InventorySnapshots",
                column: "DealerId");

            migrationBuilder.CreateIndex(
                name: "IX_InventorySnapshot_TenantDealerDate",
                table: "InventorySnapshots",
                columns: new[] { "TenantId", "DealerId", "SnapshotDate" });

            migrationBuilder.CreateIndex(
                name: "IX_InventorySnapshot_TenantPeriodDate",
                table: "InventorySnapshots",
                columns: new[] { "TenantId", "PeriodType", "SnapshotDate" });

            migrationBuilder.CreateIndex(
                name: "IX_InventorySnapshot_Date",
                table: "InventorySnapshots",
                column: "SnapshotDate");

            migrationBuilder.CreateIndex(
                name: "UX_InventorySnapshot_DealerDatePeriod",
                table: "InventorySnapshots",
                columns: new[] { "TenantId", "DealerId", "SnapshotDate", "PeriodType" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventorySnapshots");

            migrationBuilder.DropTable(
                name: "DealerDeduplicationRules");

            migrationBuilder.DropTable(
                name: "RelistingPatterns");

            migrationBuilder.DropTable(
                name: "DealerMetrics");

            migrationBuilder.DropColumn(
                name: "ReliabilityScore",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "AvgDaysOnMarket",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "TotalListingsHistorical",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "FrequentRelisterFlag",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "LastAnalyzedAt",
                table: "Dealers");
        }
    }
}
