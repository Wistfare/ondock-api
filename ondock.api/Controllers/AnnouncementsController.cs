using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ondock.api.DTOs.Chat;
using ondock.api.Services.Interfaces;

namespace ondock.api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Obsolete("Use gRPC AnnouncementsService instead. This REST controller is deprecated.")]
public class AnnouncementsController : ControllerBase
{
    private readonly IAnnouncementService _announcementService;

    public AnnouncementsController(IAnnouncementService announcementService)
    {
        _announcementService = announcementService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AnnouncementDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AnnouncementDto>>> GetAnnouncements(
        [FromQuery] double latitude,
        [FromQuery] double longitude,
        [FromQuery] int take = 50)
    {
        var userId = GetUserId();
        var announcements = await _announcementService.GetAnnouncementsAsync(userId, latitude, longitude, take);
        return Ok(announcements);
    }

    [HttpGet("{announcementId:guid}")]
    [ProducesResponseType(typeof(AnnouncementDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AnnouncementDto>> GetAnnouncement(Guid announcementId)
    {
        var announcement = await _announcementService.GetAnnouncementAsync(announcementId);
        if (announcement == null)
        {
            return NotFound();
        }
        return Ok(announcement);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateAnnouncementResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<CreateAnnouncementResponse>> CreateAnnouncement([FromBody] CreateAnnouncementRequest request)
    {
        var userId = GetUserId();
        var response = await _announcementService.CreateAnnouncementAsync(userId, request);
        return Created("", response);
    }

    [HttpDelete("{announcementId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAnnouncement(Guid announcementId)
    {
        var userId = GetUserId();
        var deleted = await _announcementService.DeleteAnnouncementAsync(userId, announcementId);
        if (!deleted)
        {
            return NotFound();
        }
        return NoContent();
    }

    [HttpPost("{announcementId:guid}/view")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> TrackView(Guid announcementId)
    {
        var userId = GetUserId();
        await _announcementService.IncrementViewCountAsync(announcementId, userId);
        return NoContent();
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
