using System.Threading;
using ChatApplication.Client.Models;
using ChatApplication.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace ChatApplication.Client.Pages;

public partial class Chat : IAsyncDisposable
{
    // ── State ─────────────────────────────────────────────
    string searchQuery  = "";
    string inputText    = "";
    string? editingMsgId;
    bool   showNewRoomModal;
    string newRoomName  = "";
    string? roomError;
    string? lightboxUrl;
    bool   uploading;
    bool   sending;
    string uploadFileName = "";
    int    uploadPct;
    bool   recording;
    MediaUploadDraft? pendingMedia;
    ElementReference inputRef;
    System.Timers.Timer? typingTimer;

    bool sidebarOpen;
    bool showDeleteRoomModal;
    bool deleteRoomBusy;
    string? deleteRoomError;
    bool _sessionLoopStarted;
    CancellationTokenSource? _sessionCts;

    public bool IsRoomCreator =>
        State.CurrentView == AppState.ViewMode.Room
        && State.ActiveRoom != null
        && State.ActiveRoom.CreatedByUserId == Auth.UserId;

    record ToastItem(string Message, string Type, string Icon, bool Visible = true);
    List<ToastItem> toasts = new();

    IEnumerable<ChatRoomDto> FilteredRooms => string.IsNullOrWhiteSpace(searchQuery)
        ? State.Rooms
        : State.Rooms.Where(r => r.Name.Contains(searchQuery, StringComparison.OrdinalIgnoreCase));

    List<string> TypingUsernames => State.CurrentView == AppState.ViewMode.Room && State.ActiveRoomId != null
        ? State.GetTypingUsers(State.ActiveRoomId)
               .Where(id => id != Auth.UserId)
               .Select(id => State.OnlineUsers.FirstOrDefault(u => u.UserId == id)?.Username ?? "Someone")
               .Distinct()
               .ToList()
        : new List<string>();

    string InputPlaceholder => State.CurrentView == AppState.ViewMode.DM
        ? (pendingMedia != null ? "Add a caption…" : $"Message {State.ActiveDmUsername}…")
        : State.ActiveRoom != null
            ? (pendingMedia != null ? "Add a caption…" : $"Message #{State.ActiveRoom.Name}…")
            : "Select a room…";

