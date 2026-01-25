using AspNetCoreRateLimit;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add YARP reverse proxy
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Automated Market Intelligence Tool API Gateway",
        Version = "v1",
        Description = "API Gateway for the Automated Market Intelligence Tool microservices"
    });
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });

    options.AddPolicy("Production", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Add rate limiting
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.Configure<IpRateLimitPolicies>(builder.Configuration.GetSection("IpRateLimitPolicies"));
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

// Add health checks
builder.Services.AddHealthChecks()
    .AddUrlGroup(new Uri("http://localhost:5001/health"), name: "identity-service", tags: ["services"])
    .AddUrlGroup(new Uri("http://localhost:5002/health"), name: "listing-service", tags: ["services"])
    .AddUrlGroup(new Uri("http://localhost:5003/health"), name: "vehicle-aggregation-service", tags: ["services"])
    .AddUrlGroup(new Uri("http://localhost:5004/health"), name: "search-service", tags: ["services"])
    .AddUrlGroup(new Uri("http://localhost:5005/health"), name: "alert-service", tags: ["services"])
    .AddUrlGroup(new Uri("http://localhost:5006/health"), name: "watchlist-service", tags: ["services"])
    .AddUrlGroup(new Uri("http://localhost:5007/health"), name: "reporting-service", tags: ["services"])
    .AddUrlGroup(new Uri("http://localhost:5008/health"), name: "dashboard-service", tags: ["services"])
    .AddUrlGroup(new Uri("http://localhost:5009/health"), name: "dealer-service", tags: ["services"])
    .AddUrlGroup(new Uri("http://localhost:5010/health"), name: "image-service", tags: ["services"])
    .AddUrlGroup(new Uri("http://localhost:5011/health"), name: "scraping-service", tags: ["services"])
    .AddUrlGroup(new Uri("http://localhost:5012/health"), name: "geographic-service", tags: ["services"])
    .AddUrlGroup(new Uri("http://localhost:5013/health"), name: "notification-service", tags: ["services"])
    .AddUrlGroup(new Uri("http://localhost:5014/health"), name: "configuration-service", tags: ["services"])
    .AddUrlGroup(new Uri("http://localhost:5015/health"), name: "deduplication-service", tags: ["services"])
    .AddUrlGroup(new Uri("http://localhost:5016/health"), name: "backup-service", tags: ["services"]);

// Add HTTP client for health checks
builder.Services.AddHttpClient();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "API Gateway v1");
        options.RoutePrefix = "swagger";
    });
    app.UseCors("AllowAll");
}
else
{
    app.UseCors("Production");
}

app.UseIpRateLimiting();

app.UseHttpsRedirection();

// Health check endpoint
app.MapHealthChecks("/health");

// Aggregated health check endpoint with detailed status
app.MapGet("/health/services", async (IHttpClientFactory httpClientFactory) =>
{
    var services = new Dictionary<string, ServiceHealthInfo>
    {
        ["identity-service"] = new("http://localhost:5001/health", "Identity Service"),
        ["listing-service"] = new("http://localhost:5002/health", "Listing Service"),
        ["vehicle-aggregation-service"] = new("http://localhost:5003/health", "Vehicle Aggregation Service"),
        ["search-service"] = new("http://localhost:5004/health", "Search Service"),
        ["alert-service"] = new("http://localhost:5005/health", "Alert Service"),
        ["watchlist-service"] = new("http://localhost:5006/health", "WatchList Service"),
        ["reporting-service"] = new("http://localhost:5007/health", "Reporting Service"),
        ["dashboard-service"] = new("http://localhost:5008/health", "Dashboard Service"),
        ["dealer-service"] = new("http://localhost:5009/health", "Dealer Service"),
        ["image-service"] = new("http://localhost:5010/health", "Image Service"),
        ["scraping-service"] = new("http://localhost:5011/health", "Scraping Service"),
        ["geographic-service"] = new("http://localhost:5012/health", "Geographic Service"),
        ["notification-service"] = new("http://localhost:5013/health", "Notification Service"),
        ["configuration-service"] = new("http://localhost:5014/health", "Configuration Service"),
        ["deduplication-service"] = new("http://localhost:5015/health", "Deduplication Service"),
        ["backup-service"] = new("http://localhost:5016/health", "Backup Service")
    };

    var client = httpClientFactory.CreateClient();
    client.Timeout = TimeSpan.FromSeconds(5);

    var results = new Dictionary<string, object>();
    var allHealthy = true;

    foreach (var (key, info) in services)
    {
        try
        {
            var response = await client.GetAsync(info.HealthUrl);
            var status = response.IsSuccessStatusCode ? "Healthy" : "Unhealthy";
            if (!response.IsSuccessStatusCode) allHealthy = false;

            results[key] = new
            {
                name = info.DisplayName,
                status,
                statusCode = (int)response.StatusCode
            };
        }
        catch (Exception ex)
        {
            allHealthy = false;
            results[key] = new
            {
                name = info.DisplayName,
                status = "Unhealthy",
                error = ex.Message
            };
        }
    }

    var overallStatus = allHealthy ? "Healthy" : "Degraded";

    return Results.Ok(new
    {
        status = overallStatus,
        timestamp = DateTime.UtcNow,
        services = results
    });
})
.WithName("GetServicesHealth")
.WithOpenApi();

// Map YARP reverse proxy
app.MapReverseProxy();

app.Run();

record ServiceHealthInfo(string HealthUrl, string DisplayName);
