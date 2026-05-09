using ChatApplication.Client.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace ChatApplication.Client.Services;

public class HubService : IAsyncDisposable
{
    private HubConnection? _hub;
    private readonly AuthService _auth;
    private readonly Microsoft.AspNetCore.Components.NavigationManager _nav;

    // Events
    public event Action<MessageDto>?     OnMessage;
    public event Action<PrivateMessage>? OnPrivateMessage;
    public event Action<string, string>? OnUserJoinedRoom;   // userId, roomId
    public event Action<string, string>? OnUserLeftRoom;
    public event Action<string>?         OnUserOnline;
    public event Action<string>?         OnUserOffline;
    public event Action<string, string>? OnUserTyping;       // userId, roomId
    public event Action<string>?         OnUserStoppedTyping;
    public event Action?                 OnReconnected;
    public event Action?                 OnDisconnected;

    public bool IsConnected => _hub?.State == HubConnectionState.Connected;

    public HubService(AuthService auth, Microsoft.AspNetCore.Components.NavigationManager nav)
    {
        _auth = auth;
        _nav = nav;
    }

    public async Task ConnectAsync()
    {
        if (_hub is not null) return;

        _hub = new HubConnectionBuilder()
            .WithUrl(_nav.ToAbsoluteUri("/hubs/chat"), opts =>
            {
                opts.AccessTokenProvider = () => Task.FromResult(_auth.Token);
            })
            .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10) })
            .Build();

        _hub.On<MessageDto>("ReceiveMessage",         m  => OnMessage?.Invoke(m));
        _hub.On<PrivateMessage>("ReceivePrivateMessage", m => OnPrivateMessage?.Invoke(m));
        _hub.On<object>("UserJoinedRoom",  o => { var d = ParseAnon(o); OnUserJoinedRoom?.Invoke(d.userId, d.roomId); });
        _hub.On<object>("UserLeftRoom",    o => { var d = ParseAnon(o); OnUserLeftRoom?.Invoke(d.userId, d.roomId); });
        _hub.On<string>("UserOnline",      id => OnUserOnline?.Invoke(id));
        _hub.On<string>("UserOffline",     id => OnUserOffline?.Invoke(id));
        _hub.On<object>("UserTyping",      o => { var d = ParseAnon(o); OnUserTyping?.Invoke(d.userId, d.roomId); });
        _hub.On<object>("UserStoppedTyping", o => { var d = ParseAnon(o); OnUserStoppedTyping?.Invoke(d.userId); });

        _hub.Reconnected += _ => { OnReconnected?.Invoke(); return Task.CompletedTask; };
        _hub.Closed      += _ => { OnDisconnected?.Invoke(); return Task.CompletedTask; };

        await _hub.StartAsync();
    }

    public Task JoinRoomAsync(string roomId)    => Invoke("JoinRoom", roomId);
    public Task LeaveRoomAsync(string roomId)   => Invoke("LeaveRoom", roomId);
    public Task SendMessageAsync(string roomId, string content) => Invoke("SendMessage", roomId, content);
    public Task SendPrivateMessageAsync(
        string recipientId,
        string content,
        string? mediaUrl = null,
        string? mediaPublicId = null,
        string? mediaType = null,
        string? mediaName = null,
        long? mediaBytes = null)
        => Invoke("SendPrivateMessage", recipientId, content, mediaUrl, mediaPublicId, mediaType, mediaName, mediaBytes);
    public Task TypingAsync(string roomId)      => Invoke("TypingInRoom", roomId);
    public Task StopTypingAsync(string roomId)  => Invoke("StoppedTypingInRoom", roomId);

    private Task Invoke(string method, params object[] args)
        => _hub?.State == HubConnectionState.Connected
            ? _hub.InvokeCoreAsync(method, args)
            : Task.CompletedTask;

    private static (string userId, string roomId) ParseAnon(object o)
    {
        // SignalR sends anonymous objects as JsonElement
        if (o is System.Text.Json.JsonElement el)
        {
            var uid = el.TryGetProperty("userId",  out var u) ? u.GetString() ?? "" : "";
            var rid = el.TryGetProperty("roomId",  out var r) ? r.GetString() ?? "" : "";
            return (uid, rid);
        }
        return ("", "");
    }

    public async ValueTask DisposeAsync()
    {
        if (_hub is not null)
        {
            await _hub.DisposeAsync();
            _hub = null;
        }
    }
}
