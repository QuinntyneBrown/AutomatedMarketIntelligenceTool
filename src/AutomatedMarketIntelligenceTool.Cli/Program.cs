using AutomatedMarketIntelligenceTool.Cli;
using AutomatedMarketIntelligenceTool.Cli.Commands;
using AutomatedMarketIntelligenceTool.Cli.Configuration;
using AutomatedMarketIntelligenceTool.Core;
using AutomatedMarketIntelligenceTool.Core.Services;
using AutomatedMarketIntelligenceTool.Core.Services.ImageAnalysis;
using AutomatedMarketIntelligenceTool.Infrastructure;
using AutomatedMarketIntelligenceTool.Infrastructure.Services.RateLimiting;
using AutomatedMarketIntelligenceTool.Infrastructure.Services.Scrapers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Spectre.Console.Cli;

// Parse verbosity level from command line args
var verbosityLevel = VerbosityHelper.ParseFromArgs(args);
var logLevel = VerbosityHelper.ToLogEventLevel(verbosityLevel);

// Load configuration for log directory
var configManager = new ConfigurationManager();
var appSettings = configManager.LoadSettings();
var logDirectory = appSettings.Verbosity.LogDirectory;
var retainDays = appSettings.Verbosity.RetainDays;

// Configure console output template based on verbosity
var consoleTemplate = verbosityLevel switch
{
    VerbosityLevel.Quiet => "{Message:lj}{NewLine}{Exception}",
    VerbosityLevel.Normal => "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
    VerbosityLevel.Verbose => "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}",
    VerbosityLevel.Debug => "[{Timestamp:HH:mm:ss.fff} {Level:u3}] [{SourceContext}] {Message:lj} {Properties:j}{NewLine}{Exception}",
    VerbosityLevel.Trace => "[{Timestamp:HH:mm:ss.fff zzz} {Level:u3}] [{SourceContext}] {Message:lj} {Properties:j}{NewLine}{Exception}",
    _ => "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
};

// Configure Serilog with dynamic verbosity
var logConfig = new LoggerConfiguration()
    .MinimumLevel.Is(logLevel)
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .Enrich.WithProperty("Application", "AutomatedMarketIntelligenceTool.Cli")
    .Enrich.WithProperty("VerbosityLevel", verbosityLevel.ToString());

// Only show console output if not in quiet mode
if (verbosityLevel != VerbosityLevel.Quiet)
{
    logConfig = logConfig.WriteTo.Console(outputTemplate: consoleTemplate);
}

// Add file logging if enabled
if (appSettings.Verbosity.EnableFileLogging)
{
    logConfig = logConfig.WriteTo.File(
        path: Path.Combine(logDirectory, "cli-.log"),
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj} {Properties:j}{NewLine}{Exception}",
        retainedFileCountLimit: retainDays);
}

Log.Logger = logConfig.CreateLogger();

