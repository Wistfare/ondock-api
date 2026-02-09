using System.Collections.Concurrent;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ondock.api.Data;

namespace ondock.api.Hubs;

[Authorize]
public class MonitoringHub : Hub
{
    private static readonly ConcurrentDictionary<string, (Guid UserId, string DeviceId)> Connections =
        new();

    private readonly OnDockDbContext _context;

    public MonitoringHub(OnDockDbContext context)
    {
        _context = context;
    }

    public async Task RegisterDevice(string deviceId)
    {
        var userId = GetUserId();
        Connections[Context.ConnectionId] = (userId, deviceId);

        await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");
        await Groups.AddToGroupAsync(Context.ConnectionId, $"device:{userId}:{deviceId}");
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (Connections.TryRemove(Context.ConnectionId, out var info))
        {
            var (userId, deviceId) = info;

            var activeSessions = await _context.MonitoringSessions
                .Where(s => s.UserId == userId
                            && s.InitiatorDeviceId == deviceId
                            && s.EndedAt == null)
                .ToListAsync();

            if (activeSessions.Any())
            {
                var now = DateTime.UtcNow;
                foreach (var s in activeSessions)
                {
                    s.EndedAt = now;
                    s.IsStreaming = false;
                }

                await _context.SaveChangesAsync();

                foreach (var s in activeSessions)
                {
                    await Clients.Group($"user:{userId}")
                        .SendAsync("SessionEnded", s.SessionId.ToString());
                }
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    private Guid GetUserId()
    {
        var userIdClaim = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new Exception("Invalid authentication token");
        }

        return userId;
    }
}
