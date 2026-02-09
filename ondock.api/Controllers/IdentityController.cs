using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ondock.api.DTOs.Chat;
using ondock.api.Services.Interfaces;

namespace ondock.api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Obsolete("Use gRPC IdentityService instead. This REST controller is deprecated.")]
public class IdentityController : ControllerBase
{
    private readonly IIdentityService _identityService;

    public IdentityController(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    [HttpPost("reveal/request")]
    [ProducesResponseType(typeof(IdentityRevealResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IdentityRevealResponse>> RequestIdentityReveal([FromBody] IdentityRevealRequest request)
    {
        var userId = GetUserId();
        var response = await _identityService.RequestIdentityRevealAsync(userId, request);
        return Ok(response);
    }

    [HttpPost("reveal/accept")]
    [ProducesResponseType(typeof(IdentityRevealResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IdentityRevealResponse>> AcceptIdentityReveal(
        [FromQuery] string chatId,
        [FromQuery] Guid requesterId)
    {
        var userId = GetUserId();
        var response = await _identityService.AcceptIdentityRevealAsync(userId, chatId, requesterId);
        return Ok(response);
    }

    [HttpGet("reveal/status")]
    [ProducesResponseType(typeof(IdentityRevealResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IdentityRevealResponse>> GetIdentityRevealStatus(
        [FromQuery] string chatId,
        [FromQuery] Guid otherUserId)
    {
        var userId = GetUserId();
        var response = await _identityService.GetIdentityRevealStatusAsync(userId, chatId, otherUserId);
        if (response == null)
        {
            return NotFound();
        }
        return Ok(response);
    }

    [HttpGet("reveal/pending")]
    [ProducesResponseType(typeof(IReadOnlyList<IdentityRevealResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<IdentityRevealResponse>>> GetPendingRevealRequests()
    {
        var userId = GetUserId();
        var responses = await _identityService.GetPendingRevealRequestsAsync(userId);
        return Ok(responses);
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user token");
        }
        return userId;
    }
}
