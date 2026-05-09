using ChatApplication.Client.Models;

namespace ChatApplication.Client.Services;

/// <summary>Shared in-memory state across all Blazor components.</summary>
public class AppState
{
    // ── Rooms ─────────────────────────────────────────────
    public List<ChatRoomDto>  Rooms        { get; set; } = new();
    public string?            ActiveRoomId { get; set; }
    public ChatRoomDto?       ActiveRoom   => Rooms.FirstOrDefault(r => r.Id == ActiveRoomId);
    public Dictionary<string, List<RoomMemberDto>> RoomMembers { get; } = new();

    public List<RoomMemberDto> GetRoomMembers(string roomId)
    {
        if (!RoomMembers.TryGetValue(roomId, out var list))
            RoomMembers[roomId] = list = new();
        return list;
    }

    // ── Messages per room ─────────────────────────────────
    public Dictionary<string, List<MessageDto>> RoomMessages { get; } = new();

    public List<MessageDto> GetRoomMessages(string roomId)
    {
        if (!RoomMessages.TryGetValue(roomId, out var list))
            RoomMessages[roomId] = list = new();
        return list;
    }

    // ── Private messages ──────────────────────────────────
    public string?            ActiveDmUserId   { get; set; }
    public string?            ActiveDmUsername { get; set; }

    // DM thread: key = other userId
    public Dictionary<string, List<PrivateMessage>> DmMessages { get; } = new();

    public List<PrivateMessage> GetDmMessages(string userId)
    {
        if (!DmMessages.TryGetValue(userId, out var list))
            DmMessages[userId] = list = new();
        return list;
    }

    // ── Online users ──────────────────────────────────────
    public List<UserStatusDto> OnlineUsers { get; set; } = new();
    public List<UserStatusDto> Users { get; set; } = new();

    // ── Unread counts ─────────────────────────────────────
    public Dictionary<string, int> UnreadRooms { get; } = new();
    public Dictionary<string, int> UnreadDms   { get; } = new();

    public int TotalUnreadDms => UnreadDms.Values.Sum();

    // ── View mode ─────────────────────────────────────────
    public enum ViewMode { Room, DM }
    public ViewMode CurrentView { get; set; } = ViewMode.Room;

    // ── Typing ────────────────────────────────────────────
    public Dictionary<string, HashSet<string>> TypingInRoom { get; } = new();

    public void SetTyping(string roomId, string userId, bool typing)
    {
        if (!TypingInRoom.TryGetValue(roomId, out var set))
            TypingInRoom[roomId] = set = new();
        if (typing) set.Add(userId); else set.Remove(userId);
    }

    public IEnumerable<string> GetTypingUsers(string roomId)
        => TypingInRoom.TryGetValue(roomId, out var s) ? s : Enumerable.Empty<string>();

    // ── Change notification ───────────────────────────────
    public event Action? OnChange;
    public void Notify() => OnChange?.Invoke();
}
