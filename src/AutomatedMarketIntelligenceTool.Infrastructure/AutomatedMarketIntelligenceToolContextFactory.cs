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
            if (provider == "sqlserver")
            {
                optionsBuilder.UseSqlServer(connectionString);
            }
            else
            {
                optionsBuilder.UseSqlite(connectionString);
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
