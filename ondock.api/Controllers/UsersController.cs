using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ondock.api.DTOs.Chat;
using ondock.api.DTOs.Contact;
using ondock.api.DTOs.Settings;
using ondock.api.Services.Interfaces;

namespace ondock.api.Controllers;

/// <summary>
/// RESTful API controller for user profile and settings operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
[Obsolete("Use gRPC UsersService instead. This REST controller is deprecated.")]
public class UsersController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly IContactService _contactService;
    private readonly IUserSettingsService _userSettingsService;
    private readonly ILocationService _locationService;

    public UsersController(
        IChatService chatService,
        IContactService contactService,
        IUserSettingsService userSettingsService,
        ILocationService locationService)
    {
        _chatService = chatService;
        _contactService = contactService;
        _userSettingsService = userSettingsService;
        _locationService = locationService;
    }

    #region Profile

    /// <summary>
    /// Get the current user's chat profile
    /// </summary>
    [HttpGet("me/profile")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserProfileResponse>> GetMyProfile()
    {
        var userId = GetUserId();
        var result = await _chatService.GetProfileAsync(userId);
        return Ok(result);
    }

    /// <summary>
    /// Get the current user's chat profile
    /// </summary>
    [HttpGet("me/Error")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [AllowAnonymous]
    public async Task<ActionResult<UserProfileResponse>> GetMyError()
    {
        throw new Exception("Test exception");
        var userId = GetUserId();
        var result = await _chatService.GetProfileAsync(userId);
        return Ok(result);
    }

    /// <summary>
    /// Update the current user's chat profile
    /// </summary>
    [HttpPut("me/profile")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserProfileResponse>> UpdateMyProfile([FromBody] UpsertUserProfileRequest request)
    {
        var userId = GetUserId();
        var result = await _chatService.UpsertProfileAsync(userId, request);
        return Ok(result);
    }

    #endregion

    #region Settings

    /// <summary>
    /// Get the current user's settings (includes DND, notifications, privacy, etc.)
    /// </summary>
    [HttpGet("me/settings")]
    [ProducesResponseType(typeof(UserSettingsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserSettingsResponse>> GetSettings()
    {
        var userId = GetUserId();
        var result = await _userSettingsService.GetSettingsAsync(userId);
        return Ok(result);
    }

    /// <summary>
    /// Update the current user's settings (partial update supported)
    /// </summary>
    [HttpPut("me/settings")]
    [ProducesResponseType(typeof(UserSettingsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserSettingsResponse>> UpdateSettings([FromBody] UpdateUserSettingsRequest request)
    {
        var userId = GetUserId();
        var result = await _userSettingsService.UpdateSettingsAsync(userId, request);
        return Ok(result);
    }

    #endregion

    #region Discovery

    /// <summary>
    /// Get nearby users for potential chat connections and map display
    /// </summary>
    [HttpGet("me/nearby")]
    [ProducesResponseType(typeof(NearbyUsersResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<NearbyUsersResponse>> GetNearbyUsers(
        [FromQuery] double latitude,
        [FromQuery] double longitude,
        [FromQuery] double speedMph = 0,
        [FromQuery] double? radiusMiles = null)
    {
        var userId = GetUserId();
        var result = await _locationService.GetNearbyUsersAsync(userId, latitude, longitude, speedMph, radiusMiles);
        return Ok(result);
    }

    #endregion

    #region Contacts

    /// <summary>
    /// Get the current user's permanent contacts (revealed identities)
    /// </summary>
    [HttpGet("me/contacts")]
    [ProducesResponseType(typeof(PermanentContactsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<PermanentContactsResponse>> GetContacts()
    {
        var userId = GetUserId();
        var result = await _contactService.GetPermanentContactsAsync(userId);
        return Ok(result);
    }

    #endregion

    #region QR Code

    /// <summary>
    /// Generate a QR code for contact exchange
    /// </summary>
    [HttpPost("me/qr-code")]
    [ProducesResponseType(typeof(GenerateQRCodeResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<GenerateQRCodeResponse>> GenerateQRCode([FromQuery] Guid? chatId = null)
    {
        var userId = GetUserId();
        var result = await _contactService.GenerateQRCodeAsync(userId, chatId);
        return Ok(result);
    }

    /// <summary>
    /// Scan a QR code to initiate contact exchange
    /// </summary>
    [HttpPost("me/qr-code/scan")]
    [ProducesResponseType(typeof(ScanQRCodeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ScanQRCodeResponse>> ScanQRCode([FromBody] ScanQRCodeRequest request)
    {
        var userId = GetUserId();
        var result = await _contactService.ScanQRCodeAsync(userId, request);
        return Ok(result);
    }

    #endregion

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new ondock.api.Exceptions.UnauthorizedException("Invalid authentication token");
        }
        return userId;
    }
}
