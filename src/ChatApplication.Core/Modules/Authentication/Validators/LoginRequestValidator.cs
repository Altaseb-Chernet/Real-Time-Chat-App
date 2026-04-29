using ChatApplication.Core.Modules.Authentication.Models;

namespace ChatApplication.Core.Modules.Authentication.Validators;

public class LoginRequestValidator
{
    public IEnumerable<string> Validate(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email)) yield return "Email is required.";
        if (string.IsNullOrWhiteSpace(request.Password)) yield return "Password is required.";
    }
}
