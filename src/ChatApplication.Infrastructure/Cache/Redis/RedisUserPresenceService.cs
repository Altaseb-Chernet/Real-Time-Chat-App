using System.Text.Json;
using ChatApplication.Core.Modules.User.Contracts;
using ChatApplication.Core.Modules.User.Models;
using ChatApplication.Shared.Enums;
using StackExchange.Redis;

namespace ChatApplication.Infrastructure.Cache.Redis;

/// <summary>
/// Tracks user presence in Redis.
///
/// Data layout:
///   presence:{userId}   → Hash  { status, username, lastSeen }   (TTL: 24h, refreshed on activity)
///   presence:online     → Set   of userIds currently Online or Away
/// </summary>
public class RedisUserPresenceService : IUserPresenceService
{
    private readonly IDatabase _db;

    private const string OnlineSetKey = "presence:online";
    private static readonly TimeSpan PresenceTtl = TimeSpan.FromHours(24);

    public RedisUserPresenceService(RedisConnection connection)
        => _db = connection.GetDatabase();

    public async Task SetOnlineAsync(string userId, string username)
    {
        var key = UserKey(userId);
        var now = DateTime.UtcNow;

        var batch = _db.CreateBatch();
        var hashTask = batch.HashSetAsync(key, new HashEntry[]
        {
            new("status",   ((int)UserStatusType.Online).ToString()),
            new("username", username),
            new("lastSeen", now.ToString("O"))
        });
        var expireTask = batch.KeyExpireAsync(key, PresenceTtl);
        var setTask    = batch.SetAddAsync(OnlineSetKey, userId);
        batch.Execute();

        await Task.WhenAll(hashTask, expireTask, setTask);
    }

    public async Task SetOfflineAsync(string userId)
    {
        var key = UserKey(userId);
        var now = DateTime.UtcNow;

        var batch = _db.CreateBatch();
        var hashTask   = batch.HashSetAsync(key, new HashEntry[]
        {
            new("status",   ((int)UserStatusType.Offline).ToString()),
            new("lastSeen", now.ToString("O"))
        });
        var expireTask = batch.KeyExpireAsync(key, PresenceTtl);
        var removeTask = batch.SetRemoveAsync(OnlineSetKey, userId);
        batch.Execute();

        await Task.WhenAll(hashTask, expireTask, removeTask);
    }

    public async Task SetAwayAsync(string userId)
    {
        var key = UserKey(userId);
        var now = DateTime.UtcNow;

        var batch = _db.CreateBatch();
        var hashTask   = batch.HashSetAsync(key, new HashEntry[]
        {
            new("status",   ((int)UserStatusType.Away).ToString()),
            new("lastSeen", now.ToString("O"))
        });
        var expireTask = batch.KeyExpireAsync(key, PresenceTtl);
        // Keep in online set — Away users are still "reachable"
        var setTask    = batch.SetAddAsync(OnlineSetKey, userId);
        batch.Execute();

        await Task.WhenAll(hashTask, expireTask, setTask);
    }

    public async Task<UserStatus> GetStatusAsync(string userId)
    {
        var fields = await _db.HashGetAllAsync(UserKey(userId));
        return fields.Length == 0
            ? new UserStatus { UserId = userId, Status = UserStatusType.Offline, LastSeen = DateTime.MinValue }
            : MapToStatus(userId, fields);
    }

    public async Task<IEnumerable<UserStatus>> GetOnlineUsersAsync()
    {
        var members = await _db.SetMembersAsync(OnlineSetKey);
        if (members.Length == 0) return Enumerable.Empty<UserStatus>();

        var tasks = members
            .Select(m => _db.HashGetAllAsync(UserKey(m.ToString())))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        return results
            .Select((fields, i) => MapToStatus(members[i].ToString(), fields))
            .Where(s => s.Status != UserStatusType.Offline);
    }

    public async Task<bool> IsOnlineAsync(string userId)
        => await _db.SetContainsAsync(OnlineSetKey, userId);

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static string UserKey(string userId) => $"presence:{userId}";

    private static UserStatus MapToStatus(string userId, HashEntry[] fields)
    {
        var dict = fields.ToDictionary(f => f.Name.ToString(), f => f.Value.ToString());

        var status = dict.TryGetValue("status", out var s) && int.TryParse(s, out var code)
            ? (UserStatusType)code
            : UserStatusType.Offline;

        var lastSeen = dict.TryGetValue("lastSeen", out var ls) && DateTime.TryParse(ls, out var dt)
            ? dt
            : DateTime.MinValue;

        var username = dict.TryGetValue("username", out var u) ? u : string.Empty;

        return new UserStatus
        {
            UserId   = userId,
            Username = username,
            Status   = status,
            LastSeen = lastSeen
        };
    }
}
