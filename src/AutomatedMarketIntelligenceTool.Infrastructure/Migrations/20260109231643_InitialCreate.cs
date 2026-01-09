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

            migrationBuilder.CreateTable(
                name: "CustomMarkets",
                columns: table => new
                {
                    CustomMarketId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    PostalCodes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    Provinces = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CenterLatitude = table.Column<double>(type: "REAL", nullable: true),
                    CenterLongitude = table.Column<double>(type: "REAL", nullable: true),
                    RadiusKm = table.Column<int>(type: "INTEGER", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    ConfigurationJson = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomMarkets", x => x.CustomMarketId);
                });

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

            migrationBuilder.CreateTable(
                name: "DeduplicationAudit",
                columns: table => new
                {
                    AuditEntryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Listing1Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Listing2Id = table.Column<Guid>(type: "TEXT", nullable: true),
                    Decision = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ConfidenceScore = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: true),
                    WasAutomatic = table.Column<bool>(type: "INTEGER", nullable: false),
                    ManualOverride = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    OverrideReason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    OriginalAuditEntryId = table.Column<Guid>(type: "TEXT", nullable: true),
                    IsFalsePositive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    IsFalseNegative = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    FuzzyMatchDetails = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeduplicationAudit", x => x.AuditEntryId);
                });

            migrationBuilder.CreateTable(
                name: "DeduplicationConfig",
                columns: table => new
                {
                    ConfigId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ConfigKey = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ConfigValue = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeduplicationConfig", x => x.ConfigId);
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
                name: "Reports",
                columns: table => new
                {
                    ReportId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Format = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    SearchCriteria = table.Column<string>(type: "TEXT", nullable: true),
                    FilePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    FileSize = table.Column<long>(type: "INTEGER", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ErrorMessage = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reports", x => x.ReportId);
                });

            migrationBuilder.CreateTable(
                name: "ResourceThrottles",
                columns: table => new
                {
                    ResourceThrottleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ResourceType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    MaxValue = table.Column<int>(type: "INTEGER", nullable: false),
                    TimeWindow = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CurrentUsage = table.Column<int>(type: "INTEGER", nullable: false),
                    WindowStartTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    Action = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    WarningThresholdPercent = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceThrottles", x => x.ResourceThrottleId);
                });

            migrationBuilder.CreateTable(
                name: "ResponseCache",
                columns: table => new
                {
                    CacheEntryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CacheKey = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Url = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    ResponseData = table.Column<byte[]>(type: "BLOB", nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CachedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    HitCount = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    ResponseSizeBytes = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResponseCache", x => x.CacheEntryId);
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
                name: "ScheduledReports",
                columns: table => new
                {
                    ScheduledReportId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Format = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Schedule = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ScheduledTime = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    ScheduledDayOfWeek = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    ScheduledDayOfMonth = table.Column<int>(type: "INTEGER", nullable: true),
                    SearchCriteriaJson = table.Column<string>(type: "TEXT", nullable: true),
                    CustomMarketId = table.Column<Guid>(type: "TEXT", nullable: true),
                    OutputDirectory = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    FilenameTemplate = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    LastRunAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    NextRunAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SuccessfulRuns = table.Column<int>(type: "INTEGER", nullable: false),
                    FailedRuns = table.Column<int>(type: "INTEGER", nullable: false),
                    LastErrorMessage = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    EmailRecipients = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    IncludeStatistics = table.Column<bool>(type: "INTEGER", nullable: false),
                    IncludePriceTrends = table.Column<bool>(type: "INTEGER", nullable: false),
                    MaxListings = table.Column<int>(type: "INTEGER", nullable: false),
                    RetentionCount = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledReports", x => x.ScheduledReportId);
                });

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
                    Longitude = table.Column<decimal>(type: "TEXT", nullable: true),
                    DealerId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Listings", x => x.ListingId);
                    table.ForeignKey(
                        name: "FK_Listings_Dealers_DealerId",
                        column: x => x.DealerId,
                        principalTable: "Dealers",
                        principalColumn: "DealerId",
                        onDelete: ReferentialAction.Restrict);
                });

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

            migrationBuilder.CreateIndex(
                name: "IX_AlertNotification_AlertId",
                table: "AlertNotifications",
                column: "AlertId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertNotification_SentAt",
                table: "AlertNotifications",
                column: "SentAt");

            migrationBuilder.CreateIndex(
                name: "IX_AlertNotification_TenantId",
                table: "AlertNotifications",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertNotifications_ListingId",
                table: "AlertNotifications",
                column: "ListingId");

            migrationBuilder.CreateIndex(
                name: "IX_Alert_IsActive",
                table: "Alerts",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Alert_TenantId",
                table: "Alerts",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Alert_TenantName",
                table: "Alerts",
                columns: new[] { "TenantId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomMarket_Active",
                table: "CustomMarkets",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomMarket_TenantId",
                table: "CustomMarkets",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "UQ_CustomMarket_Name",
                table: "CustomMarkets",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Dealer_NormalizedName",
                table: "Dealers",
                column: "NormalizedName");

            migrationBuilder.CreateIndex(
                name: "IX_Dealer_TenantId",
                table: "Dealers",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Dealer_TenantNormalizedName",
                table: "Dealers",
                columns: new[] { "TenantId", "NormalizedName" });

            migrationBuilder.CreateIndex(
                name: "IX_Audit_CreatedAt",
                table: "DeduplicationAudit",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Audit_Decision",
                table: "DeduplicationAudit",
                column: "Decision");

            migrationBuilder.CreateIndex(
                name: "IX_Audit_FalsePositiveNegative",
                table: "DeduplicationAudit",
                columns: new[] { "TenantId", "IsFalsePositive", "IsFalseNegative" });

            migrationBuilder.CreateIndex(
                name: "IX_Audit_Listing1Id",
                table: "DeduplicationAudit",
                column: "Listing1Id");

            migrationBuilder.CreateIndex(
                name: "IX_Audit_TenantId",
                table: "DeduplicationAudit",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Audit_TenantId_CreatedAt",
                table: "DeduplicationAudit",
                columns: new[] { "TenantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DedupConfig_TenantId",
                table: "DeduplicationConfig",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "UQ_DedupConfig_Key",
                table: "DeduplicationConfig",
                columns: new[] { "TenantId", "ConfigKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Listing_BatchDedup",
                table: "Listings",
                columns: new[] { "TenantId", "IsActive", "Make", "Year" });

            migrationBuilder.CreateIndex(
                name: "IX_Listing_BodyStyle",
                table: "Listings",
                column: "BodyStyle");

            migrationBuilder.CreateIndex(
                name: "IX_Listing_Condition",
                table: "Listings",
                column: "Condition");

            migrationBuilder.CreateIndex(
                name: "IX_Listing_CreatedAt_TenantId",
                table: "Listings",
                columns: new[] { "CreatedAt", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_Listing_FirstSeenDate",
                table: "Listings",
                column: "FirstSeenDate");

            migrationBuilder.CreateIndex(
                name: "IX_Listing_FuelType",
                table: "Listings",
                column: "FuelType");

            migrationBuilder.CreateIndex(
                name: "IX_Listing_FuzzyCandidate",
                table: "Listings",
                columns: new[] { "Make", "Year", "Price", "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Listing_IsActive",
                table: "Listings",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Listing_Make_Model_Year_TenantId",
                table: "Listings",
                columns: new[] { "Make", "Model", "Year", "TenantId" });

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
                name: "IX_Listing_Vin_TenantId",
                table: "Listings",
                columns: new[] { "Vin", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_Listing_Year",
                table: "Listings",
                column: "Year");

            migrationBuilder.CreateIndex(
                name: "IX_Listings_DealerId",
                table: "Listings",
                column: "DealerId");

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
                name: "IX_Report_CreatedAt",
                table: "Reports",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Report_Status",
                table: "Reports",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Report_TenantId",
                table: "Reports",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceThrottle_Enabled",
                table: "ResourceThrottles",
                columns: new[] { "TenantId", "IsEnabled" });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceThrottle_TenantId",
                table: "ResourceThrottles",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "UQ_ResourceThrottle_Type",
                table: "ResourceThrottles",
                columns: new[] { "TenantId", "ResourceType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResponseCache_ExpiresAt",
                table: "ResponseCache",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_ResponseCache_Url",
                table: "ResponseCache",
                column: "Url");

            migrationBuilder.CreateIndex(
                name: "UQ_ResponseCache_CacheKey",
                table: "ResponseCache",
                column: "CacheKey",
                unique: true);

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
                name: "IX_ScheduledReport_DueReports",
                table: "ScheduledReports",
                columns: new[] { "Status", "NextRunAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledReport_TenantId",
                table: "ScheduledReports",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "UQ_ScheduledReport_Name",
                table: "ScheduledReports",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Health_RecordedAt",
                table: "ScraperHealth",
                column: "RecordedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Health_Site",
                table: "ScraperHealth",
                column: "SiteName");

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

            migrationBuilder.CreateIndex(
                name: "IX_WatchedListing_ListingId",
                table: "WatchedListings",
                column: "ListingId");

            migrationBuilder.CreateIndex(
                name: "IX_WatchedListing_TenantId",
                table: "WatchedListings",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "UQ_WatchedListing_TenantListing",
                table: "WatchedListings",
                columns: new[] { "TenantId", "ListingId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlertNotifications");

            migrationBuilder.DropTable(
                name: "CustomMarkets");

            migrationBuilder.DropTable(
                name: "DeduplicationAudit");

            migrationBuilder.DropTable(
                name: "DeduplicationConfig");

            migrationBuilder.DropTable(
                name: "PriceHistory");

            migrationBuilder.DropTable(
                name: "Reports");

            migrationBuilder.DropTable(
                name: "ResourceThrottles");

            migrationBuilder.DropTable(
                name: "ResponseCache");

            migrationBuilder.DropTable(
                name: "ReviewQueue");

            migrationBuilder.DropTable(
                name: "ScheduledReports");

            migrationBuilder.DropTable(
                name: "ScraperHealth");

            migrationBuilder.DropTable(
                name: "SearchProfiles");

            migrationBuilder.DropTable(
                name: "SearchSessions");

            migrationBuilder.DropTable(
                name: "Vehicles");

            migrationBuilder.DropTable(
                name: "WatchedListings");

            migrationBuilder.DropTable(
                name: "Alerts");

            migrationBuilder.DropTable(
                name: "Listings");

            migrationBuilder.DropTable(
                name: "Dealers");
        }
    }
}