try
{
    var services = new ServiceCollection();

    // Add logging
    services.AddLogging(builder =>
    {
        builder.ClearProviders();
        builder.AddSerilog(dispose: true);
    });

// Add DbContext
services.AddDbContext<IAutomatedMarketIntelligenceToolContext, AutomatedMarketIntelligenceToolContext>(options =>
{
    // Use SQLite for Phase 1
    options.UseSqlite("Data Source=car-search.db");
});

    // Add core services
    services.AddScoped<ISearchService, SearchService>();
    services.AddScoped<IDuplicateDetectionService, DuplicateDetectionService>();
    services.AddScoped<IAutoCompleteService, AutoCompleteService>();
    services.AddSingleton<ICorrelationIdProvider, CorrelationIdProvider>();

    // Add rate limiting with configuration
    services.AddSingleton(_ => new RateLimitConfiguration
    {
        DefaultDelayMs = 3000, // 3 seconds between requests
        MaxBackoffMs = 30000   // Max 30 seconds backoff
    });
    services.AddSingleton<IRateLimiter, RateLimiter>();
    services.AddSingleton<IScraperFactory, ScraperFactory>();

    // Phase 4: Image Analysis Services
    services.AddHttpClient<IImageDownloadService, ImageDownloadService>();
    services.AddSingleton<PerceptualHashCalculator>();
    services.AddScoped<IImageHashingService, ImageHashingService>();

    // Phase 4: Review and Relisted Detection Services
    services.AddScoped<IReviewService, ReviewService>();
    services.AddScoped<IRelistedDetectionService, RelistedDetectionService>();

    // Store verbosity level for services to access
    services.AddSingleton(typeof(VerbosityLevel), verbosityLevel);

    // Create service provider
    var registrar = new TypeRegistrar(services);
    var app = new CommandApp(registrar);

    app.Configure(config =>
    {
        config.SetApplicationName("car-search");
        config.SetApplicationVersion("3.0.0");
        
        config.AddCommand<SearchCommand>("search")
            .WithDescription("Search for car listings in the database")
            .WithExample("search", "-t", "12345678-1234-1234-1234-123456789012", "-m", "Toyota", "--model", "Camry")
            .WithExample("search", "-t", "12345678-1234-1234-1234-123456789012", "--price-max", "30000", "--format", "json")
            .WithExample("search", "-i", "-t", "12345678-1234-1234-1234-123456789012");
        
        config.AddCommand<ScrapeCommand>("scrape")
            .WithDescription("Scrape car listings from automotive websites")
            .WithExample("scrape", "-t", "12345678-1234-1234-1234-123456789012", "-s", "autotrader", "-m", "Toyota")
            .WithExample("scrape", "-t", "12345678-1234-1234-1234-123456789012", "-s", "all", "-p", "M5V 3L9", "-r", "50");
        
        config.AddCommand<ListCommand>("list")
            .WithDescription("List saved car listings from the database with filtering and pagination")
            .WithExample("list", "-t", "12345678-1234-1234-1234-123456789012")
            .WithExample("list", "-t", "12345678-1234-1234-1234-123456789012", "-m", "Toyota", "--condition", "Used")
            .WithExample("list", "-t", "12345678-1234-1234-1234-123456789012", "--new-only", "--sort", "Price", "--page-size", "50");
        
        config.AddCommand<ExportCommand>("export")
            .WithDescription("Export car listings to JSON or CSV files")
            .WithExample("export", "results.json", "-t", "12345678-1234-1234-1234-123456789012", "-m", "Honda")
            .WithExample("export", "results.csv", "-t", "12345678-1234-1234-1234-123456789012", "--condition", "New", "--format", "csv");
        
        config.AddCommand<ConfigCommand>("config")
            .WithDescription("Manage application configuration settings")
            .WithExample("config", "list")
            .WithExample("config", "get", "Database:Provider")
            .WithExample("config", "set", "Scraping:DefaultDelayMs", "5000");

        config.AddCommand<ReviewCommand>("review")
            .WithDescription("Manage the review queue for near-match duplicates")
            .WithExample("review", "list", "-t", "12345678-1234-1234-1234-123456789012")
            .WithExample("review", "list", "-t", "12345678-1234-1234-1234-123456789012", "--status", "Pending")
            .WithExample("review", "resolve", "abc12345", "-t", "12345678-1234-1234-1234-123456789012", "--same-vehicle")
            .WithExample("review", "dismiss", "abc12345", "-t", "12345678-1234-1234-1234-123456789012");

        config.PropagateExceptions();
        config.ValidateExamples();
    });

    Log.Information("Starting AutomatedMarketIntelligenceTool CLI");
    return await app.RunAsync(args);
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}

// Type registrar for dependency injection with Spectre.Console.Cli
internal sealed class TypeRegistrar : ITypeRegistrar
{
    private readonly IServiceCollection _services;

    public TypeRegistrar(IServiceCollection services)
    {
        _services = services;
    }

    public ITypeResolver Build()
    {
        return new TypeResolver(_services.BuildServiceProvider());
    }

    public void Register(Type service, Type implementation)
    {
        _services.AddSingleton(service, implementation);
    }

    public void RegisterInstance(Type service, object implementation)
    {
        _services.AddSingleton(service, implementation);
    }

    public void RegisterLazy(Type service, Func<object> factory)
    {
        _services.AddSingleton(service, _ => factory());
    }
}

internal sealed class TypeResolver : ITypeResolver
{
    private readonly IServiceProvider _provider;

    public TypeResolver(IServiceProvider provider)
    {
        _provider = provider;
    }

    public object? Resolve(Type? type)
    {
        return type == null ? null : _provider.GetService(type);
    }
}
