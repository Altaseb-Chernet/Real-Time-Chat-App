using ChatApplication.Core.Modules.Authentication.Contracts;
using ChatApplication.Core.Modules.Authentication.Models;

namespace ChatApplication.Core.Modules.Authentication.Services;

public class AuthService : IAuthService
{
    private readonly ITokenService _tokenService;

    public AuthService(ITokenService tokenService) => _tokenService = tokenService;

    public Task<AuthResponse> LoginAsync(LoginRequest request) => throw new NotImplementedException();
    public Task<AuthResponse> RegisterAsync(RegisterRequest request) => throw new NotImplementedException();
    public Task LogoutAsync(string userId) => throw new NotImplementedException();
}
