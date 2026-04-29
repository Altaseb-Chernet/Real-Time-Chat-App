using ChatApplication.Infrastructure.Cache.Redis;
using ChatApplication.Infrastructure.SignalR.Interfaces;
using StackExchange.Redis;

namespace ChatApplication.Infrastructure.SignalR.Services;

/// <summary>
/// Redis-backed connection → userId reverse-lookup map.
///
/// Data layout:
///   connmgr:{connectionId} → String userId
/// </summary>
public class SignalRConnectionManager : IConnectionManager
{
    private readonly IDatabase _db;
    private static readonly TimeSpan ConnectionTtl = TimeSpan.FromDays(1);

    public SignalRConnectionManager(RedisConnection connection)
        => _db = connection.GetDatabase();

    public async Task AddConnectionAsync(string userId, string connectionId)
        => await _db.StringSetAsync(Key(connectionId), userId, ConnectionTtl);

    public async Task RemoveConnectionAsync(string connectionId)
        => await _db.KeyDeleteAsync(Key(connectionId));

    public async Task<string?> GetUserIdAsync(string connectionId)
    {
        var value = await _db.StringGetAsync(Key(connectionId));
        return value.IsNullOrEmpty ? null : value.ToString();
    }

    private static string Key(string connectionId) => $"connmgr:{connectionId}";
}
