using System.Text.RegularExpressions;
using ChatApplication.Core.Modules.Authentication.Models;

namespace ChatApplication.Core.Modules.Authentication.Validators;

public class LoginRequestValidator
{
    private static readonly Regex EmailRegex =
        new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public IEnumerable<string> Validate(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            yield return "Email is required.";
        else if (!EmailRegex.IsMatch(request.Email))
            yield return "Email is not valid.";

        if (string.IsNullOrWhiteSpace(request.Password))
            yield return "Password is required.";
    }
}
