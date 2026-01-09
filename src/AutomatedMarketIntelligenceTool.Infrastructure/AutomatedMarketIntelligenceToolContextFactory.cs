using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AutomatedMarketIntelligenceTool.Infrastructure;

public class AutomatedMarketIntelligenceToolContextFactory : IDesignTimeDbContextFactory<AutomatedMarketIntelligenceToolContext>
{
    public AutomatedMarketIntelligenceToolContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AutomatedMarketIntelligenceToolContext>();
        
        // Check for connection string in environment variable or args
        var connectionString = Environment.GetEnvironmentVariable("AMI_CONNECTION_STRING");
        var provider = Environment.GetEnvironmentVariable("AMI_DB_PROVIDER")?.ToLowerInvariant() ?? "sqlite";

        if (!string.IsNullOrEmpty(connectionString))
        {
            switch (provider)
            {
                case "sqlserver":
                    optionsBuilder.UseSqlServer(connectionString);
                    break;
                case "postgresql":
                case "postgres":
                    optionsBuilder.UseNpgsql(connectionString);
                    break;
                default:
                    optionsBuilder.UseSqlite(connectionString);
                    break;
            }
        }
        else
        {
            // Default to SQLite for migrations
            optionsBuilder.UseSqlite("Data Source=automatedmarketintelligencetool.db");
        }

        return new AutomatedMarketIntelligenceToolContext(optionsBuilder.Options);
    }
}
