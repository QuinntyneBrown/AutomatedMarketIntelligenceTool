using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AutomatedMarketIntelligenceTool.Infrastructure;

public class AutomatedMarketIntelligenceToolContextFactory : IDesignTimeDbContextFactory<AutomatedMarketIntelligenceToolContext>
{
    public AutomatedMarketIntelligenceToolContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AutomatedMarketIntelligenceToolContext>();
        optionsBuilder.UseSqlite("Data Source=automatedmarketintelligencetool.db");

        return new AutomatedMarketIntelligenceToolContext(optionsBuilder.Options);
    }
}
