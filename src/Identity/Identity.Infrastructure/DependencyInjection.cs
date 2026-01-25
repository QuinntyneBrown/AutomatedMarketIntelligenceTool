using Identity.Core;
using Identity.Core.Services;
using Identity.Infrastructure.Data;
using Identity.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database - Aspire injects connection string as "identitydb"
        var connectionString = configuration.GetConnectionString("identitydb");
        var useInMemoryDb = string.IsNullOrEmpty(connectionString)
            || configuration.GetValue<bool>("UseInMemoryDatabase");

        if (useInMemoryDb)
        {
            services.AddDbContext<IdentityDbContext>(options =>
                options.UseInMemoryDatabase("IdentityService"));
        }
        else
        {
            services.AddDbContext<IdentityDbContext>(options =>
                options.UseSqlServer(connectionString!));
        }

        services.AddScoped<IIdentityContext>(provider =>
            provider.GetRequiredService<IdentityDbContext>());

        // JWT Options
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        // Services
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IEmailService, EmailService>();

        return services;
    }
}
