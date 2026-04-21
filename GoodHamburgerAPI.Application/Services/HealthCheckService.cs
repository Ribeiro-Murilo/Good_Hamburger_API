using GoodHamburgerAPI.Infrastructure.Cache;
using GoodHamburgerAPI.Infrastructure.Data;

namespace GoodHamburgerAPI.Application.Services;

public class HealthCheckService : IHealthCheckService
{
    private readonly IDbHealthCheck _dbHealthCheck;
    private readonly IRedisService _redisService;

    public HealthCheckService(IDbHealthCheck dbHealthCheck, IRedisService redisService)
    {
        _dbHealthCheck = dbHealthCheck;
        _redisService = redisService;
    }

    public async Task<HealthStatus> GetHealthStatusAsync()
    {
        var health = new HealthStatus();
        var allHealthy = true;

        health.Components["Database"] = await CheckDatabaseHealth();
        if (health.Components["Database"].Status != "Healthy")
            allHealthy = false;

        health.Components["Cache"] = await CheckRedisHealth();
        if (health.Components["Cache"].Status != "Healthy")
            allHealthy = false;

        health.Status = allHealthy ? "Healthy" : "Degraded";
        return health;
    }

    private async Task<ComponentHealth> CheckDatabaseHealth()
    {
        try
        {
            var canConnect = await _dbHealthCheck.CanConnectAsync();
            return canConnect
                ? new ComponentHealth { Status = "Healthy", Message = "Database connection successful" }
                : new ComponentHealth { Status = "Unhealthy", Message = "Database unavailable" };
        }
        catch (Exception ex)
        {
            return new ComponentHealth { Status = "Unhealthy", Message = $"Database unavailable: {ex.Message}" };
        }
    }

    private async Task<ComponentHealth> CheckRedisHealth()
    {
        try
        {
            var testKey = $"health_check_{DateTime.UtcNow.Ticks}";
            await _redisService.SetAsync(testKey, "test", TimeSpan.FromSeconds(10));
            var exists = await _redisService.ExistsAsync(testKey);
            await _redisService.RemoveAsync(testKey);

            if (exists)
                return new ComponentHealth { Status = "Healthy", Message = "Redis connection successful" };
            else
                return new ComponentHealth { Status = "Unhealthy", Message = "Redis unavailable or test operation failed" };
        }
        catch (Exception ex)
        {
            return new ComponentHealth { Status = "Unhealthy", Message = $"Redis unavailable: {ex.Message}" };
        }
    }
}
