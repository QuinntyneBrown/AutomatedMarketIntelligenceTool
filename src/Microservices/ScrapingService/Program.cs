using AutomatedMarketIntelligenceTool.Core;
using AutomatedMarketIntelligenceTool.Infrastructure;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using ScrapingService.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("ServiceName", "ScrapingService")
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Configure OpenTelemetry
var serviceName = "ScrapingService";
var serviceVersion = "1.0.0";

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(serviceName, serviceVersion))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddGrpcClientInstrumentation()
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri(builder.Configuration["OpenTelemetry:Endpoint"] ?? "http://localhost:4317");
        }))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri(builder.Configuration["OpenTelemetry:Endpoint"] ?? "http://localhost:4317");
        }));

// Add gRPC
builder.Services.AddGrpc(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.MaxReceiveMessageSize = 16 * 1024 * 1024; // 16 MB
    options.MaxSendMessageSize = 16 * 1024 * 1024; // 16 MB
});
builder.Services.AddGrpcReflection();

// Add database context
var connectionString = builder.Configuration.GetConnectionString("ScrapingDb")
    ?? "Data Source=scraping-service.db";
builder.Services.AddDbContext<AutomatedMarketIntelligenceToolContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddScoped<IAutomatedMarketIntelligenceToolContext>(sp =>
    sp.GetRequiredService<AutomatedMarketIntelligenceToolContext>());

// Register infrastructure services
builder.Services.AddInfrastructureServices();
builder.Services.AddCoreServices();

// Add health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AutomatedMarketIntelligenceToolContext>();

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseSerilogRequestLogging();

// Map gRPC services
app.MapGrpcService<ScrapingGrpcService>();
app.MapGrpcReflectionService();

// Health check endpoints
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapGet("/", () => "Scraping Service - gRPC endpoint available");

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AutomatedMarketIntelligenceToolContext>();
    await context.Database.EnsureCreatedAsync();
}

try
{
    Log.Information("Starting Scraping Service");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Scraping Service terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
