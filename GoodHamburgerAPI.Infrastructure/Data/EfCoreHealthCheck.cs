using Microsoft.EntityFrameworkCore;

namespace GoodHamburgerAPI.Infrastructure.Data;

public class EfCoreHealthCheck : IDbHealthCheck
{
    private readonly AppDbContext _dbContext;

    public EfCoreHealthCheck(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> CanConnectAsync()
    {
        await _dbContext.Database.ExecuteSqlAsync($"SELECT 1");
        return true;
    }
}
