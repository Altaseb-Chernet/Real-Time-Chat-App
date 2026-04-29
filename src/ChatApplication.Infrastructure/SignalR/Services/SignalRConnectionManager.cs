using ChatApplication.Infrastructure.SignalR.Interfaces;

namespace ChatApplication.Infrastructure.SignalR.Services;

public class SignalRConnectionManager : IConnectionManager
{
    private readonly Dictionary<string, string> _connections = new();

    public Task AddConnectionAsync(string userId, string connectionId)
    {
        _connections[connectionId] = userId;
        return Task.CompletedTask;
    }

    public Task RemoveConnectionAsync(string connectionId)
    {
        _connections.Remove(connectionId);
        return Task.CompletedTask;
    }

    public Task<string?> GetUserIdAsync(string connectionId)
        => Task.FromResult(_connections.TryGetValue(connectionId, out var userId) ? userId : null);
}
