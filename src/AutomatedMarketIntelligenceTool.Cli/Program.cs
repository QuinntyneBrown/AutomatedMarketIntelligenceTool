using AutomatedMarketIntelligenceTool.Cli.Commands;
using AutomatedMarketIntelligenceTool.Core;
using AutomatedMarketIntelligenceTool.Core.Services;
using AutomatedMarketIntelligenceTool.Infrastructure;
using AutomatedMarketIntelligenceTool.Infrastructure.Services.Scrapers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Spectre.Console.Cli;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .Enrich.WithProperty("Application", "AutomatedMarketIntelligenceTool.Cli")
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/cli-.log",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}",
        retainedFileCountLimit: 7)
    .CreateLogger();

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

    // Add services
    services.AddScoped<ISearchService, SearchService>();
    services.AddScoped<IDuplicateDetectionService, DuplicateDetectionService>();
    services.AddSingleton<IScraperFactory, ScraperFactory>();
    services.AddSingleton<ICorrelationIdProvider, CorrelationIdProvider>();

    // Create service provider
    var registrar = new TypeRegistrar(services);
    var app = new CommandApp(registrar);

    app.Configure(config =>
    {
        config.SetApplicationName("car-search");
        
        config.AddCommand<SearchCommand>("search")
            .WithDescription("Search for car listings in the database")
            .WithExample("search", "-t", "12345678-1234-1234-1234-123456789012", "-m", "Toyota", "--model", "Camry")
            .WithExample("search", "-t", "12345678-1234-1234-1234-123456789012", "--price-max", "30000", "--format", "json");
        
        config.AddCommand<ScrapeCommand>("scrape")
            .WithDescription("Scrape car listings from automotive websites")
            .WithExample("scrape", "-t", "12345678-1234-1234-1234-123456789012", "-s", "autotrader", "-m", "Toyota")
            .WithExample("scrape", "-t", "12345678-1234-1234-1234-123456789012", "-s", "all", "-z", "90210", "-r", "50");

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