    // ── Init ──────────────────────────────────────────────
    protected override async Task OnInitializedAsync()
    {
        await Auth.InitAsync();
        if (!Auth.IsAuthenticated) { Nav.NavigateTo("/"); return; }
        if (!Auth.IsSessionValid())
        {
            await Auth.ClearAuthOnlyAsync();
            Nav.NavigateTo("/");
            return;
        }

        State.OnChange += StateHasChanged;

        // Load rooms and online users
        State.Rooms       = await Api.GetRoomsAsync();
        State.Users       = await Api.GetAllUsersAsync();
        State.OnlineUsers = await Api.GetOnlineUsersAsync();

        // Connect SignalR
        Hub.OnMessage          += OnMessage;
        Hub.OnPrivateMessage   += OnPrivateMessage;
        Hub.OnUserOnline       += async _ => await RefreshUsersAndPresence();
        Hub.OnUserOffline      += async _ => await RefreshUsersAndPresence();
        Hub.OnUserTyping       += (uid, rid) => { State.SetTyping(rid, uid, true);  State.Notify(); };
        Hub.OnUserStoppedTyping += uid => { foreach (var r in State.TypingInRoom.Keys.ToList()) State.SetTyping(r, uid, false); State.Notify(); };
        Hub.OnReconnected      += async () => { Toast("Reconnected ✓", "success"); if (State.ActiveRoomId != null) await Hub.JoinRoomAsync(State.ActiveRoomId); };
        Hub.OnDisconnected     += () => Toast("Disconnected", "error");

        await Hub.ConnectAsync();
        State.Notify();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender || _sessionLoopStarted) return;
        _sessionLoopStarted = true;
        _sessionCts = new CancellationTokenSource();
        _ = RunSessionWatchAsync(_sessionCts.Token);
        await Task.CompletedTask;
    }

    async Task RunSessionWatchAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(45), ct);
                if (Auth.IsAuthenticated && !Auth.IsSessionValid())
                {
                    await Auth.ClearAuthOnlyAsync();
                    await InvokeAsync(() =>
                    {
                        Toast("Signed out — session expired or idle timeout.", "info");
                        Nav.NavigateTo("/");
                    });
                    break;
                }
            }
        }
        catch (OperationCanceledException) { /* normal */ }
    }

    async Task OnPointerActivity(PointerEventArgs _)
    {
        Auth.TouchActivity();
        await Auth.TouchActivityPersistedAsync();
    }

    void OpenSidebar()
    {
        sidebarOpen = true;
        StateHasChanged();
    }

    void CloseSidebar()
    {
        sidebarOpen = false;
        StateHasChanged();
    }

    void OpenDeleteRoomModal()
    {
        deleteRoomError = null;
        showDeleteRoomModal = true;
        StateHasChanged();
    }

    void CloseDeleteRoomModal()
    {
        if (deleteRoomBusy) return;
        showDeleteRoomModal = false;
        StateHasChanged();
    }

    async Task ConfirmDeleteRoom()
    {
        if (State.ActiveRoomId == null || !IsRoomCreator) return;
        deleteRoomBusy = true;
        deleteRoomError = null;
        StateHasChanged();
        var id = State.ActiveRoomId;
        try
        {
            await Hub.LeaveRoomAsync(id);
            await Api.DeleteRoomAsync(id);
            State.ActiveRoomId = null;
            State.RoomMessages.Remove(id);
            State.RoomMembers.Remove(id);
            State.UnreadRooms.Remove(id);
            State.Rooms = await Api.GetRoomsAsync();
            showDeleteRoomModal = false;
            Toast("Room deleted.", "success");
            State.Notify();
        }
        catch (Exception ex)
        {
            deleteRoomError = ex.Message ?? "Could not delete room.";
            Toast(deleteRoomError, "error");
        }
        finally
        {
            deleteRoomBusy = false;
            StateHasChanged();
        }
    }

    // ── Message handlers ──────────────────────────────────
    void OnMessage(MessageDto msg)
    {
        var list = State.GetRoomMessages(msg.RoomId);
        if (!list.Any(m => m.Id == msg.Id)) list.Add(msg);

        if (msg.RoomId != State.ActiveRoomId || State.CurrentView != AppState.ViewMode.Room)
        {
            State.UnreadRooms[msg.RoomId] = (State.UnreadRooms.TryGetValue(msg.RoomId, out var u) ? u : 0) + 1;
        }
        State.Notify();
        _ = ScrollToBottom();
    }

    void OnPrivateMessage(PrivateMessage msg)
    {
        var otherId = msg.SenderId == Auth.UserId ? msg.RecipientId : msg.SenderId;
        var list    = State.GetDmMessages(otherId);
        if (!list.Any(m => m.Id == msg.Id)) list.Add(msg);

        if (State.ActiveDmUserId != otherId || State.CurrentView != AppState.ViewMode.DM)
        {
            State.UnreadDms[otherId] = (State.UnreadDms.TryGetValue(otherId, out var u) ? u : 0) + 1;
            var preview = string.IsNullOrWhiteSpace(msg.Content) ? (msg.MediaName ?? "Attachment") : msg.Content;
            preview = preview.Length > 50 ? preview[..50] : preview;
            Toast($"💬 {msg.SenderUsername}: {preview}", "info");
        }
        State.Notify();
        _ = ScrollToBottom();
    }

    // ── Room selection ────────────────────────────────────
    async Task SelectRoom(ChatRoomDto room)
    {
        if (State.ActiveRoomId == room.Id && State.CurrentView == AppState.ViewMode.Room) return;

        if (State.ActiveRoomId != null) await Hub.LeaveRoomAsync(State.ActiveRoomId);

        State.CurrentView  = AppState.ViewMode.Room;
        State.ActiveRoomId = room.Id;
        State.ActiveDmUserId = null;
        State.UnreadRooms.Remove(room.Id);

        if (!State.RoomMessages.ContainsKey(room.Id))
        {
            var msgs = await Api.GetMessagesAsync(room.Id);
            State.RoomMessages[room.Id] = msgs;
        }

        // Load members (for admin kick UI)
        try
        {
            State.RoomMembers[room.Id] = await Api.GetRoomMembersAsync(room.Id);
        }
        catch { /* ignore; UI can still work */ }

        await Hub.JoinRoomAsync(room.Id);
        CloseSidebar();
        State.Notify();
        await ScrollToBottom();
        await FocusInput();
    }

    async Task RefreshMessages()
    {
        if (State.ActiveRoomId == null) return;
        var msgs = await Api.GetMessagesAsync(State.ActiveRoomId);
        State.RoomMessages[State.ActiveRoomId] = msgs;
        try
        {
            State.RoomMembers[State.ActiveRoomId] = await Api.GetRoomMembersAsync(State.ActiveRoomId);
        }
        catch { }
        State.Notify();
        await ScrollToBottom();
    }

    async Task KickUser(string userId)
    {
        if (State.ActiveRoomId == null) return;
        try
        {
            await Api.KickMemberAsync(State.ActiveRoomId, userId);
            State.RoomMembers[State.ActiveRoomId] = await Api.GetRoomMembersAsync(State.ActiveRoomId);
            Toast("User removed", "success");
            State.Notify();
        }
        catch (Exception ex)
        {
            Toast(ex.Message, "error");
        }
    }

    async Task LeaveRoom()
    {
        if (State.ActiveRoomId == null) return;
        await Api.LeaveRoomAsync(State.ActiveRoomId);
        await Hub.LeaveRoomAsync(State.ActiveRoomId);
        State.ActiveRoomId = null;
        State.Rooms = await Api.GetRoomsAsync();
        State.Notify();
    }

    // ── DM ────────────────────────────────────────────────
    async Task OpenDm(UserStatusDto user)
    {
        CloseSidebar();
        State.CurrentView    = AppState.ViewMode.DM;
        State.ActiveDmUserId = user.UserId;
        State.ActiveDmUsername = user.Username;
        State.UnreadDms.Remove(user.UserId);

        if (!State.DmMessages.ContainsKey(user.UserId))
        {
            State.DmMessages[user.UserId] = await Api.GetPrivateMessagesAsync(user.UserId);
        }

        State.Notify();
        await ScrollToBottom();
        await FocusInput();
    }

    async Task OpenDmFromPanel(UserStatusDto user)
    {
        if (user.UserId == Auth.UserId) return;
        await OpenDm(user);
    }

    // ── Send ──────────────────────────────────────────────
    async Task SendOrEdit()
    {
        if (editingMsgId != null) { await SubmitEdit(); return; }
        if (sending) return;

        var text = inputText.Trim();
        var hasMedia = pendingMedia is not null;
        if (string.IsNullOrEmpty(text) && !hasMedia) return;

        StopTypingTimer();
        sending = true;

        try
        {
            if (State.CurrentView == AppState.ViewMode.Room && State.ActiveRoomId != null)
            {
                // Use REST for sending so we support both text + attachments.
                // The API broadcasts via SignalR; do not add optimistically to avoid duplicates.
                var content = string.IsNullOrEmpty(text)
                    ? (pendingMedia?.FileName ?? "")
                    : text;

                await Api.SendMessageAsync(
                    State.ActiveRoomId,
                    content,
                    pendingMedia?.Url,
                    pendingMedia?.PublicId,
                    pendingMedia?.MediaType,
                    pendingMedia?.FileName,
                    pendingMedia?.Bytes);

                inputText = "";
                pendingMedia = null;
            }
            else if (State.CurrentView == AppState.ViewMode.DM && State.ActiveDmUserId != null)
            {
                var content = string.IsNullOrEmpty(text)
                    ? (pendingMedia?.FileName ?? "")
                    : text;

                var media = pendingMedia;
                await Hub.SendPrivateMessageAsync(
                    State.ActiveDmUserId,
                    content,
                    media?.Url,
                    media?.PublicId,
                    media?.MediaType,
                    media?.FileName,
                    media?.Bytes);

                inputText = "";
                pendingMedia = null;
            }
        }
        catch (Exception ex)
        {
            Toast("Send failed: " + ex.Message, "error");
        }
        finally
        {
            sending = false;
            StateHasChanged();
        }
    }

    void HandleKey(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && !e.ShiftKey) { _ = SendOrEdit(); return; }
        if (e.Key == "Escape" && editingMsgId != null) { CancelEdit(); return; }

        // Typing indicator
        if (State.CurrentView == AppState.ViewMode.Room && State.ActiveRoomId != null)
        {
            _ = Hub.TypingAsync(State.ActiveRoomId);
            ResetTypingTimer();
        }
    }

    void ResetTypingTimer()
    {
        typingTimer?.Stop();
        typingTimer ??= new System.Timers.Timer(2500) { AutoReset = false };
        typingTimer.Elapsed += (_, _) =>
        {
            if (State.ActiveRoomId != null) _ = Hub.StopTypingAsync(State.ActiveRoomId);
        };
        typingTimer.Start();
    }

    void StopTypingTimer() { typingTimer?.Stop(); }

    // ── Edit ──────────────────────────────────────────────
    void StartEdit(string msgId)
    {
        editingMsgId = msgId;
        var msg = State.GetRoomMessages(State.ActiveRoomId!).FirstOrDefault(m => m.Id == msgId);
        if (msg != null) inputText = msg.Content;
        StateHasChanged();
        _ = FocusInput();
    }

    async Task SubmitEdit()
    {
        var text = inputText.Trim();
        if (string.IsNullOrEmpty(text) || editingMsgId == null) return;
        var updated = await Api.EditMessageAsync(editingMsgId, text);
        if (updated != null)
        {
            var list = State.GetRoomMessages(State.ActiveRoomId!);
            var idx  = list.FindIndex(m => m.Id == editingMsgId);
            if (idx >= 0) list[idx] = updated;
        }
        CancelEdit();
        State.Notify();
    }

    void CancelEdit() { editingMsgId = null; inputText = ""; StateHasChanged(); }

    // ── Delete ────────────────────────────────────────────
    async Task DeleteMsg(string msgId)
    {
        await Api.DeleteMessageAsync(msgId);
        var list = State.GetRoomMessages(State.ActiveRoomId!);
        var idx  = list.FindIndex(m => m.Id == msgId);
        if (idx >= 0)
        {
            var old = list[idx];
            list[idx] = old with { IsDeleted = true, Content = "🚫 This message was deleted" };
        }
        State.Notify();
    }

    async Task CopyMsg(string text)
    {
        await JS.InvokeVoidAsync("navigator.clipboard.writeText", text);
        Toast("Copied!", "success");
    }

    // ── File upload ───────────────────────────────────────

    async Task HandleFileUpload(InputFileChangeEventArgs e)
    {
        var file = e.File;
        if (file.Size > 50 * 1024 * 1024) { Toast("File exceeds 50 MB", "error"); return; }

        uploading      = true;
        uploadFileName = file.Name;
        uploadPct      = 0;
        StateHasChanged();

        try
        {
            await using var stream = file.OpenReadStream(50 * 1024 * 1024);
            var result = await Api.UploadMediaAsync(stream, file.Name, file.ContentType);
            if (result == null) { Toast("Upload failed", "error"); return; }

            uploadPct = 100;
            StateHasChanged();

            pendingMedia = new MediaUploadDraft(
                result.Url,
                result.PublicId,
                // If API returns "raw" but file is audio/image/video, keep the more specific type when possible
                (result.MediaType ?? "raw"),
                result.FileName,
                result.Bytes);

            Toast("Attachment ready ✓ Add a caption (optional) and press Send.", "success");
            await FocusInput();
        }
        catch (Exception ex) { Toast("Upload error: " + ex.Message, "error"); }
        finally { uploading = false; uploadPct = 0; StateHasChanged(); }
    }

    // ── Voice recording ───────────────────────────────────
    async Task ToggleRecording()
    {
        if (recording)
        {
            await StopAndSendVoiceNote();
            return;
        }

        try
        {
            await JS.InvokeVoidAsync("voice.startVoiceRecording");
            recording = true;
            StateHasChanged();
            Toast("Recording… click again to send", "info");
        }
        catch (Exception ex)
        {
            Toast("Mic error: " + ex.Message, "error");
        }
    }

    async Task StopAndSendVoiceNote()
    {
        recording = false;
        StateHasChanged();

        try
        {
            var rec = await JS.InvokeAsync<VoiceRec>("voice.stopVoiceRecording");
            var bytes = Convert.FromBase64String(rec.base64);
            if (bytes.Length < 512)
            {
                Toast("Recorded audio is empty. Check microphone permission and try again.", "error");
                return;
            }

            uploading = true;
            uploadFileName = rec.fileName;
            uploadPct = 0;
            StateHasChanged();

            await using var ms = new MemoryStream(bytes);
            var result = await Api.UploadMediaAsync(ms, rec.fileName, rec.mimeType);
            if (result == null) { Toast("Upload failed", "error"); return; }

            uploadPct = 100;
            StateHasChanged();

            pendingMedia = new MediaUploadDraft(
                result.Url,
                result.PublicId,
                "audio",
                result.FileName,
                result.Bytes);

            Toast("Voice note ready ✓ Add a caption (optional) and press Send.", "success");
            await FocusInput();
        }
        catch (Exception ex)
        {
            Toast("Recording/upload error: " + ex.Message, "error");
            try { await JS.InvokeVoidAsync("voice.cancelVoiceRecording"); } catch { }
        }
        finally
        {
            uploading = false;
            uploadPct = 0;
            StateHasChanged();
        }
    }

    // ── Rooms CRUD ────────────────────────────────────────
    void OpenNewRoomModal()  { showNewRoomModal = true; newRoomName = ""; roomError = null; }
    void CloseNewRoomModal() { showNewRoomModal = false; }

    async Task CreateRoom()
    {
        if (string.IsNullOrWhiteSpace(newRoomName)) return;
        ChatRoomDto? room = null;
        try
        {
            room = await Api.CreateRoomAsync(newRoomName.Trim());
        }
        catch (Exception ex)
        {
            roomError = ex.Message;
            return;
        }

        if (room == null) { roomError = "Failed to create room."; return; }
        State.Rooms = await Api.GetRoomsAsync();
        CloseNewRoomModal();
        Toast($"Room #{room.Name} created!", "success");
        await SelectRoom(room);
    }

    // ── Lightbox ──────────────────────────────────────────
    void OpenLightbox(string url) { lightboxUrl = url; StateHasChanged(); }
    void CloseLightbox()          { lightboxUrl = null; StateHasChanged(); }

    // ── Toast ─────────────────────────────────────────────
    void Toast(string msg, string type)
    {
        var icon = type == "success" ? "✓" : type == "error" ? "✕" : "ℹ";
        var t    = new ToastItem(msg, type, icon);
        toasts.Add(t);
        StateHasChanged();
        _ = Task.Delay(3500).ContinueWith(_ => { toasts.Remove(t); InvokeAsync(StateHasChanged); });
    }

    // ── Helpers ───────────────────────────────────────────
    async Task ScrollToBottom()
    {
        await Task.Delay(50);
        await JS.InvokeVoidAsync("scrollToBottom", "messages-area");
    }

    async Task FocusInput()
    {
        await Task.Delay(50);
        await inputRef.FocusAsync();
    }

    async Task Logout()
    {
        await Auth.LogoutAsync();
        Nav.NavigateTo("/");
    }

    public async ValueTask DisposeAsync()
    {
        try { _sessionCts?.Cancel(); } catch { /* ignore */ }
        _sessionCts?.Dispose();
        _sessionCts = null;
        State.OnChange -= StateHasChanged;
        Hub.OnMessage          -= OnMessage;
        Hub.OnPrivateMessage   -= OnPrivateMessage;
        typingTimer?.Dispose();
        await Hub.DisposeAsync();
    }

    // JS payload shape from `wwwroot/js/voice.js`
    private class VoiceRec
    {
        public string base64 { get; set; } = "";
        public string mimeType { get; set; } = "audio/webm";
        public string fileName { get; set; } = "voice.webm";
        public int bytes { get; set; }
    }

    private record MediaUploadDraft(string Url, string PublicId, string MediaType, string FileName, long Bytes);

    async Task RefreshUsersAndPresence()
    {
        State.Users = await Api.GetAllUsersAsync();
        State.OnlineUsers = await Api.GetOnlineUsersAsync();
        State.Notify();
    }

    string AvatarInitial(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "?";
        return name.Trim()[0].ToString().ToUpperInvariant();
    }
}
