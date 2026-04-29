using System.Text.Json;
using ChatApplication.Infrastructure.Cache.Interfaces;
using StackExchange.Redis;

namespace ChatApplication.Infrastructure.Cache.Redis;

public class RedisCacheService : ICacheService
{
    private readonly IDatabase _db;

    public RedisCacheService(RedisConnection connection) => _db = connection.GetDatabase();

    public async Task<T?> GetAsync<T>(string key)
    {
        var value = await _db.StringGetAsync(key);
        return value.IsNullOrEmpty ? default : JsonSerializer.Deserialize<T>(value!);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
        => await _db.StringSetAsync(key, JsonSerializer.Serialize(value), expiry);

    public async Task RemoveAsync(string key) => await _db.KeyDeleteAsync(key);

    public async Task<bool> ExistsAsync(string key) => await _db.KeyExistsAsync(key);
}
