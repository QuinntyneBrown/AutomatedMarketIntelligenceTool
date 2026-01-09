using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutomatedMarketIntelligenceTool.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase2Infrastructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add new columns to Listings table
            migrationBuilder.AddColumn<int>(
                name: "Drivetrain",
                table: "Listings",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SellerType",
                table: "Listings",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SellerName",
                table: "Listings",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SellerPhone",
                table: "Listings",
                type: "TEXT",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ListingDate",
                table: "Listings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DaysOnMarket",
                table: "Listings",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Listings",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeactivatedAt",
                table: "Listings",
                type: "TEXT",
                nullable: true);

            // Change BodyStyle from string to enum (int)
            // Step 1: Add new column
            migrationBuilder.AddColumn<int>(
                name: "BodyStyle_New",
                table: "Listings",
                type: "INTEGER",
                nullable: true);

            // Step 2: Migrate data (done at application level if needed)
            // Step 3: Drop old column
            migrationBuilder.DropColumn(
                name: "BodyStyle",
                table: "Listings");

            // Step 4: Rename new column to original name
            migrationBuilder.RenameColumn(
                name: "BodyStyle_New",
                table: "Listings",
                newName: "BodyStyle");

            // Create PriceHistory table
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

            // Create SearchSessions table
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

            // Create new indexes for Listings
            migrationBuilder.CreateIndex(
                name: "IX_Listing_Condition",
                table: "Listings",
                column: "Condition");

            migrationBuilder.CreateIndex(
                name: "IX_Listing_Transmission",
                table: "Listings",
                column: "Transmission");

            migrationBuilder.CreateIndex(
                name: "IX_Listing_FuelType",
                table: "Listings",
                column: "FuelType");

            migrationBuilder.CreateIndex(
                name: "IX_Listing_BodyStyle",
                table: "Listings",
                column: "BodyStyle");

            migrationBuilder.CreateIndex(
                name: "IX_Listing_IsActive",
                table: "Listings",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Listing_FirstSeenDate",
                table: "Listings",
                column: "FirstSeenDate");

            // Create indexes for PriceHistory
            migrationBuilder.CreateIndex(
                name: "IX_PriceHistory_TenantId",
                table: "PriceHistory",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceHistory_ListingId",
                table: "PriceHistory",
                column: "ListingId");

            // Create indexes for SearchSessions
            migrationBuilder.CreateIndex(
                name: "IX_SearchSession_TenantId",
                table: "SearchSessions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SearchSession_StartTime",
                table: "SearchSessions",
                column: "StartTime");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop indexes for SearchSessions
            migrationBuilder.DropIndex(
                name: "IX_SearchSession_StartTime",
                table: "SearchSessions");

            migrationBuilder.DropIndex(
                name: "IX_SearchSession_TenantId",
                table: "SearchSessions");

            // Drop indexes for PriceHistory
            migrationBuilder.DropIndex(
                name: "IX_PriceHistory_ListingId",
                table: "PriceHistory");

            migrationBuilder.DropIndex(
                name: "IX_PriceHistory_TenantId",
                table: "PriceHistory");

            // Drop new indexes for Listings
            migrationBuilder.DropIndex(
                name: "IX_Listing_FirstSeenDate",
                table: "Listings");

            migrationBuilder.DropIndex(
                name: "IX_Listing_IsActive",
                table: "Listings");

            migrationBuilder.DropIndex(
                name: "IX_Listing_BodyStyle",
                table: "Listings");

            migrationBuilder.DropIndex(
                name: "IX_Listing_FuelType",
                table: "Listings");

            migrationBuilder.DropIndex(
                name: "IX_Listing_Transmission",
                table: "Listings");

            migrationBuilder.DropIndex(
                name: "IX_Listing_Condition",
                table: "Listings");

            // Drop tables
            migrationBuilder.DropTable(
                name: "SearchSessions");

            migrationBuilder.DropTable(
                name: "PriceHistory");

            // Revert BodyStyle to string
            migrationBuilder.RenameColumn(
                name: "BodyStyle",
                table: "Listings",
                newName: "BodyStyle_Old");

            migrationBuilder.AddColumn<string>(
                name: "BodyStyle",
                table: "Listings",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.DropColumn(
                name: "BodyStyle_Old",
                table: "Listings");

            // Drop new columns from Listings
            migrationBuilder.DropColumn(
                name: "DeactivatedAt",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "DaysOnMarket",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "ListingDate",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "SellerPhone",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "SellerName",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "SellerType",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "Drivetrain",
                table: "Listings");
        }
    }
}

