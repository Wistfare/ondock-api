using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ondock.api.DTOs.Monitoring;
using ondock.api.Services.Interfaces;
using System.Security.Claims;

namespace ondock.api.Controllers;

[ApiController]
[Route("api/Monitoring")]
[Produces("application/json")]
[Authorize]
[Obsolete("Use gRPC MonitoringService instead. This REST controller is deprecated.")]
public class MonitoringController : ControllerBase
{
    private readonly IMonitoringService _monitoringService;

    public MonitoringController(IMonitoringService monitoringService)
    {
        _monitoringService = monitoringService;
    }

    [HttpPost("session/start")]
    [ProducesResponseType(typeof(MonitoringSessionResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<MonitoringSessionResponse>> StartSession([FromBody] StartMonitoringSessionRequest request)
    {
        var userId = GetUserId();
        var result = await _monitoringService.StartSessionAsync(userId, request);
        return Ok(result);
    }

    [HttpPost("session/end/{sessionId}")]
    [ProducesResponseType(typeof(MonitoringSessionResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<MonitoringSessionResponse>> EndSession([FromRoute] Guid sessionId)
    {
        var userId = GetUserId();
        var result = await _monitoringService.EndSessionAsync(userId, sessionId);
        return Ok(result);
    }

    [HttpGet("sessions")]
    [ProducesResponseType(typeof(IEnumerable<MonitoringSessionResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<MonitoringSessionResponse>>> GetSessions([FromQuery] int take = 25)
    {
        var userId = GetUserId();
        var result = await _monitoringService.GetSessionsAsync(userId, take);
        return Ok(result);
    }

    [HttpPost("session/{sessionId}/join")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> JoinSession([FromRoute] Guid sessionId, [FromBody] JoinMonitoringSessionRequest request)
    {
        var userId = GetUserId();
        await _monitoringService.JoinSessionAsync(userId, sessionId, request);
        return Ok();
    }

    [HttpPost("status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateStatus([FromBody] MonitoringStatusRequest request)
    {
        var userId = GetUserId();
        await _monitoringService.UpdateStatusAsync(userId, request);
        return Ok();
    }

    [HttpPost("session/{sessionId}/request-stream")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> RequestStream([FromRoute] Guid sessionId, [FromBody] RequestStreamRequest request)
    {
        var userId = GetUserId();
        await _monitoringService.RequestStreamAsync(userId, sessionId, request);
        return Accepted();
    }

    [HttpPost("session/{sessionId}/stream-state")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateStreamState([FromRoute] Guid sessionId, [FromBody] UpdateStreamStateRequest request)
    {
        var userId = GetUserId();
        await _monitoringService.UpdateStreamStateAsync(userId, sessionId, request);
        return Ok();
    }

    [HttpGet("session/{sessionId}/token")]
    [ProducesResponseType(typeof(LiveKitTokenResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<LiveKitTokenResponse>> GetToken([FromRoute] Guid sessionId, [FromQuery] string? deviceId = null)
    {
        var userId = GetUserId();
        var result = await _monitoringService.GetLiveKitTokenAsync(userId, sessionId, deviceId);
        return Ok(result);
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
