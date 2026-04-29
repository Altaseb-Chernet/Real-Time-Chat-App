using ChatApplication.Infrastructure.Cache.Redis;
using ChatApplication.Infrastructure.SignalR.Interfaces;
using StackExchange.Redis;

namespace ChatApplication.Infrastructure.SignalR.Services;

/// <summary>
/// Redis-backed user connection tracker.
///
/// Data layout:
///   tracker:user:{userId}       → Set of connectionIds for this user
///   tracker:conn:{connectionId} → String userId (reverse lookup)
/// </summary>
public class SignalRUserTracker : IUserTracker
{
    private readonly IDatabase _db;
    private static readonly TimeSpan ConnectionTtl = TimeSpan.FromDays(1);

    public SignalRUserTracker(RedisConnection connection)
        => _db = connection.GetDatabase();

    public async Task TrackAsync(string userId, string connectionId)
    {
        var batch = _db.CreateBatch();
        var setTask    = batch.SetAddAsync(UserConnectionsKey(userId), connectionId);
        var expireTask = batch.KeyExpireAsync(UserConnectionsKey(userId), ConnectionTtl);
        var strTask    = batch.StringSetAsync(ConnectionUserKey(connectionId), userId, ConnectionTtl);
        batch.Execute();
        await Task.WhenAll(setTask, expireTask, strTask);
    }

    public async Task UntrackAsync(string connectionId)
    {
        var userId = await _db.StringGetAsync(ConnectionUserKey(connectionId));
        if (userId.IsNullOrEmpty) return;

        var batch = _db.CreateBatch();
        var removeTask = batch.SetRemoveAsync(UserConnectionsKey(userId!), connectionId);
        var deleteTask = batch.KeyDeleteAsync(ConnectionUserKey(connectionId));
        batch.Execute();
        await Task.WhenAll(removeTask, deleteTask);
    }

    public async Task<IEnumerable<string>> GetConnectionsAsync(string userId)
    {
        var members = await _db.SetMembersAsync(UserConnectionsKey(userId));
        return members.Select(m => m.ToString());
    }

    public async Task<bool> IsOnlineAsync(string userId)
        => await _db.KeyExistsAsync(UserConnectionsKey(userId))
        && await _db.SetLengthAsync(UserConnectionsKey(userId)) > 0;

    private static string UserConnectionsKey(string userId)     => $"tracker:user:{userId}";
    private static string ConnectionUserKey(string connectionId) => $"tracker:conn:{connectionId}";
}
