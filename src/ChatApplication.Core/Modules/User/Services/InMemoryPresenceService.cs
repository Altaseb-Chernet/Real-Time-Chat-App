using System.Collections.Concurrent;
using ChatApplication.Core.Modules.User.Contracts;
using ChatApplication.Core.Modules.User.Models;
using ChatApplication.Shared.Enums;

namespace ChatApplication.Core.Modules.User.Services;

/// <summary>
/// In-process presence store. Works without Redis.
/// Replace with RedisUserPresenceService for multi-instance deployments.
/// </summary>
public class InMemoryPresenceService : IUserPresenceService
{
    private readonly ConcurrentDictionary<string, UserStatus> _store = new();

    public Task SetOnlineAsync(string userId, string username)
    {
        _store[userId] = new UserStatus
        {
            UserId   = userId,
            Username = username,
            Status   = UserStatusType.Online,
            LastSeen = DateTime.UtcNow
        };
        return Task.CompletedTask;
    }

    public Task SetOfflineAsync(string userId)
    {
        if (_store.TryGetValue(userId, out var s))
        {
            s.Status   = UserStatusType.Offline;
            s.LastSeen = DateTime.UtcNow;
        }
        _store.TryRemove(userId, out _);
        return Task.CompletedTask;
    }

    public Task SetAwayAsync(string userId)
    {
        if (_store.TryGetValue(userId, out var s))
        {
            s.Status   = UserStatusType.Away;
            s.LastSeen = DateTime.UtcNow;
        }
        return Task.CompletedTask;
    }

    public Task<UserStatus> GetStatusAsync(string userId)
    {
        var status = _store.TryGetValue(userId, out var s)
            ? s
            : new UserStatus { UserId = userId, Status = UserStatusType.Offline, LastSeen = DateTime.MinValue };
        return Task.FromResult(status);
    }

    public Task<IEnumerable<UserStatus>> GetOnlineUsersAsync()
    {
        var online = _store.Values
            .Where(s => s.Status != UserStatusType.Offline)
            .ToList();
        return Task.FromResult<IEnumerable<UserStatus>>(online);
    }

    public Task<bool> IsOnlineAsync(string userId)
        => Task.FromResult(_store.ContainsKey(userId));
}
