namespace GoodHamburgerAPI.Infrastructure.Data;

public interface IDbHealthCheck
{
    Task<bool> CanConnectAsync();
}
