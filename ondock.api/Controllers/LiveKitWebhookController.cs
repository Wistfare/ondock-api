using System.IO;
using Microsoft.AspNetCore.Mvc;
using ondock.api.Services.Interfaces;
using ondock.api.Utilities;
using Livekit.Server.Sdk.Dotnet;
using ondock.api.Data.Entities;

namespace ondock.api.Controllers;

[ApiController]
[Route("livekit/webhook")]
public class LiveKitWebhookController : ControllerBase
{
    private readonly IDockLightStatusProcessor _processor;
    private readonly ILogger<LiveKitWebhookController> _logger;
    private readonly IConfiguration _config;

    public LiveKitWebhookController(IDockLightStatusProcessor processor, ILogger<LiveKitWebhookController> logger, IConfiguration config)
    {
        _processor = processor;
        _logger = logger;
        _config = config;
    }

    [HttpPost]
    public async Task<IActionResult> Receive(CancellationToken ct)
    {
        var apiKey = Environment.GetEnvironmentVariable("LIVEKIT_API_KEY") ?? _config.GetSection("LiveKit:ApiKey").Value;
        var apiSecret = Environment.GetEnvironmentVariable("LIVEKIT_API_SECRET") ?? _config.GetSection("LiveKit:ApiSecret").Value;
        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(apiSecret))
        {
            return StatusCode(500, new { error = new { message = "LiveKit credentials not configured" } });
        }

        string rawBody;
        using (var reader = new StreamReader(Request.Body))
        {
            rawBody = await reader.ReadToEndAsync();
        }
        if (!Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            return Unauthorized(new { error = new { message = "Missing Authorization header" } });
        }

        var receiver = new WebhookReceiver(apiKey, apiSecret);
        WebhookEvent evt;
        try
        {
            evt = receiver.Receive(rawBody, authHeader!);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Invalid LiveKit webhook signature or payload");
            return BadRequest(new { error = new { message = "Invalid webhook" } });
        }

        // Derive userId from participant identity (assuming identity = Guid userId string)
        Guid userId;
        if (!Guid.TryParse(evt.Participant?.Identity, out userId))
        {
            _logger.LogDebug("Ignoring webhook; participant identity not a user Guid: {Identity}", evt.Participant?.Identity);
            return Ok();
        }

        // Map event type to reason/status
        var eventName = evt.Event.ToString().ToLowerInvariant();
        DockLightStatusType status = DockLightStatusType.ConnectionLost;
        string reason = eventName switch
        {
            var e when e.Contains("participant_disconnected") => DockLightStatusReasons.ParticipantDisconnected,
            var e when e.Contains("room_finished") || e.Contains("room_close") => DockLightStatusReasons.RoomClosed,
            _ => DockLightStatusReasons.ParticipantAbsent
        };

        await _processor.RecordLiveKitStatusAsync(userId, status, reason, null, ct);
        return Ok(new { recorded = true });
    }
}