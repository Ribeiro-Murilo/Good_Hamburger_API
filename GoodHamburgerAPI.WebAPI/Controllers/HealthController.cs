using GoodHamburgerAPI.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace GoodHamburgerAPI.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly IHealthCheckService _healthCheckService;

    public HealthController(IHealthCheckService healthCheckService)
    {
        _healthCheckService = healthCheckService;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<HealthStatus>> GetHealth()
    {
        var health = await _healthCheckService.GetHealthStatusAsync();

        var statusCode = health.Status == "Healthy" ? StatusCodes.Status200OK : StatusCodes.Status503ServiceUnavailable;
        return StatusCode(statusCode, health);
    }
}
