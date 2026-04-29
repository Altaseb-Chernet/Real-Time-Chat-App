using System.Security.Claims;
using ChatApplication.Core.Modules.User.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ChatApplication.API.Hubs;

[Authorize]
public class PresenceHub : Hub
{
    private readonly IUserPresenceService _presenceService;

    public PresenceHub(IUserPresenceService presenceService)
        => _presenceService = presenceService;

    // -------------------------------------------------------------------------
    // Connection lifecycle
    // -------------------------------------------------------------------------

    public override async Task OnConnectedAsync()
    {
        var (userId, username) = GetIdentity();
        await _presenceService.SetOnlineAsync(userId, username);

        // Send the caller the current online user list
        var onlineUsers = await _presenceService.GetOnlineUsersAsync();
        await Clients.Caller.SendAsync(PresenceEvents.OnlineUsers, onlineUsers);

        // Notify everyone else
        await Clients.Others.SendAsync(PresenceEvents.UserOnline, new
        {
            UserId   = userId,
            Username = username
        });

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var (userId, _) = GetIdentity();
        await _presenceService.SetOfflineAsync(userId);

        await Clients.Others.SendAsync(PresenceEvents.UserOffline, userId);

        await base.OnDisconnectedAsync(exception);
    }

    // -------------------------------------------------------------------------
    // Client-callable methods
    // -------------------------------------------------------------------------

    /// <summary>Mark the caller as Away.</summary>
    public async Task SetAway()
    {
        var (userId, _) = GetIdentity();
        await _presenceService.SetAwayAsync(userId);
        await Clients.Others.SendAsync(PresenceEvents.UserAway, userId);
    }

    /// <summary>Return from Away back to Online.</summary>
    public async Task SetOnline()
    {
        var (userId, username) = GetIdentity();
        await _presenceService.SetOnlineAsync(userId, username);
        await Clients.Others.SendAsync(PresenceEvents.UserOnline, new { UserId = userId, Username = username });
    }

    /// <summary>Get the current status of a specific user.</summary>
    public async Task GetUserStatus(string userId)
    {
        var status = await _presenceService.GetStatusAsync(userId);
        await Clients.Caller.SendAsync(PresenceEvents.UserStatus, status);
    }

    /// <summary>Get all currently online/away users.</summary>
    public async Task GetOnlineUsers()
    {
        var users = await _presenceService.GetOnlineUsersAsync();
        await Clients.Caller.SendAsync(PresenceEvents.OnlineUsers, users);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private (string userId, string username) GetIdentity()
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? Context.User?.FindFirstValue("sub")
                  ?? throw new HubException("Unauthorized.");

        var username = Context.User?.FindFirstValue(ClaimTypes.Email)
                    ?? Context.User?.FindFirstValue("email")
                    ?? userId;

        return (userId, username);
    }
}

public static class PresenceEvents
{
    public const string OnlineUsers = "OnlineUsers";
    public const string UserOnline  = "UserOnline";
    public const string UserOffline = "UserOffline";
    public const string UserAway    = "UserAway";
    public const string UserStatus  = "UserStatus";
}
