using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutomatedMarketIntelligenceTool.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Listings",
                columns: table => new
                {
                    ListingId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ExternalId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SourceSite = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ListingUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Make = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Model = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Year = table.Column<int>(type: "INTEGER", nullable: false),
                    Trim = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Price = table.Column<decimal>(type: "TEXT", precision: 12, scale: 2, nullable: false),
                    Mileage = table.Column<int>(type: "INTEGER", nullable: true),
                    Vin = table.Column<string>(type: "TEXT", maxLength: 17, nullable: true),
                    City = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Province = table.Column<string>(type: "TEXT", maxLength: 2, nullable: true),
                    PostalCode = table.Column<string>(type: "TEXT", maxLength: 7, nullable: true),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false, defaultValue: "CAD"),
                    Condition = table.Column<int>(type: "INTEGER", nullable: false),
                    Transmission = table.Column<int>(type: "INTEGER", nullable: true),
                    FuelType = table.Column<int>(type: "INTEGER", nullable: true),
                    BodyStyle = table.Column<int>(type: "INTEGER", nullable: true),
                    Drivetrain = table.Column<int>(type: "INTEGER", nullable: true),
                    ExteriorColor = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    InteriorColor = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    SellerType = table.Column<int>(type: "INTEGER", nullable: true),
                    SellerName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    SellerPhone = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    ImageUrls = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    ImageHashes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    ListingDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DaysOnMarket = table.Column<int>(type: "INTEGER", nullable: true),
                    FirstSeenDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastSeenDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsNewListing = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    DeactivatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RelistedCount = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    PreviousListingId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LinkedVehicleId = table.Column<Guid>(type: "TEXT", nullable: true),
                    MatchConfidence = table.Column<decimal>(type: "TEXT", nullable: true),
                    MatchMethod = table.Column<string>(type: "TEXT", nullable: true),
                    Latitude = table.Column<decimal>(type: "TEXT", nullable: true),
                    Longitude = table.Column<decimal>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Listings", x => x.ListingId);
                });

            migrationBuilder.CreateTable(
                name: "PriceHistory",
                columns: table => new
                {
                    PriceHistoryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ListingId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Price = table.Column<decimal>(type: "TEXT", precision: 12, scale: 2, nullable: false),
                    ObservedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PriceChange = table.Column<decimal>(type: "TEXT", precision: 12, scale: 2, nullable: true),
                    ChangePercentage = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceHistory", x => x.PriceHistoryId);
                });

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

            migrationBuilder.CreateTable(
                name: "SearchProfiles",
                columns: table => new
                {
                    SearchProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    SearchParametersJson = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastUsedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SearchProfiles", x => x.SearchProfileId);
                });

            migrationBuilder.CreateTable(
                name: "SearchSessions",
                columns: table => new
                {
                    SearchSessionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StartTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    SearchParameters = table.Column<string>(type: "TEXT", nullable: false),
                    TotalListingsFound = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    NewListingsCount = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    PriceChangesCount = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    ErrorMessage = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SearchSessions", x => x.SearchSessionId);
                });

            migrationBuilder.CreateTable(
                name: "Vehicles",
                columns: table => new
                {
                    VehicleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PrimaryVin = table.Column<string>(type: "TEXT", maxLength: 17, nullable: true),
                    Make = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Model = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Year = table.Column<int>(type: "INTEGER", nullable: false),
                    Trim = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    BestPrice = table.Column<decimal>(type: "decimal(12,2)", nullable: true),
                    AveragePrice = table.Column<decimal>(type: "decimal(12,2)", nullable: true),
                    ListingCount = table.Column<int>(type: "INTEGER", nullable: false),
                    FirstSeenDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastSeenDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vehicles", x => x.VehicleId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Listing_BodyStyle",
                table: "Listings",
                column: "BodyStyle");

            migrationBuilder.CreateIndex(
                name: "IX_Listing_Condition",
                table: "Listings",
                column: "Condition");

            migrationBuilder.CreateIndex(
                name: "IX_Listing_FirstSeenDate",
                table: "Listings",
                column: "FirstSeenDate");

            migrationBuilder.CreateIndex(
                name: "IX_Listing_FuelType",
                table: "Listings",
                column: "FuelType");

            migrationBuilder.CreateIndex(
                name: "IX_Listing_IsActive",
                table: "Listings",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Listing_MakeModel",
                table: "Listings",
                columns: new[] { "Make", "Model" });

            migrationBuilder.CreateIndex(
                name: "IX_Listing_Price",
                table: "Listings",
                column: "Price");

            migrationBuilder.CreateIndex(
                name: "IX_Listing_RelistedCount",
                table: "Listings",
                column: "RelistedCount",
                filter: "[RelistedCount] > 0");

            migrationBuilder.CreateIndex(
                name: "IX_Listing_TenantId",
                table: "Listings",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Listing_Transmission",
                table: "Listings",
                column: "Transmission");

            migrationBuilder.CreateIndex(
                name: "IX_Listing_Vin",
                table: "Listings",
                column: "Vin");

            migrationBuilder.CreateIndex(
                name: "IX_Listing_Year",
                table: "Listings",
                column: "Year");

            migrationBuilder.CreateIndex(
                name: "UQ_Listing_External",
                table: "Listings",
                columns: new[] { "SourceSite", "ExternalId", "TenantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PriceHistory_ListingId",
                table: "PriceHistory",
                column: "ListingId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceHistory_TenantId",
                table: "PriceHistory",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Review_Listings",
                table: "ReviewQueue",
                columns: new[] { "Listing1Id", "Listing2Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Review_Status",
                table: "ReviewQueue",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Review_TenantId",
                table: "ReviewQueue",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SearchProfiles_TenantId",
                table: "SearchProfiles",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SearchProfiles_TenantId_Name",
                table: "SearchProfiles",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SearchSession_StartTime",
                table: "SearchSessions",
                column: "StartTime");

            migrationBuilder.CreateIndex(
                name: "IX_SearchSession_TenantId",
                table: "SearchSessions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_Make_Model_Year",
                table: "Vehicles",
                columns: new[] { "Make", "Model", "Year" });

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_PrimaryVin",
                table: "Vehicles",
                column: "PrimaryVin");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_TenantId",
                table: "Vehicles",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Listings");

            migrationBuilder.DropTable(
                name: "PriceHistory");

            migrationBuilder.DropTable(
                name: "ReviewQueue");

            migrationBuilder.DropTable(
                name: "SearchProfiles");

            migrationBuilder.DropTable(
                name: "SearchSessions");

            migrationBuilder.DropTable(
                name: "Vehicles");
        }
    }
}
