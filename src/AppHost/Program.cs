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
var identityApi = builder.AddProject<Projects.Identity_Api>("identity-api")
    .WithReference(identityDb)
    .WithReference(messaging)
    .WithReference(cache)
    .WaitFor(identityDb);

var listingApi = builder.AddProject<Projects.Listing_Api>("listing-api")
    .WithReference(listingDb)
    .WithReference(messaging)
    .WithReference(cache)
    .WaitFor(listingDb);

var vehicleAggregationApi = builder.AddProject<Projects.VehicleAggregation_Api>("vehicleaggregation-api")
    .WithReference(vehicleDb)
    .WithReference(messaging)
    .WithReference(cache)
    .WaitFor(vehicleDb);

var searchApi = builder.AddProject<Projects.Search_Api>("search-api")
    .WithReference(searchDb)
    .WithReference(messaging)
    .WithReference(cache)
    .WaitFor(searchDb);

var alertApi = builder.AddProject<Projects.Alert_Api>("alert-api")
    .WithReference(alertDb)
    .WithReference(messaging)
    .WithReference(cache)
    .WaitFor(alertDb);

var watchlistApi = builder.AddProject<Projects.WatchList_Api>("watchlist-api")
    .WithReference(watchlistDb)
    .WithReference(messaging)
    .WithReference(cache)
    .WaitFor(watchlistDb);

var reportingApi = builder.AddProject<Projects.Reporting_Api>("reporting-api")
    .WithReference(reportingDb)
    .WithReference(messaging)
    .WithReference(cache)
    .WaitFor(reportingDb);

var dashboardApi = builder.AddProject<Projects.Dashboard_Api>("dashboard-api")
    .WithReference(messaging)
    .WithReference(cache);

var dealerApi = builder.AddProject<Projects.Dealer_Api>("dealer-api")
    .WithReference(dealerDb)
    .WithReference(messaging)
    .WithReference(cache)
    .WaitFor(dealerDb);

var imageApi = builder.AddProject<Projects.Image_Api>("image-api")
    .WithReference(imageDb)
    .WithReference(messaging)
    .WithReference(cache)
    .WaitFor(imageDb);

var scrapingOrchestrationApi = builder.AddProject<Projects.ScrapingOrchestration_Api>("scrapingorchestration-api")
    .WithReference(scrapingDb)
    .WithReference(messaging)
    .WithReference(cache)
    .WaitFor(scrapingDb);

var geographicApi = builder.AddProject<Projects.Geographic_Api>("geographic-api")
    .WithReference(geographicDb)
    .WithReference(messaging)
    .WithReference(cache)
    .WaitFor(geographicDb);

var notificationApi = builder.AddProject<Projects.Notification_Api>("notification-api")
    .WithReference(notificationDb)
    .WithReference(messaging)
    .WithReference(cache)
    .WaitFor(notificationDb);

var configurationApi = builder.AddProject<Projects.Configuration_Api>("configuration-api")
    .WithReference(configurationDb)
    .WithReference(messaging)
    .WithReference(cache)
    .WaitFor(configurationDb);

var deduplicationApi = builder.AddProject<Projects.Deduplication_Api>("deduplication-api")
    .WithReference(deduplicationDb)
    .WithReference(messaging)
    .WithReference(cache)
    .WaitFor(deduplicationDb);

var backupApi = builder.AddProject<Projects.Backup_Api>("backup-api")
    .WithReference(messaging)
    .WithReference(cache);

// Worker Services
var backupService = builder.AddProject<Projects.Backup_Service>("backup-service")
    .WithReference(messaging);

var scrapingWorkerService = builder.AddProject<Projects.ScrapingWorker_Service>("scrapingworker-service")
    .WithReference(messaging)
    .WithReference(cache);

// API Gateway - references all API services for routing
var apiGateway = builder.AddProject<Projects.ApiGateway>("api-gateway")
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
