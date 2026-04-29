namespace ChatApplication.Infrastructure.SignalR.Interfaces;

public interface IConnectionManager
{
    Task AddConnectionAsync(string userId, string connectionId);
    Task RemoveConnectionAsync(string connectionId);
    Task<string?> GetUserIdAsync(string connectionId);
}
