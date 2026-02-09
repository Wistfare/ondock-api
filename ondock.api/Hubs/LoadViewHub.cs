using System.Collections.Concurrent;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using ondock.api.DTOs.LoadView;

namespace ondock.api.Hubs;

[Authorize]
public class LoadViewHub : Hub
{
    private static readonly ConcurrentDictionary<string, Guid> Connections = new();
    private readonly ILogger<LoadViewHub> _logger;

    public LoadViewHub(ILogger<LoadViewHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        Connections[Context.ConnectionId] = userId;
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");
        _logger.LogInformation("LoadView client connected: {ConnectionId}, User: {UserId}", Context.ConnectionId, userId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Connections.TryRemove(Context.ConnectionId, out _);
        _logger.LogInformation("LoadView client disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Subscribe to updates for a specific LoadView request
    /// </summary>
    public async Task JoinRequest(string requestId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"request:{requestId}");
        _logger.LogDebug("Client {ConnectionId} joined request {RequestId}", Context.ConnectionId, requestId);
    }

    /// <summary>
    /// Unsubscribe from updates for a specific LoadView request
    /// </summary>
    public async Task LeaveRequest(string requestId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"request:{requestId}");
        _logger.LogDebug("Client {ConnectionId} left request {RequestId}", Context.ConnectionId, requestId);
    }

    /// <summary>
    /// Subscribe to nearby LoadView requests based on location
    /// </summary>
    public async Task JoinNearbyRequests(double latitude, double longitude)
    {
        var userId = GetUserId();
        // Use a geohash-like grouping for nearby requests (simplified: grid cells)
        var gridKey = GetGridKey(latitude, longitude);
        await Groups.AddToGroupAsync(Context.ConnectionId, $"grid:{gridKey}");
        _logger.LogDebug("Client {ConnectionId} joined grid {GridKey}", Context.ConnectionId, gridKey);
    }

    /// <summary>
    /// Unsubscribe from nearby LoadView requests
    /// </summary>
    public async Task LeaveNearbyRequests(double latitude, double longitude)
    {
        var gridKey = GetGridKey(latitude, longitude);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"grid:{gridKey}");
    }

    private Guid GetUserId()
    {
        var userIdClaim = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new HubException("Invalid authentication token");
        }
        return userId;
    }

    // Simple grid key based on lat/lng (approximately 10km cells)
    private static string GetGridKey(double latitude, double longitude)
    {
        var latGrid = Math.Floor(latitude * 10) / 10;
        var lngGrid = Math.Floor(longitude * 10) / 10;
        return $"{latGrid:F1}_{lngGrid:F1}";
    }
}
