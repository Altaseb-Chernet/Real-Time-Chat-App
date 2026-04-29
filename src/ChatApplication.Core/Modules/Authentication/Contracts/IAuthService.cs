using ChatApplication.Core.Modules.Authentication.Models;

namespace ChatApplication.Core.Modules.Authentication.Contracts;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task LogoutAsync(string userId);
}
