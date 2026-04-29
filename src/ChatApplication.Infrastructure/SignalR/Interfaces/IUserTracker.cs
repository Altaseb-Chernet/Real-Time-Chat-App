namespace ChatApplication.Infrastructure.SignalR.Interfaces;

public interface IUserTracker
{
    Task TrackAsync(string userId, string connectionId);
    Task UntrackAsync(string connectionId);
    Task<IEnumerable<string>> GetConnectionsAsync(string userId);
    Task<bool> IsOnlineAsync(string userId);
}
