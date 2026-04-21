using GoodHamburgerAPI.Infrastructure.Cache;
using GoodHamburgerAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace GoodHamburgerAPI.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        AddDatabase(services, configuration);
        AddRedis(services, configuration);
        AddHealthChecks(services);

        return services;
    }

    private static void AddDatabase(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<AppDbContext>(options =>
            options.UseMySql(
                connectionString,
                ServerVersion.AutoDetect(connectionString),
                mySqlOptions => mySqlOptions.EnableRetryOnFailure()
            )
        );
    }

    private static void AddRedis(IServiceCollection services, IConfiguration configuration)
    {
        var redisConnectionString = configuration.GetConnectionString("Redis") ?? "localhost:6379";

        services.AddSingleton<IConnectionMultiplexer>(provider =>
        {
            try
            {
                var options = ConfigurationOptions.Parse(redisConnectionString);
                return ConnectionMultiplexer.Connect(options);
            }
            catch
            {
                return null!;
            }
        });

        services.AddScoped<IRedisService, RedisService>();
    }

    private static void AddHealthChecks(IServiceCollection services)
    {
        services.AddScoped<IDbHealthCheck, EfCoreHealthCheck>();
    }
}
