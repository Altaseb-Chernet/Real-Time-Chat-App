using System.Security.Claims;
using ChatApplication.Core.Common.Exceptions;
using ChatApplication.Core.Modules.Chat.Contracts;
using ChatApplication.Core.Modules.Chat.Models;
using ChatApplication.Core.Modules.User.Contracts;
using ChatApplication.Infrastructure.SignalR.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ChatApplication.API.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IMessageService _messageService;
    private readonly IChatRoomService _chatRoomService;
    private readonly IUserTracker _userTracker;
    private readonly IConnectionManager _connectionManager;
    private readonly IUserPresenceService _presenceService;

    public ChatHub(
        IMessageService messageService,
        IChatRoomService chatRoomService,
        IUserTracker userTracker,
        IConnectionManager connectionManager,
        IUserPresenceService presenceService)
    {
        _messageService    = messageService;
        _chatRoomService   = chatRoomService;
        _userTracker       = userTracker;
        _connectionManager = connectionManager;
        _presenceService   = presenceService;
    }

    // -------------------------------------------------------------------------
    // Connection lifecycle
    // -------------------------------------------------------------------------

    public override async Task OnConnectedAsync()
    {
        var (userId, username) = GetIdentity();
        await _userTracker.TrackAsync(userId, Context.ConnectionId);
        await _connectionManager.AddConnectionAsync(userId, Context.ConnectionId);
        await _presenceService.SetOnlineAsync(userId, username);

        await Clients.Others.SendAsync(HubEvents.UserOnline, userId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var (userId, _) = GetIdentity();
        await _userTracker.UntrackAsync(Context.ConnectionId);
        await _connectionManager.RemoveConnectionAsync(Context.ConnectionId);

        // Only mark offline when the user has no remaining connections
        var stillOnline = await _userTracker.IsOnlineAsync(userId);
        if (!stillOnline)
        {
            await _presenceService.SetOfflineAsync(userId);
            await Clients.Others.SendAsync(HubEvents.UserOffline, userId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    // -------------------------------------------------------------------------
    // Room operations
    // -------------------------------------------------------------------------

    /// <summary>Join a SignalR group for a chat room and persist membership.</summary>
    public async Task JoinRoom(string roomId)
    {
        var userId = GetUserId();

        var room = await _chatRoomService.GetRoomAsync(roomId)
            ?? throw new HubException($"Room '{roomId}' not found.");

        await Groups.AddToGroupAsync(Context.ConnectionId, RoomGroup(roomId));

        await Clients.Group(RoomGroup(roomId)).SendAsync(HubEvents.UserJoinedRoom, new
        {
            UserId = userId,
            RoomId = roomId,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>Leave a chat room group.</summary>
    public async Task LeaveRoom(string roomId)
    {
        var userId = GetUserId();

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, RoomGroup(roomId));

        await Clients.Group(RoomGroup(roomId)).SendAsync(HubEvents.UserLeftRoom, new
        {
            UserId = userId,
            RoomId = roomId,
            Timestamp = DateTime.UtcNow
        });
    }

    // -------------------------------------------------------------------------
    // Group messaging
    // -------------------------------------------------------------------------

    /// <summary>Send a message to a chat room. Persists to DB and broadcasts to all room members.</summary>
    public async Task SendMessage(string roomId, string content)
    {
        var userId = GetUserId();

        MessageResponse response;
        try
        {
            response = await _messageService.SendMessageAsync(new SendMessageRequest
            {
                Content = content,
                SenderId = userId,
                RoomId = roomId
            });
        }
        catch (AppException ex)
        {
            throw new HubException(ex.Message);
        }

        // Broadcast to everyone in the room group (including sender)
        await Clients.Group(RoomGroup(roomId)).SendAsync(HubEvents.ReceiveMessage, response);
    }

    // -------------------------------------------------------------------------
    // Private messaging
    // -------------------------------------------------------------------------

    /// <summary>Send a direct message to a specific user. Delivered to all their active connections.</summary>
    public async Task SendPrivateMessage(string recipientUserId, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new HubException("Message content cannot be empty.");

        var senderId = GetUserId();
        var senderUsername = GetUsername();

        var payload = new
        {
            SenderId = senderId,
            SenderUsername = senderUsername,
            RecipientId = recipientUserId,
            Content = content,
            SentAt = DateTime.UtcNow
        };

        // Deliver to all recipient connections
        var recipientConnections = await _userTracker.GetConnectionsAsync(recipientUserId);
        foreach (var connectionId in recipientConnections)
            await Clients.Client(connectionId).SendAsync(HubEvents.ReceivePrivateMessage, payload);

        // Echo back to sender's other connections
        var senderConnections = (await _userTracker.GetConnectionsAsync(senderId))
            .Where(c => c != Context.ConnectionId);
        foreach (var connectionId in senderConnections)
            await Clients.Client(connectionId).SendAsync(HubEvents.ReceivePrivateMessage, payload);
    }

    // -------------------------------------------------------------------------
    // Typing indicators
    // -------------------------------------------------------------------------

    public async Task TypingInRoom(string roomId)
    {
        var userId = GetUserId();
        await Clients.OthersInGroup(RoomGroup(roomId))
                     .SendAsync(HubEvents.UserTyping, new { UserId = userId, RoomId = roomId });
    }

    public async Task StoppedTypingInRoom(string roomId)
    {
        var userId = GetUserId();
        await Clients.OthersInGroup(RoomGroup(roomId))
                     .SendAsync(HubEvents.UserStoppedTyping, new { UserId = userId, RoomId = roomId });
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private string GetUserId()
        => Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? Context.User?.FindFirstValue("sub")
        ?? throw new HubException("Unauthorized.");

    private string GetUsername()
        => Context.User?.FindFirstValue(ClaimTypes.Name)
        ?? Context.User?.FindFirstValue("email")
        ?? GetUserId();

    private (string userId, string username) GetIdentity()
    {
        var userId = GetUserId();
        return (userId, GetUsername());
    }

    private static string RoomGroup(string roomId) => $"room:{roomId}";
}

/// <summary>Centralised event name constants shared between hub and clients.</summary>
public static class HubEvents
{
    public const string ReceiveMessage = "ReceiveMessage";
    public const string ReceivePrivateMessage = "ReceivePrivateMessage";
    public const string UserJoinedRoom = "UserJoinedRoom";
    public const string UserLeftRoom = "UserLeftRoom";
    public const string UserOnline = "UserOnline";
    public const string UserOffline = "UserOffline";
    public const string UserTyping = "UserTyping";
    public const string UserStoppedTyping = "UserStoppedTyping";
}
