using System.Net.Http.Json;
using System.Text.Json;
using ChatApplication.Client.Models;
using Microsoft.JSInterop;

namespace ChatApplication.Client.Services;

public class AuthService
{
    private readonly HttpClient _http;
    private readonly IJSRuntime _js;

    public string? Token    { get; private set; }
    public string? UserId   { get; private set; }
    public string? Username { get; private set; }
    public bool    IsAuthenticated => !string.IsNullOrEmpty(Token);

    public AuthService(HttpClient http, IJSRuntime js)
    {
        _http = http;
        _js   = js;
    }

    public async Task InitAsync()
    {
        Token    = await _js.InvokeAsync<string?>("localStorage.getItem", "token");
        UserId   = await _js.InvokeAsync<string?>("localStorage.getItem", "userId");
        Username = await _js.InvokeAsync<string?>("localStorage.getItem", "username");
        if (!string.IsNullOrEmpty(Token))
            _http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Token);
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
        Token = UserId = Username = null;
        _http.DefaultRequestHeaders.Authorization = null;
        await _js.InvokeVoidAsync("localStorage.clear");
    }

    private async Task SaveSession(AuthResponse r)
    {
        Token    = r.Token;
        UserId   = r.UserId;
        Username = r.Username;
        _http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Token);
        await _js.InvokeVoidAsync("localStorage.setItem", "token",    Token);
        await _js.InvokeVoidAsync("localStorage.setItem", "userId",   UserId);
        await _js.InvokeVoidAsync("localStorage.setItem", "username", Username);
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
