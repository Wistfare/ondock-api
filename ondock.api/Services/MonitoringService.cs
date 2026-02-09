using Microsoft.EntityFrameworkCore;
using ondock.api.Data;
using ondock.api.Data.Entities;
using ondock.api.DTOs.Monitoring;
using ondock.api.Exceptions;
using ondock.api.Services.Interfaces;

namespace ondock.api.Services;

public class MonitoringService : IMonitoringService
{
    private readonly OnDockDbContext _context;
    private readonly ILiveKitTokenService _liveKitTokenService;
    private readonly IMonitoringRealtimeService _realtime;
    private readonly IMagicPushService _magicPush;

    public MonitoringService(
        OnDockDbContext context,
        ILiveKitTokenService liveKitTokenService,
        IMonitoringRealtimeService realtime,
        IMagicPushService magicPush)
    {
        _context = context;
        _liveKitTokenService = liveKitTokenService;
        _realtime = realtime;
        _magicPush = magicPush;
    }

    public async Task<MonitoringSessionResponse> StartSessionAsync(Guid userId, StartMonitoringSessionRequest request)
    {
        var now = DateTime.UtcNow;

        var session = new MonitoringSession
        {
            SessionId = Guid.NewGuid(),
            UserId = userId,
            InitiatorDeviceId = request.InitiatorDeviceId,
            StartedAt = now,
            IsStreaming = false,
            Metadata = request.Metadata
        };

        _context.MonitoringSessions.Add(session);

        var participant = new MonitoringSessionParticipant
        {
            ParticipantId = Guid.NewGuid(),
            SessionId = session.SessionId,
            UserId = userId,
            DeviceId = request.InitiatorDeviceId,
            Role = "publisher",
            JoinedAt = now
        };

        _context.MonitoringSessionParticipants.Add(participant);

        await _context.SaveChangesAsync();

        return new MonitoringSessionResponse
        {
            SessionId = session.SessionId,
            StartedAt = session.StartedAt,
            EndedAt = session.EndedAt,
            InitiatorDeviceId = session.InitiatorDeviceId,
            IsStreaming = session.IsStreaming
        };
    }

    public async Task<MonitoringSessionResponse> EndSessionAsync(Guid userId, Guid sessionId)
    {
        var session = await _context.MonitoringSessions
            .FirstOrDefaultAsync(s => s.SessionId == sessionId && s.UserId == userId);

        if (session == null)
        {
            throw new NotFoundException("Monitoring session not found");
        }

        if (session.EndedAt == null)
        {
            session.EndedAt = DateTime.UtcNow;
            session.IsStreaming = false;
            await _context.SaveChangesAsync();
        }

        await _realtime.SendSessionEndedAsync(userId, session.SessionId);

        return new MonitoringSessionResponse
        {
            SessionId = session.SessionId,
            StartedAt = session.StartedAt,
            EndedAt = session.EndedAt,
            InitiatorDeviceId = session.InitiatorDeviceId,
            IsStreaming = session.IsStreaming
        };
    }

    public async Task<IReadOnlyList<MonitoringSessionResponse>> GetSessionsAsync(Guid userId, int take)
    {
        var sessions = await _context.MonitoringSessions
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.StartedAt)
            .Take(take)
            .ToListAsync();

