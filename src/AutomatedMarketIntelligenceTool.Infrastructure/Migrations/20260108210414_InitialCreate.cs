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
                    State = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ZipCode = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    Condition = table.Column<int>(type: "INTEGER", nullable: false),
                    Transmission = table.Column<int>(type: "INTEGER", nullable: true),
                    FuelType = table.Column<int>(type: "INTEGER", nullable: true),
                    BodyStyle = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ExteriorColor = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    InteriorColor = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    ImageUrls = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    FirstSeenDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastSeenDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsNewListing = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Listings", x => x.ListingId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Listing_MakeModel",
                table: "Listings",
                columns: new[] { "Make", "Model" });

            migrationBuilder.CreateIndex(
                name: "IX_Listing_Price",
                table: "Listings",
                column: "Price");

            migrationBuilder.CreateIndex(
                name: "IX_Listing_TenantId",
                table: "Listings",
                column: "TenantId");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Listings");
        }
    }
}
