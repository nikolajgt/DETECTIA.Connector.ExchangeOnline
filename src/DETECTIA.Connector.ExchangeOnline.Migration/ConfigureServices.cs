using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DETECTIA.Connector.ExchangeOnline.Migration;

public static class ConfigureServices
{
    public static IServiceCollection AddEntityFramework(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        services.AddPooledDbContextFactory<AppDatabaseContext>(options =>
        {
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
        });

        return services;
    }
}