        return sessions
            .Select(s => new MonitoringSessionResponse
            {
                SessionId = s.SessionId,
                StartedAt = s.StartedAt,
                EndedAt = s.EndedAt,
                InitiatorDeviceId = s.InitiatorDeviceId,
                IsStreaming = s.IsStreaming
            })
            .ToList();
    }

    public async Task JoinSessionAsync(Guid userId, Guid sessionId, JoinMonitoringSessionRequest request)
    {
        var session = await _context.MonitoringSessions
            .FirstOrDefaultAsync(s => s.SessionId == sessionId);

        if (session == null || session.EndedAt != null)
        {
            throw new NotFoundException("Monitoring session not found or already ended");
        }

        var deviceId = request.DeviceId ?? "unknown";
        var role = deviceId == session.InitiatorDeviceId ? "publisher" : "viewer";
        var now = DateTime.UtcNow;

        var existing = await _context.MonitoringSessionParticipants
            .FirstOrDefaultAsync(p => p.SessionId == sessionId && p.UserId == userId && p.DeviceId == deviceId);

        if (existing != null)
        {
            existing.LeftAt = null;
            existing.Role = role;
            await _context.SaveChangesAsync();
            return;
        }

        var participant = new MonitoringSessionParticipant
        {
            ParticipantId = Guid.NewGuid(),
            SessionId = sessionId,
            UserId = userId,
            DeviceId = deviceId,
            Role = role,
            JoinedAt = now
        };

        _context.MonitoringSessionParticipants.Add(participant);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateStatusAsync(Guid userId, MonitoringStatusRequest request)
    {
        // Find the most recent active monitoring session for this user
        var session = await _context.MonitoringSessions
            .Where(s => s.UserId == userId && s.EndedAt == null)
            .OrderByDescending(s => s.StartedAt)
            .FirstOrDefaultAsync();

        if (session == null)
        {
            // No active session; ignore silently for MVP
            return;
        }

        var deviceId = string.IsNullOrWhiteSpace(request.DeviceId) ? "unknown" : request.DeviceId;
        var newStatus = request.Status;

        // Get last known status for this session (any device)
        var lastEvent = await _context.MonitoringStatusEvents
            .Where(e => e.SessionId == session.SessionId)
            .OrderByDescending(e => e.CreatedAt)
            .FirstOrDefaultAsync();

        var previousStatus = lastEvent?.Status ?? 0; // default to NO LIGHT if none

        // Persist the new status event
        var ev = new Data.Entities.MonitoringStatusEvent
        {
            EventId = Guid.NewGuid(),
            UserId = userId,
            SessionId = session.SessionId,
            DeviceId = deviceId,
            Status = newStatus,
            RawDetectionData = request.RawDetectionData,
            CreatedAt = DateTime.UtcNow,
        };
        _context.MonitoringStatusEvents.Add(ev);
        await _context.SaveChangesAsync();

        // If status changed, fan out a realtime notification via SignalR
        if (newStatus != previousStatus)
        {
            await _realtime.SendStatusChangeAsync(
                userId,
                session.SessionId,
                deviceId,
                previousStatus,
                newStatus);

            // When monitoring completes (GREEN), emit an event to MagicPush so
            // other devices for this user can receive a push + alarm.
            if (newStatus == 2 && previousStatus != 2)
            {
                await _magicPush.SendMonitoringCompletedAsync(userId, session.SessionId);
            }
        }
    }

    public async Task RequestStreamAsync(Guid userId, Guid sessionId, RequestStreamRequest request)
    {
        var session = await _context.MonitoringSessions
            .FirstOrDefaultAsync(s => s.SessionId == sessionId && s.EndedAt == null);

        if (session == null)
        {
            throw new NotFoundException("Monitoring session not found or already ended");
        }

        // Notify the publisher's device to start streaming via SignalR
        await _realtime.SendStartStreamingAsync(
            session.UserId,
            session.InitiatorDeviceId,
            sessionId,
            request.DeviceId);
    }

    public async Task UpdateStreamStateAsync(Guid userId, Guid sessionId, UpdateStreamStateRequest request)
    {
        var session = await _context.MonitoringSessions
            .FirstOrDefaultAsync(s => s.SessionId == sessionId && s.UserId == userId);

        if (session == null)
        {
            throw new NotFoundException("Monitoring session not found");
        }

        session.IsStreaming = request.IsStreaming;
        await _context.SaveChangesAsync();
    }

    public async Task<LiveKitTokenResponse> GetLiveKitTokenAsync(Guid userId, Guid sessionId, string? deviceId)
    {
        var session = await _context.MonitoringSessions
            .FirstOrDefaultAsync(s => s.SessionId == sessionId && s.EndedAt == null);

        if (session == null)
        {
            throw new NotFoundException("Monitoring session not found or already ended");
        }

        // Default deviceId if none is provided
        var effectiveDeviceId = deviceId ?? "unknown";

        // Determine role for this user/device
        var participant = await _context.MonitoringSessionParticipants
            .FirstOrDefaultAsync(p =>
                p.SessionId == sessionId &&
                p.UserId == userId &&
                p.DeviceId == effectiveDeviceId);

        var role = participant?.Role;
        if (string.IsNullOrEmpty(role))
        {
            role = effectiveDeviceId == session.InitiatorDeviceId ? "publisher" : "viewer";
        }

        var canPublish = string.Equals(role, "publisher", StringComparison.OrdinalIgnoreCase);
        var canSubscribe = true;

        // Use sessionId as the LiveKit room name so publisher and viewers join the same room
        var roomName = session.SessionId.ToString();

        // Identity should be unique per participant within the room
        var identity = $"{userId}:{effectiveDeviceId}";

        var token = _liveKitTokenService.GenerateToken(
            roomName,
            identity,
            canPublish,
            canSubscribe
        );

        return new LiveKitTokenResponse
        {
            Token = token
        };
    }

    private async Task ValidateActiveSessionAsync(Guid sessionId)
    {
        var session = await _context.MonitoringSessions
            .FirstOrDefaultAsync(s => s.SessionId == sessionId && s.EndedAt == null);

        if (session == null)
        {
            throw new NotFoundException("Monitoring session not found or already ended");
        }
    }
}
