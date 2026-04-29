using ChatApplication.Infrastructure.SignalR.Interfaces;

namespace ChatApplication.Infrastructure.SignalR.Services;

public class SignalRUserTracker : IUserTracker
{
    private readonly Dictionary<string, HashSet<string>> _userConnections = new();

    public Task TrackAsync(string userId, string connectionId)
    {
        if (!_userConnections.TryGetValue(userId, out var connections))
            _userConnections[userId] = connections = new HashSet<string>();
        connections.Add(connectionId);
        return Task.CompletedTask;
    }

    public Task UntrackAsync(string connectionId)
    {
        foreach (var connections in _userConnections.Values)
            connections.Remove(connectionId);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<string>> GetConnectionsAsync(string userId)
        => Task.FromResult<IEnumerable<string>>(
            _userConnections.TryGetValue(userId, out var c) ? c : Enumerable.Empty<string>());

    public Task<bool> IsOnlineAsync(string userId)
        => Task.FromResult(_userConnections.TryGetValue(userId, out var c) && c.Count > 0);
}
