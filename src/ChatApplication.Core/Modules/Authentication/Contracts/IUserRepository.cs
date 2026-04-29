namespace ChatApplication.Core.Modules.Authentication.Contracts;

/// <summary>
/// Minimal AppUser persistence contract used by the auth layer.
/// </summary>
public interface IUserRepository
{
    Task<AppUser?> GetByEmailAsync(string email);
    Task<AppUser> AddAsync(AppUser user);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
