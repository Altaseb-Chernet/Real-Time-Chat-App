using ChatApplication.Core.Modules.Authentication.Models;

namespace ChatApplication.Core.Modules.Authentication.Validators;

public class RegisterRequestValidator
{
    public IEnumerable<string> Validate(RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username)) yield return "Username is required.";
        if (string.IsNullOrWhiteSpace(request.Email)) yield return "Email is required.";
        if (string.IsNullOrWhiteSpace(request.Password)) yield return "Password is required.";
    }
}
