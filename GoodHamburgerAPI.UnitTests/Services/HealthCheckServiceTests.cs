using GoodHamburgerAPI.Application.Services;
using GoodHamburgerAPI.Infrastructure.Cache;
using GoodHamburgerAPI.Infrastructure.Data;
using Moq;
using Xunit;

namespace GoodHamburgerAPI.UnitTests.Services;

public class HealthCheckServiceTests
{
    private readonly Mock<IDbHealthCheck> _mockDbHealthCheck;
    private readonly Mock<IRedisService> _mockRedisService;
    private readonly HealthCheckService _healthCheckService;

    public HealthCheckServiceTests()
    {
        _mockDbHealthCheck = new Mock<IDbHealthCheck>();
        _mockRedisService = new Mock<IRedisService>();
        _healthCheckService = new HealthCheckService(_mockDbHealthCheck.Object, _mockRedisService.Object);
    }

    [Fact]
    public async Task GetHealthStatusAsync_WhenAllComponentsAreHealthy_ShouldReturnHealthyStatus()
    {
        _mockDbHealthCheck.Setup(d => d.CanConnectAsync()).ReturnsAsync(true);
        _mockRedisService.Setup(r => r.SetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan?>()))
            .Returns(Task.CompletedTask);
        _mockRedisService.Setup(r => r.ExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(true);
        _mockRedisService.Setup(r => r.RemoveAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var result = await _healthCheckService.GetHealthStatusAsync();

        Assert.Equal("Healthy", result.Status);
        Assert.Equal("Healthy", result.Components["Database"].Status);
        Assert.Equal("Healthy", result.Components["Cache"].Status);
    }

    [Fact]
    public async Task GetHealthStatusAsync_WhenDatabaseIsUnhealthy_ShouldReturnDegradedStatus()
    {
        _mockDbHealthCheck.Setup(d => d.CanConnectAsync()).ReturnsAsync(false);
        _mockRedisService.Setup(r => r.SetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan?>()))
            .Returns(Task.CompletedTask);
        _mockRedisService.Setup(r => r.ExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(true);
        _mockRedisService.Setup(r => r.RemoveAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var result = await _healthCheckService.GetHealthStatusAsync();

        Assert.Equal("Degraded", result.Status);
        Assert.Equal("Unhealthy", result.Components["Database"].Status);
        Assert.Equal("Healthy", result.Components["Cache"].Status);
    }

    [Fact]
    public async Task GetHealthStatusAsync_WhenDatabaseThrowsException_ShouldReturnDegradedStatus()
    {
        _mockDbHealthCheck.Setup(d => d.CanConnectAsync())
            .ThrowsAsync(new Exception("Connection failed"));
        _mockRedisService.Setup(r => r.SetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan?>()))
            .Returns(Task.CompletedTask);
        _mockRedisService.Setup(r => r.ExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(true);
        _mockRedisService.Setup(r => r.RemoveAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var result = await _healthCheckService.GetHealthStatusAsync();

        Assert.Equal("Degraded", result.Status);
        Assert.Equal("Unhealthy", result.Components["Database"].Status);
        Assert.Contains("Connection failed", result.Components["Database"].Message);
    }

    [Fact]
    public async Task GetHealthStatusAsync_WhenRedisIsUnhealthy_ShouldReturnDegradedStatus()
    {
        _mockDbHealthCheck.Setup(d => d.CanConnectAsync()).ReturnsAsync(true);
        _mockRedisService.Setup(r => r.SetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan?>()))
            .Returns(Task.CompletedTask);
        _mockRedisService.Setup(r => r.ExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(false);
        _mockRedisService.Setup(r => r.RemoveAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var result = await _healthCheckService.GetHealthStatusAsync();

        Assert.Equal("Degraded", result.Status);
        Assert.Equal("Healthy", result.Components["Database"].Status);
        Assert.Equal("Unhealthy", result.Components["Cache"].Status);
    }

    [Fact]
    public async Task GetHealthStatusAsync_WhenRedisThrowsException_ShouldReturnDegradedStatus()
    {
        _mockDbHealthCheck.Setup(d => d.CanConnectAsync()).ReturnsAsync(true);
        _mockRedisService.Setup(r => r.SetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan?>()))
            .ThrowsAsync(new Exception("Redis unavailable"));

        var result = await _healthCheckService.GetHealthStatusAsync();

        Assert.Equal("Degraded", result.Status);
        Assert.Equal("Healthy", result.Components["Database"].Status);
        Assert.Equal("Unhealthy", result.Components["Cache"].Status);
        Assert.Contains("Redis unavailable", result.Components["Cache"].Message);
    }
}
