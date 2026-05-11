using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using ChatApplication.Client.Models;
using Microsoft.JSInterop;

namespace ChatApplication.Client.Services;

public class AuthService
{
    public const string LsRememberedEmail = "rememberedEmail";
    private const string LsLastActivity = "lastActivityUtc";

    /// <summary>Sign-out after this long with no pointer/keyboard activity (while app is open).</summary>
    public static readonly TimeSpan IdleTimeout = TimeSpan.FromMinutes(30);

    private readonly HttpClient _http;
    private readonly IJSRuntime _js;

    private DateTime _lastActivityUtc = DateTime.UtcNow;

    public string? Token    { get; private set; }
    public string? UserId   { get; private set; }
    public string? Username { get; private set; }
    /// <summary>UTC expiry from server (JWT). Null if legacy stored session without expiry.</summary>
    public DateTime? TokenExpiresAtUtc { get; private set; }

    public bool IsAuthenticated => !string.IsNullOrEmpty(Token);

    public AuthService(HttpClient http, IJSRuntime js)
    {
        _http = http;
        _js   = js;
    }

    /// <summary>Call on user interaction so idle timeout does not fire while using the app.</summary>
    public void TouchActivity() => _lastActivityUtc = DateTime.UtcNow;

    /// <summary>Updates activity and persists UTC time (for idle timeout across reloads).</summary>
    public async Task TouchActivityPersistedAsync()
    {
        TouchActivity();
        try
        {
            await _js.InvokeVoidAsync("localStorage.setItem", LsLastActivity,
                _lastActivityUtc.ToString("o", CultureInfo.InvariantCulture));
        }
        catch { /* ignore */ }
    }

    /// <summary>True when a token exists, JWT is not expired, and user has not been idle too long.</summary>
    public bool IsSessionValid()
    {
        if (string.IsNullOrEmpty(Token)) return false;
        if (!TokenExpiresAtUtc.HasValue) return false;
        if (DateTime.UtcNow >= TokenExpiresAtUtc.Value) return false;
        if (DateTime.UtcNow - _lastActivityUtc > IdleTimeout) return false;
        return true;
    }

    public async Task InitAsync()
    {
        Token    = await _js.InvokeAsync<string?>("localStorage.getItem", "token");
        UserId   = await _js.InvokeAsync<string?>("localStorage.getItem", "userId");
        Username = await _js.InvokeAsync<string?>("localStorage.getItem", "username");
        var expRaw = await _js.InvokeAsync<string?>("localStorage.getItem", "tokenExpiresAt");
        if (DateTime.TryParse(expRaw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var exp))
            TokenExpiresAtUtc = exp.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(exp, DateTimeKind.Utc)
                : exp.ToUniversalTime();
        else
            TokenExpiresAtUtc = null;

        var lastStr = await _js.InvokeAsync<string?>("localStorage.getItem", LsLastActivity);
        if (DateTime.TryParse(lastStr, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var lastAct))
        {
            var lastUtc = lastAct.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(lastAct, DateTimeKind.Utc)
                : lastAct.ToUniversalTime();
            _lastActivityUtc = lastUtc;
            if (!string.IsNullOrEmpty(Token) && DateTime.UtcNow - lastUtc > IdleTimeout)
            {
                await ClearAuthOnlyAsync();
                return;
            }
        }
        else
            _lastActivityUtc = DateTime.UtcNow;

        // Legacy sessions (no expiry) cause API errors — clear auth but keep remembered email.
        if (!string.IsNullOrEmpty(Token) && !TokenExpiresAtUtc.HasValue)
        {
            await ClearAuthOnlyAsync();
            return;
        }

        if (!string.IsNullOrEmpty(Token))
            _http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Token);

        await TouchActivityPersistedAsync();
    }

    public async Task<string?> GetRememberedEmailAsync()
    => await _js.InvokeAsync<string?>("localStorage.getItem", LsRememberedEmail);

    public async Task SetRememberedEmailAsync(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            await _js.InvokeVoidAsync("localStorage.removeItem", LsRememberedEmail);
        else
            await _js.InvokeVoidAsync("localStorage.setItem", LsRememberedEmail, email.Trim());
    }

    /// <summary>Removes auth tokens from memory and storage; preserves <see cref="LsRememberedEmail"/>.</summary>
    public async Task ClearAuthOnlyAsync()
    {
        Token = UserId = Username = null;
        TokenExpiresAtUtc = null;
        _http.DefaultRequestHeaders.Authorization = null;
        await _js.InvokeVoidAsync("localStorage.removeItem", "token");
        await _js.InvokeVoidAsync("localStorage.removeItem", "userId");
        await _js.InvokeVoidAsync("localStorage.removeItem", "username");
        await _js.InvokeVoidAsync("localStorage.removeItem", "tokenExpiresAt");
        await _js.InvokeVoidAsync("localStorage.removeItem", LsLastActivity);
    }

    public async Task<(bool ok, string? error)> LoginAsync(string email, string password)
    {
        var res = await _http.PostAsJsonAsync("/api/auth/login", new { email, password });
        var raw = await res.Content.ReadAsStringAsync();
        if (!res.IsSuccessStatusCode)
            return (false, TryReadError(raw));

        var body = JsonSerializer.Deserialize<AuthResponse>(raw, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        if (body is null) return (false, "Invalid login response.");
        await SaveSession(body);
        return (true, null);
    }

    public async Task<(bool ok, string? error)> RegisterAsync(string username, string email, string password)
    {
        var res = await _http.PostAsJsonAsync("/api/auth/register", new { username, email, password });
        var raw = await res.Content.ReadAsStringAsync();
        if (!res.IsSuccessStatusCode)
            return (false, TryReadError(raw));

        var body = JsonSerializer.Deserialize<AuthResponse>(raw, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        if (body is null) return (false, "Invalid register response.");
        await SaveSession(body);
        return (true, null);
    }

    public async Task LogoutAsync()
    {
        await ClearAuthOnlyAsync();
    }

    private async Task SaveSession(AuthResponse r)
    {
        Token    = r.Token;
        UserId   = r.UserId;
        Username = r.Username;
        TokenExpiresAtUtc = r.ExpiresAt.Kind == DateTimeKind.Utc
            ? r.ExpiresAt
            : r.ExpiresAt.ToUniversalTime();
        _http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Token);
        await _js.InvokeVoidAsync("localStorage.setItem", "token",    Token);
        await _js.InvokeVoidAsync("localStorage.setItem", "userId",   UserId);
        await _js.InvokeVoidAsync("localStorage.setItem", "username", Username);
        await _js.InvokeVoidAsync("localStorage.setItem", "tokenExpiresAt",
            TokenExpiresAtUtc.Value.ToString("o", CultureInfo.InvariantCulture));
        await TouchActivityPersistedAsync();
    }

    private static string TryReadError(string raw)
    {
        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            // Prefer detailed validation errors over generic "Validation failed"
            if (root.TryGetProperty("errors", out var e) && e.ValueKind == JsonValueKind.Array)
            {
                var joined = string.Join(" ",
                    e.EnumerateArray().Select(x => x.GetString()).Where(x => !string.IsNullOrWhiteSpace(x)));
                if (!string.IsNullOrWhiteSpace(joined)) return joined;
            }

            if (root.TryGetProperty("message", out var m)) return m.GetString() ?? "Error";
            if (!string.IsNullOrWhiteSpace(raw)) return raw;
        }
        catch { }
        return "An error occurred.";
    }
}
