using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutomatedMarketIntelligenceTool.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase4UserFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add DealerId to Listings table
            migrationBuilder.AddColumn<Guid>(
                name: "DealerId",
                table: "Listings",
                type: "TEXT",
                nullable: true);

            // Create Dealers table
            migrationBuilder.CreateTable(
                name: "Dealers",
                columns: table => new
                {
                    DealerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    NormalizedName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    City = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    State = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Phone = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    ListingCount = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    FirstSeenAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dealers", x => x.DealerId);
                });

            // Create WatchedListings table
            migrationBuilder.CreateTable(
                name: "WatchedListings",
                columns: table => new
                {
                    WatchedListingId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ListingId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    NotifyOnPriceChange = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    NotifyOnRemoval = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WatchedListings", x => x.WatchedListingId);
                    table.ForeignKey(
                        name: "FK_WatchedListings_Listings_ListingId",
                        column: x => x.ListingId,
                        principalTable: "Listings",
                        principalColumn: "ListingId",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create Alerts table
            migrationBuilder.CreateTable(
                name: "Alerts",
                columns: table => new
                {
                    AlertId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Criteria = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    NotificationMethod = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    NotificationTarget = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    LastTriggeredAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TriggerCount = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alerts", x => x.AlertId);
                });

            // Create AlertNotifications table
            migrationBuilder.CreateTable(
                name: "AlertNotifications",
                columns: table => new
                {
                    NotificationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AlertId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ListingId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SentAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertNotifications", x => x.NotificationId);
                    table.ForeignKey(
                        name: "FK_AlertNotifications_Alerts_AlertId",
                        column: x => x.AlertId,
                        principalTable: "Alerts",
                        principalColumn: "AlertId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AlertNotifications_Listings_ListingId",
                        column: x => x.ListingId,
                        principalTable: "Listings",
                        principalColumn: "ListingId",
                        onDelete: ReferentialAction.Restrict);
                });

            // Create foreign key for Listings.DealerId
            migrationBuilder.CreateIndex(
                name: "IX_Listing_DealerId",
                table: "Listings",
                column: "DealerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Listings_Dealers_DealerId",
                table: "Listings",
                column: "DealerId",
                principalTable: "Dealers",
                principalColumn: "DealerId",
                onDelete: ReferentialAction.Restrict);

            // Create indexes for Dealers
            migrationBuilder.CreateIndex(
                name: "IX_Dealer_TenantId",
                table: "Dealers",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Dealer_NormalizedName",
                table: "Dealers",
                column: "NormalizedName");

            migrationBuilder.CreateIndex(
                name: "IX_Dealer_TenantNormalizedName",
                table: "Dealers",
                columns: new[] { "TenantId", "NormalizedName" });

            // Create indexes for WatchedListings
            migrationBuilder.CreateIndex(
                name: "IX_WatchedListing_TenantId",
                table: "WatchedListings",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_WatchedListing_ListingId",
                table: "WatchedListings",
                column: "ListingId");

            migrationBuilder.CreateIndex(
                name: "UQ_WatchedListing_TenantListing",
                table: "WatchedListings",
                columns: new[] { "TenantId", "ListingId" },
                unique: true);

            // Create indexes for Alerts
            migrationBuilder.CreateIndex(
                name: "IX_Alert_TenantId",
                table: "Alerts",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Alert_IsActive",
                table: "Alerts",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Alert_TenantName",
                table: "Alerts",
                columns: new[] { "TenantId", "Name" });

            // Create indexes for AlertNotifications
            migrationBuilder.CreateIndex(
                name: "IX_AlertNotification_TenantId",
                table: "AlertNotifications",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertNotification_AlertId",
                table: "AlertNotifications",
                column: "AlertId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertNotification_SentAt",
                table: "AlertNotifications",
                column: "SentAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop foreign keys
            migrationBuilder.DropForeignKey(
                name: "FK_Listings_Dealers_DealerId",
                table: "Listings");

            // Drop tables
            migrationBuilder.DropTable(
                name: "AlertNotifications");

            migrationBuilder.DropTable(
                name: "Alerts");

            migrationBuilder.DropTable(
                name: "WatchedListings");

            migrationBuilder.DropTable(
                name: "Dealers");

            // Drop indexes
            migrationBuilder.DropIndex(
                name: "IX_Listing_DealerId",
                table: "Listings");

            // Drop columns
            migrationBuilder.DropColumn(
                name: "DealerId",
                table: "Listings");
        }
    }
}
