var builder = DistributedApplication.CreateBuilder(args);

// Infrastructure Resources
var sqlServer = builder.AddSqlServer("sqlserver")
    .WithLifetime(ContainerLifetime.Persistent);

// Databases
var identityDb = sqlServer.AddDatabase("identitydb");
var listingDb = sqlServer.AddDatabase("listingdb");
var vehicleDb = sqlServer.AddDatabase("vehicledb");
var searchDb = sqlServer.AddDatabase("searchdb");
var alertDb = sqlServer.AddDatabase("alertdb");
var watchlistDb = sqlServer.AddDatabase("watchlistdb");
var reportingDb = sqlServer.AddDatabase("reportingdb");
var dealerDb = sqlServer.AddDatabase("dealerdb");
var imageDb = sqlServer.AddDatabase("imagedb");
var scrapingDb = sqlServer.AddDatabase("scrapingdb");
var geographicDb = sqlServer.AddDatabase("geographicdb");
var notificationDb = sqlServer.AddDatabase("notificationdb");
var configurationDb = sqlServer.AddDatabase("configurationdb");
var deduplicationDb = sqlServer.AddDatabase("deduplicationdb");

// RabbitMQ for messaging
var messaging = builder.AddRabbitMQ("messaging")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithManagementPlugin();

// Redis for caching
var cache = builder.AddRedis("cache")
    .WithLifetime(ContainerLifetime.Persistent);

// API Services
var identityApi = builder.AddProject("identity-api", "../Identity/Identity.Api/Identity.Api.csproj")
    .WithReference(identityDb)
    .WithReference(messaging)
    .WithReference(cache)
    .WaitFor(identityDb);

var listingApi = builder.AddProject("listing-api", "../Listing/Listing.Api/Listing.Api.csproj")
    .WithReference(listingDb)
    .WithReference(messaging)
    .WithReference(cache)
    .WaitFor(listingDb);

var vehicleAggregationApi = builder.AddProject("vehicleaggregation-api", "../VehicleAggregation/VehicleAggregation.Api/VehicleAggregation.Api.csproj")
    .WithReference(vehicleDb)
    .WithReference(messaging)
    .WithReference(cache)
    .WaitFor(vehicleDb);

var searchApi = builder.AddProject("search-api", "../Search/Search.Api/Search.Api.csproj")
    .WithReference(searchDb)
    .WithReference(messaging)
    .WithReference(cache)
    .WaitFor(searchDb);

var alertApi = builder.AddProject("alert-api", "../Alert/Alert.Api/Alert.Api.csproj")
    .WithReference(alertDb)
    .WithReference(messaging)
    .WithReference(cache)
    .WaitFor(alertDb);

var watchlistApi = builder.AddProject("watchlist-api", "../WatchList/WatchList.Api/WatchList.Api.csproj")
    .WithReference(watchlistDb)
    .WithReference(messaging)
    .WithReference(cache)
    .WaitFor(watchlistDb);

var reportingApi = builder.AddProject("reporting-api", "../Reporting/Reporting.Api/Reporting.Api.csproj")
    .WithReference(reportingDb)
    .WithReference(messaging)
    .WithReference(cache)
    .WaitFor(reportingDb);

var dashboardApi = builder.AddProject("dashboard-api", "../Dashboard/Dashboard.Api/Dashboard.Api.csproj")
    .WithReference(messaging)
    .WithReference(cache);

var dealerApi = builder.AddProject("dealer-api", "../Dealer/Dealer.Api/Dealer.Api.csproj")
    .WithReference(dealerDb)
    .WithReference(messaging)
    .WithReference(cache)
    .WaitFor(dealerDb);

var imageApi = builder.AddProject("image-api", "../Image/Image.Api/Image.Api.csproj")
    .WithReference(imageDb)
    .WithReference(messaging)
    .WithReference(cache)
    .WaitFor(imageDb);

var scrapingOrchestrationApi = builder.AddProject("scrapingorchestration-api", "../ScrapingOrchestration/ScrapingOrchestration.Api/ScrapingOrchestration.Api.csproj")
    .WithReference(scrapingDb)
    .WithReference(messaging)
    .WithReference(cache)
    .WaitFor(scrapingDb);

var geographicApi = builder.AddProject("geographic-api", "../Geographic/Geographic.Api/Geographic.Api.csproj")
    .WithReference(geographicDb)
    .WithReference(messaging)
    .WithReference(cache)
    .WaitFor(geographicDb);

var notificationApi = builder.AddProject("notification-api", "../Notification/Notification.Api/Notification.Api.csproj")
    .WithReference(notificationDb)
    .WithReference(messaging)
    .WithReference(cache)
    .WaitFor(notificationDb);

var configurationApi = builder.AddProject("configuration-api", "../Configuration/Configuration.Api/Configuration.Api.csproj")
    .WithReference(configurationDb)
    .WithReference(messaging)
    .WithReference(cache)
    .WaitFor(configurationDb);

var deduplicationApi = builder.AddProject("deduplication-api", "../Deduplication/Deduplication.Api/Deduplication.Api.csproj")
    .WithReference(deduplicationDb)
    .WithReference(messaging)
    .WithReference(cache)
    .WaitFor(deduplicationDb);

var backupApi = builder.AddProject("backup-api", "../Backup/Backup.Api/Backup.Api.csproj")
    .WithReference(messaging)
    .WithReference(cache);

// Worker Services
var backupService = builder.AddProject("backup-service", "../Backup/Backup.Service/Backup.Service.csproj")
    .WithReference(messaging);

var scrapingWorkerService = builder.AddProject("scrapingworker-service", "../ScrapingWorker/ScrapingWorker.Service/ScrapingWorker.Service.csproj")
    .WithReference(messaging)
    .WithReference(cache);

// API Gateway - references all API services for routing
var apiGateway = builder.AddProject("api-gateway", "../ApiGateway/ApiGateway.csproj")
    .WithReference(identityApi)
    .WithReference(listingApi)
    .WithReference(vehicleAggregationApi)
    .WithReference(searchApi)
    .WithReference(alertApi)
    .WithReference(watchlistApi)
    .WithReference(reportingApi)
    .WithReference(dashboardApi)
    .WithReference(dealerApi)
    .WithReference(imageApi)
    .WithReference(scrapingOrchestrationApi)
    .WithReference(geographicApi)
    .WithReference(notificationApi)
    .WithReference(configurationApi)
    .WithReference(deduplicationApi)
    .WithReference(backupApi);

// Angular UI
var angularUi = builder.AddNpmApp("angular-ui", "../Ui")
    .WithReference(apiGateway)
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile();

builder.Build().Run();
