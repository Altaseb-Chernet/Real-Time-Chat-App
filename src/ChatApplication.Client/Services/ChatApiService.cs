using System.Net.Http.Json;
using System.Text.Json;
using ChatApplication.Client.Models;

namespace ChatApplication.Client.Services;

public class ChatApiService
{
    private readonly HttpClient _http;

    public ChatApiService(HttpClient http) => _http = http;

    // ── Rooms ─────────────────────────────────────────────
    public async Task<List<ChatRoomDto>> GetRoomsAsync()
    {
        var r = await _http.GetFromJsonAsync<ApiResponse<List<ChatRoomDto>>>("/api/chat/rooms");
        return r?.Data ?? new();
    }

    public async Task<ChatRoomDto?> CreateRoomAsync(string name)
    {
        var res  = await _http.PostAsJsonAsync("/api/chat/rooms", new { name });
        if (!res.IsSuccessStatusCode)
            throw new Exception(await ReadApiErrorAsync(res));

        var body = await res.Content.ReadFromJsonAsync<ApiResponse<ChatRoomDto>>();
        return body?.Data;
    }

    public async Task JoinRoomAsync(string roomId)
        => await _http.PostAsync($"/api/chat/rooms/{roomId}/join", null);

    public async Task LeaveRoomAsync(string roomId)
        => await _http.PostAsync($"/api/chat/rooms/{roomId}/leave", null);

    /// <summary>Deletes the room (creator only, enforced on server).</summary>
    public async Task DeleteRoomAsync(string roomId)
    {
        var res = await _http.DeleteAsync($"/api/chat/rooms/{roomId}");
        if (!res.IsSuccessStatusCode)
            throw new Exception(await ReadApiErrorAsync(res));
    }

    public async Task<List<RoomMemberDto>> GetRoomMembersAsync(string roomId)
    {
        var res = await _http.GetAsync($"/api/chat/rooms/{roomId}/members");
        if (!res.IsSuccessStatusCode)
            throw new Exception(await ReadApiErrorAsync(res));

        var body = await res.Content.ReadFromJsonAsync<ApiResponse<List<RoomMemberDto>>>();
        return body?.Data ?? new();
    }

    public async Task KickMemberAsync(string roomId, string userId)
    {
        var res = await _http.DeleteAsync($"/api/chat/rooms/{roomId}/members/{userId}");
        if (!res.IsSuccessStatusCode)
            throw new Exception(await ReadApiErrorAsync(res));
    }

    // ── Messages ──────────────────────────────────────────
    public async Task<List<MessageDto>> GetMessagesAsync(string roomId, int page = 1, int pageSize = 80)
    {
        var r = await _http.GetFromJsonAsync<ApiResponse<PagedResponse<MessageDto>>>(
            $"/api/chat/rooms/{roomId}/messages?page={page}&pageSize={pageSize}");
        return r?.Data?.Items ?? new();
    }

    public async Task<MessageDto?> SendMessageAsync(string roomId, string content,
        string? mediaUrl = null, string? mediaPublicId = null,
        string? mediaType = null, string? mediaName = null, long? mediaBytes = null)
    {
        var res  = await _http.PostAsJsonAsync($"/api/chat/rooms/{roomId}/messages",
            new { content, mediaUrl, mediaPublicId, mediaType, mediaName, mediaBytes });
        if (!res.IsSuccessStatusCode)
            throw new Exception(await ReadApiErrorAsync(res));
        var body = await res.Content.ReadFromJsonAsync<ApiResponse<MessageDto>>();
        return body?.Data;
    }

    public async Task<MessageDto?> EditMessageAsync(string messageId, string content)
    {
        var res  = await _http.PutAsJsonAsync($"/api/chat/messages/{messageId}", new { content });
        var body = await res.Content.ReadFromJsonAsync<ApiResponse<MessageDto>>();
        return body?.Data;
    }

    public async Task DeleteMessageAsync(string messageId)
    {
        var res = await _http.DeleteAsync($"/api/chat/messages/{messageId}");
        if (!res.IsSuccessStatusCode)
            throw new Exception(await ReadApiErrorAsync(res));
    }

    // ── Users ─────────────────────────────────────────────
    public async Task<List<UserStatusDto>> GetOnlineUsersAsync()
    {
        var r = await _http.GetFromJsonAsync<ApiResponse<List<UserStatusDto>>>("/api/user/online");
        return r?.Data ?? new();
    }

    public async Task<List<UserStatusDto>> GetAllUsersAsync()
    {
        var r = await _http.GetFromJsonAsync<ApiResponse<List<UserStatusDto>>>("/api/user/all");
        return r?.Data ?? new();
    }

    public async Task<List<PrivateMessage>> GetPrivateMessagesAsync(string otherUserId, int take = 200)
    {
        var r = await _http.GetFromJsonAsync<ApiResponse<List<PrivateMessage>>>($"/api/chat/dms/{otherUserId}?take={take}");
        return r?.Data ?? new();
    }

    // ── Media ─────────────────────────────────────────────
    public async Task<MediaUploadResult?> UploadMediaAsync(Stream stream, string fileName, string contentType,
        IProgress<int>? progress = null)
    {
        using var content = new MultipartFormDataContent();
        using var sc      = new StreamContent(stream);
        try
        {
            sc.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(contentType);
        }
        catch
        {
            // Some browsers provide values like "audio/webm;codecs=opus" which may fail parsing in strict cases.
            // Fall back to the base type.
            var baseType = (contentType ?? "application/octet-stream").Split(';')[0].Trim();
            sc.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(baseType);
        }
        content.Add(sc, "file", fileName);

        var res  = await _http.PostAsync("/api/media/upload", content);
        if (!res.IsSuccessStatusCode)
            throw new Exception(await ReadApiErrorAsync(res));

        var body = await res.Content.ReadFromJsonAsync<ApiResponse<MediaUploadResult>>();
        return body?.Data;
    }

    private static async Task<string> ReadApiErrorAsync(HttpResponseMessage res)
    {
        var raw = await res.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(raw))
            return $"Request failed ({(int)res.StatusCode}).";

        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            if (root.TryGetProperty("errors", out var e) && e.ValueKind == JsonValueKind.Array)
            {
                var joined = string.Join(" ",
                    e.EnumerateArray().Select(x => x.GetString()).Where(x => !string.IsNullOrWhiteSpace(x)));
                if (!string.IsNullOrWhiteSpace(joined)) return joined;
            }

            if (root.TryGetProperty("message", out var m))
                return m.GetString() ?? $"Request failed ({(int)res.StatusCode}).";
        }
        catch
        {
            // ignore json parse errors
        }

        return raw;
    }
}
