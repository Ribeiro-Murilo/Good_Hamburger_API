using StackExchange.Redis;
using System.Text.Json;

namespace GoodHamburgerAPI.Infrastructure.Cache;

public class RedisService : IRedisService
{
    private readonly IConnectionMultiplexer? _connectionMultiplexer;

    public RedisService(IConnectionMultiplexer? connectionMultiplexer)
    {
        _connectionMultiplexer = connectionMultiplexer;
    }

    private bool IsConnected => _connectionMultiplexer?.IsConnected ?? false;

    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            if (!IsConnected)
                return default;

            var db = _connectionMultiplexer!.GetDatabase();
            var value = await db.StringGetAsync(key);

            if (!value.HasValue)
                return default;

            return JsonSerializer.Deserialize<T>(value.ToString());
        }
        catch
        {
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        try
        {
            if (!IsConnected)
                return;

            var db = _connectionMultiplexer!.GetDatabase();
            var jsonValue = JsonSerializer.Serialize(value);
            await db.StringSetAsync(key, jsonValue, expiration);
        }
        catch
        {
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            if (!IsConnected)
                return;

            var db = _connectionMultiplexer!.GetDatabase();
            await db.KeyDeleteAsync(key);
        }
        catch
        {
        }
    }

    public async Task<bool> ExistsAsync(string key)
    {
        try
        {
            if (!IsConnected)
                return false;

            var db = _connectionMultiplexer!.GetDatabase();
            return await db.KeyExistsAsync(key);
        }
        catch
        {
            return false;
        }
    }
}
