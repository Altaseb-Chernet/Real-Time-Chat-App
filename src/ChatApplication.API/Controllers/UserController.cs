using System.Security.Claims;
using ChatApplication.Core.Modules.User.Contracts;
using ChatApplication.Core.Modules.User.Models;
using ChatApplication.Shared.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatApplication.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IUserPresenceService _presenceService;

    public UserController(IUserService userService, IUserPresenceService presenceService)
    {
        _userService = userService;
        _presenceService = presenceService;
    }

    // -------------------------------------------------------------------------
    // Profile
    // -------------------------------------------------------------------------

    /// <summary>Get the currently authenticated user's profile.</summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(ApiResponse<UserProfileResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMe()
    {
        var userId = GetUserId();
        var user = await _userService.GetByIdAsync(userId);
        if (user is null)
            return NotFound(ApiResponse<UserProfileResponse>.Fail("User not found."));

        return Ok(ApiResponse<UserProfileResponse>.Ok(MapUser(user)));
    }

    /// <summary>Get any user's public profile by ID.</summary>
    [HttpGet("{userId}")]
    [ProducesResponseType(typeof(ApiResponse<UserProfileResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUser(string userId)
    {
        var user = await _userService.GetByIdAsync(userId);
        if (user is null)
            return NotFound(ApiResponse<UserProfileResponse>.Fail("User not found."));

        return Ok(ApiResponse<UserProfileResponse>.Ok(MapUser(user)));
    }

    /// <summary>Update the current user's profile (display name, avatar, bio).</summary>
    [HttpPut("me")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var userId = GetUserId();
        await _userService.UpdateProfileAsync(userId, new UserProfile
        {
            DisplayName = request.DisplayName ?? string.Empty,
            AvatarUrl = request.AvatarUrl,
            Bio = request.Bio
        });
        return Ok(ApiResponse<string>.Ok("Profile updated."));
    }

    /// <summary>Delete the current user's account (soft delete).</summary>
    [HttpDelete("me")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAccount()
    {
        var userId = GetUserId();
        await _userService.DeleteAsync(userId);
        return NoContent();
    }

    // -------------------------------------------------------------------------
    // Presence
    // -------------------------------------------------------------------------

    /// <summary>Get all currently online / away users.</summary>
    [HttpGet("online")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<UserStatusResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOnlineUsers()
    {
        var users = await _presenceService.GetOnlineUsersAsync();
        return Ok(ApiResponse<IEnumerable<UserStatusResponse>>.Ok(users.Select(MapStatus)));
    }

    /// <summary>Get the presence status of a specific user.</summary>
    [HttpGet("{userId}/status")]
    [ProducesResponseType(typeof(ApiResponse<UserStatusResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserStatus(string userId)
    {
        var status = await _presenceService.GetStatusAsync(userId);
        return Ok(ApiResponse<UserStatusResponse>.Ok(MapStatus(status)));
    }

    /// <summary>Set the current user's status to Away.</summary>
    [HttpPost("me/away")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetAway()
    {
        var userId = GetUserId();
        await _presenceService.SetAwayAsync(userId);
        return NoContent();
    }

    /// <summary>Set the current user's status back to Online.</summary>
    [HttpPost("me/online")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetOnline()
    {
        var (userId, username) = GetIdentity();
        await _presenceService.SetOnlineAsync(userId, username);
        return NoContent();
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private string GetUserId()
        => User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub")
        ?? throw new UnauthorizedAccessException();

    private (string userId, string username) GetIdentity()
    {
        var userId = GetUserId();
        var username = User.FindFirstValue(ClaimTypes.Email)
                    ?? User.FindFirstValue("email")
                    ?? userId;
        return (userId, username);
    }

    private static UserProfileResponse MapUser(AppUser u) => new()
    {
        Id = u.Id,
        Username = u.Username,
        Email = u.Email,
        Role = u.Role,
        AvatarUrl = u.AvatarUrl,
        Bio = u.Bio,
        CreatedAt = u.CreatedAt
    };

    private static UserStatusResponse MapStatus(UserStatus s) => new()
    {
        UserId = s.UserId,
        Username = s.Username,
        Status = s.Status.ToString(),
        LastSeen = s.LastSeen
    };
}

// -------------------------------------------------------------------------
// Request / response types local to this controller
// -------------------------------------------------------------------------

public class UpdateProfileRequest
{
    /// <example>John Doe</example>
    public string? DisplayName { get; set; }

    /// <example>https://example.com/avatar.png</example>
    public string? AvatarUrl { get; set; }

    /// <example>Software developer and coffee enthusiast.</example>
    public string? Bio { get; set; }
}

public class UserProfileResponse
{
    public string Id { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UserStatusResponse
{
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime LastSeen { get; set; }
}
