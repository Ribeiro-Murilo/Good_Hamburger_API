namespace GoodHamburgerAPI.Application.Services;

public interface IHealthCheckService
{
    Task<HealthStatus> GetHealthStatusAsync();
}

public class HealthStatus
{
    public string Status { get; set; } = "Unhealthy";
    public Dictionary<string, ComponentHealth> Components { get; set; } = new();
}

public class ComponentHealth
{
    public string Status { get; set; } = "Unhealthy";
    public string? Message { get; set; }
}
