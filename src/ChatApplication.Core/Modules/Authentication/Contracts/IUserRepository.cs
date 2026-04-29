namespace ChatApplication.Core.Modules.Authentication.Contracts;

/// <summary>
/// Minimal user persistence contract used by the auth layer.
/// </summary>
public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task<User> AddAsync(User user);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
