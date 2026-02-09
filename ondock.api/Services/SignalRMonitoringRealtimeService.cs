using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using ondock.api.Hubs;
using ondock.api.Services.Interfaces;

namespace ondock.api.Services;

public class SignalRMonitoringRealtimeService : IMonitoringRealtimeService
{
    private readonly IHubContext<MonitoringHub> _hubContext;
    private readonly ILogger<SignalRMonitoringRealtimeService> _logger;

    public SignalRMonitoringRealtimeService(
        IHubContext<MonitoringHub> hubContext,
        ILogger<SignalRMonitoringRealtimeService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task SendStatusChangeAsync(
        Guid userId,
        Guid sessionId,
        string deviceId,
        int previousStatus,
        int newStatus)
    {
        try
        {
            await _hubContext.Clients
                .Group($"user:{userId}")
                .SendAsync(
                    "StatusChanged",
                    sessionId.ToString(),
                    deviceId,
                    previousStatus,
                    newStatus
                );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send StatusChanged via SignalR for user {UserId}", userId);
        }
    }

    public async Task SendStartStreamingAsync(
        Guid userId,
        string publisherDeviceId,
        Guid sessionId,
        string requesterDeviceId)
    {
        try
        {
            await _hubContext.Clients
                .Group($"device:{userId}:{publisherDeviceId}")
                .SendAsync(
                    "StartStreaming",
                    sessionId.ToString(),
                    requesterDeviceId
                );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send StartStreaming via SignalR for user {UserId} / device {DeviceId}",
                userId,
                publisherDeviceId
            );
        }
    }

    public async Task SendSessionEndedAsync(
        Guid userId,
        Guid sessionId)
    {
        try
        {
            await _hubContext.Clients
                .Group($"user:{userId}")
                .SendAsync(
                    "SessionEnded",
                    sessionId.ToString()
                );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send SessionEnded via SignalR for user {UserId}",
                userId
            );
        }
    }
}
