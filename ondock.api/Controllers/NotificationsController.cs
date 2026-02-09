using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ondock.api.DTOs.Notifications;
using ondock.api.Services.Interfaces;
using System.Security.Claims;

namespace ondock.api.Controllers;

[ApiController]
[Route("api/Notifications")]
[Produces("application/json")]
[Authorize]
[Obsolete("Use gRPC NotificationsService instead. This REST controller is deprecated.")]
public class NotificationsController : ControllerBase
{
    private readonly IFcmNotificationService _fcmNotificationService;

    public NotificationsController(IFcmNotificationService fcmNotificationService)
    {
        _fcmNotificationService = fcmNotificationService;
    }

    [HttpPost("register-device")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RegisterDevice([FromBody] RegisterDeviceTokenRequest request)
    {
        var userId = GetUserId();
        await _fcmNotificationService.RegisterDeviceTokenAsync(userId, request);
        return Ok();
    }

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
