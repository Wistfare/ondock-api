using Microsoft.AspNetCore.SignalR;
using ondock.api.DTOs.LoadView;
using ondock.api.Hubs;
using ondock.api.Services.Interfaces;

namespace ondock.api.Services;

public class SignalRLoadViewRealtimeService : ILoadViewRealtimeService
{
    private readonly IHubContext<LoadViewHub> _hubContext;
    private readonly ILogger<SignalRLoadViewRealtimeService> _logger;

    public SignalRLoadViewRealtimeService(
        IHubContext<LoadViewHub> hubContext,
        ILogger<SignalRLoadViewRealtimeService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotifyNewRequestAsync(LoadViewRequestDto request)
    {
        await _hubContext.Clients
            .Group($"request:{request.RequestId}")
            .SendAsync("NewRequest", request);

        _logger.LogDebug("Notified new request: {RequestId}", request.RequestId);
    }

    public async Task NotifyNewResponseAsync(Guid requestId, LoadViewResponseDto response)
    {
        await _hubContext.Clients
            .Group($"request:{requestId}")
            .SendAsync("NewResponse", response);

        // Also notify the requester directly
        await _hubContext.Clients
            .Group($"user:{response.RequestId}")
            .SendAsync("NewResponse", response);

        _logger.LogDebug("Notified new response for request: {RequestId}", requestId);
    }

    public async Task NotifyRequestExpiredAsync(Guid requestId)
    {
        await _hubContext.Clients
            .Group($"request:{requestId}")
            .SendAsync("RequestExpired", requestId);

        _logger.LogDebug("Notified request expired: {RequestId}", requestId);
    }

    public async Task NotifyLiveStreamStartedAsync(Guid requestId, LoadViewResponseDto response)
    {
        await _hubContext.Clients
            .Group($"request:{requestId}")
            .SendAsync("LiveStreamStarted", response);

        _logger.LogDebug("Notified live stream started for request: {RequestId}", requestId);
    }

    public async Task NotifyLiveStreamEndedAsync(Guid requestId, Guid responseId)
    {
        await _hubContext.Clients
            .Group($"request:{requestId}")
            .SendAsync("LiveStreamEnded", new { RequestId = requestId, ResponseId = responseId });

        _logger.LogDebug("Notified live stream ended for request: {RequestId}", requestId);
    }

    public async Task NotifyRequestFulfilledAsync(Guid requestId)
    {
        await _hubContext.Clients
            .Group($"request:{requestId}")
            .SendAsync("RequestFulfilled", requestId);

        _logger.LogDebug("Notified request fulfilled: {RequestId}", requestId);
    }

    public async Task NotifyNearbyUsersAsync(double latitude, double longitude, LoadViewRequestDto request)
    {
        // Notify users in the same grid cell
        var gridKey = GetGridKey(latitude, longitude);
        await _hubContext.Clients
            .Group($"grid:{gridKey}")
            .SendAsync("NearbyRequest", request);

        // Also notify adjacent grid cells for better coverage
        var adjacentKeys = GetAdjacentGridKeys(latitude, longitude);
        foreach (var key in adjacentKeys)
        {
            await _hubContext.Clients
                .Group($"grid:{key}")
                .SendAsync("NearbyRequest", request);
        }

        _logger.LogDebug("Notified nearby users for request: {RequestId} at grid {GridKey}", request.RequestId, gridKey);
    }

    private static string GetGridKey(double latitude, double longitude)
    {
        var latGrid = Math.Floor(latitude * 10) / 10;
        var lngGrid = Math.Floor(longitude * 10) / 10;
        return $"{latGrid:F1}_{lngGrid:F1}";
    }

    private static IEnumerable<string> GetAdjacentGridKeys(double latitude, double longitude)
    {
        var latGrid = Math.Floor(latitude * 10) / 10;
        var lngGrid = Math.Floor(longitude * 10) / 10;

        var offsets = new[] { -0.1, 0, 0.1 };
        foreach (var latOffset in offsets)
        {
            foreach (var lngOffset in offsets)
            {
                if (latOffset == 0 && lngOffset == 0) continue; // Skip center (already notified)
                yield return $"{latGrid + latOffset:F1}_{lngGrid + lngOffset:F1}";
            }
        }
    }
}
