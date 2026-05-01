using System.Collections.Concurrent;
using ChatApplication.Infrastructure.SignalR.Interfaces;

namespace ChatApplication.Infrastructure.SignalR.Services;

public class InMemoryConnectionManager : IConnectionManager
{
    private readonly ConcurrentDictionary<string, string> _connToUser = new();

    public Task AddConnectionAsync(string userId, string connectionId)
    {
        _connToUser[connectionId] = userId;
        return Task.CompletedTask;
    }

    public Task RemoveConnectionAsync(string connectionId)
    {
        _connToUser.TryRemove(connectionId, out _);
        return Task.CompletedTask;
    }

    public Task<string?> GetUserIdAsync(string connectionId)
    {
        _connToUser.TryGetValue(connectionId, out var userId);
        return Task.FromResult(userId);
    }
}
