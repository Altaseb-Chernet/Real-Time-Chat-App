using System.Collections.Concurrent;
using ChatApplication.Infrastructure.SignalR.Interfaces;

namespace ChatApplication.Infrastructure.SignalR.Services;

public class InMemoryUserTracker : IUserTracker
{
    private readonly ConcurrentDictionary<string, HashSet<string>> _userConns = new();
    private readonly object _lock = new();

    public Task TrackAsync(string userId, string connectionId)
    {
        lock (_lock)
        {
            if (!_userConns.TryGetValue(userId, out var set))
                _userConns[userId] = set = [];
            set.Add(connectionId);
        }
        return Task.CompletedTask;
    }

    public Task UntrackAsync(string connectionId)
    {
        lock (_lock)
        {
            foreach (var set in _userConns.Values)
                set.Remove(connectionId);
        }
        return Task.CompletedTask;
    }

    public Task<IEnumerable<string>> GetConnectionsAsync(string userId)
    {
        lock (_lock)
        {
            var result = _userConns.TryGetValue(userId, out var set)
                ? (IEnumerable<string>)set.ToList()
                : [];
            return Task.FromResult(result);
        }
    }

    public Task<bool> IsOnlineAsync(string userId)
    {
        lock (_lock)
        {
            return Task.FromResult(_userConns.TryGetValue(userId, out var s) && s.Count > 0);
        }
    }
}
