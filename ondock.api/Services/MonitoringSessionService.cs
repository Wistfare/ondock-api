using Microsoft.EntityFrameworkCore;
using ondock.api.Data;
using ondock.api.Data.Entities;
using ondock.api.Services.Interfaces;
using ondock.api.Exceptions;

namespace ondock.api.Services;

public class MonitoringSessionService : IMonitoringSessionService
{
    private readonly OnDockDbContext _db;
    private readonly ILogger<MonitoringSessionService> _logger;

    public MonitoringSessionService(OnDockDbContext db, ILogger<MonitoringSessionService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<MonitoringSession> StartSessionAsync(Guid userId, string initiatorDeviceId, string? metadata = null, CancellationToken ct = default)
    {
        // Enforce single active monitoring session per user
        var existingActive = await _db.MonitoringSessions
            .Where(s => s.UserId == userId && s.EndedAt == null)
            .FirstOrDefaultAsync(ct);
        if (existingActive != null)
        {
            // End previous session (force logout on existing device)
            existingActive.EndedAt = DateTime.UtcNow;
            _logger.LogInformation("Force-ended previous monitoring session {SessionId} for user {UserId}", existingActive.SessionId, userId);
        }

        // Enforce max 2 distinct devices per user
        var distinctDeviceIds = await _db.MonitoringSessionParticipants
            .Where(p => p.UserId == userId)
            .Select(p => p.DeviceId)
            .Distinct()
            .ToListAsync(ct);

        var hasDeviceAlready = distinctDeviceIds.Contains(initiatorDeviceId);
        if (!hasDeviceAlready && distinctDeviceIds.Count >= 2)
        {
            throw new ForbiddenException("Device limit (2) reached. Remove an existing device before adding a new one.");
        }

        var session = new MonitoringSession
        {
            SessionId = Guid.NewGuid(),
            UserId = userId,
            InitiatorDeviceId = initiatorDeviceId,
            StartedAt = DateTime.UtcNow,
            Metadata = metadata
        };

        var participant = new MonitoringSessionParticipant
        {
            ParticipantId = Guid.NewGuid(),
            SessionId = session.SessionId,
            UserId = userId,
            DeviceId = initiatorDeviceId,
            JoinedAt = DateTime.UtcNow
        };

        _db.MonitoringSessions.Add(session);
        _db.MonitoringSessionParticipants.Add(participant);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Started monitoring session {SessionId} for user {UserId}", session.SessionId, userId);
        return session;
    }

    public async Task<MonitoringSession?> EndSessionAsync(Guid userId, Guid sessionId, CancellationToken ct = default)
    {
        var session = await _db.MonitoringSessions
            .Where(s => s.SessionId == sessionId && s.UserId == userId)
            .FirstOrDefaultAsync(ct);
        if (session == null)
        {
            return null;
        }
        if (session.EndedAt == null)
        {
            session.EndedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Ended monitoring session {SessionId} for user {UserId}", session.SessionId, userId);
        }
        return session;
    }

    public async Task<MonitoringSession?> GetSessionAsync(Guid userId, Guid sessionId, CancellationToken ct = default)
    {
        return await _db.MonitoringSessions
            .Include(s => s.Participants)
            .Include(s => s.Events.OrderBy(e => e.Timestamp))
            .Where(s => s.SessionId == sessionId && s.UserId == userId)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<MonitoringSession>> ListSessionsAsync(Guid userId, int take = 25, CancellationToken ct = default)
    {
        return await _db.MonitoringSessions
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.StartedAt)
            .Take(take)
            .ToListAsync(ct);
    }

    public async Task<MonitoringSession?> GetActiveSessionAsync(Guid userId, CancellationToken ct = default)
    {
        return await _db.MonitoringSessions
            .Where(s => s.UserId == userId && s.EndedAt == null)
            .OrderByDescending(s => s.StartedAt)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<bool> RemoveDeviceAsync(Guid userId, string deviceId, CancellationToken ct = default)
    {
        // End any active sessions using this device for the user
        var activeSessions = await _db.MonitoringSessions
            .Where(s => s.UserId == userId && s.EndedAt == null && s.InitiatorDeviceId == deviceId)
            .ToListAsync(ct);
        foreach (var s in activeSessions)
        {
            s.EndedAt = DateTime.UtcNow;
        }

        // Remove participants entries for this device
        var participants = await _db.MonitoringSessionParticipants
            .Where(p => p.UserId == userId && p.DeviceId == deviceId)
            .ToListAsync(ct);
        if (!participants.Any() && !activeSessions.Any())
        {
            return false; // nothing to remove
        }
        _db.MonitoringSessionParticipants.RemoveRange(participants);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Removed device {DeviceId} for user {UserId}", deviceId, userId);
        return true;
    }

    public async Task<MonitoringSessionParticipant> AddViewerAsync(Guid requestingUserId, Guid sessionId, string viewerDeviceId, CancellationToken ct = default)
    {
        var session = await _db.MonitoringSessions
            .Include(s => s.Participants)
            .FirstOrDefaultAsync(s => s.SessionId == sessionId, ct);
        if (session == null)
        {
            throw new NotFoundException("Monitoring session not found");
        }
        if (session.EndedAt != null)
        {
            throw new ConflictException("Cannot join a session that has ended");
        }

        // Check if viewer already joined with same device
        var existing = session.Participants.FirstOrDefault(p => p.DeviceId == viewerDeviceId && p.LeftAt == null);
        if (existing != null)
        {
            return existing; // Idempotent join
        }

        // Enforce per-user device cap only for the session owner when adding new devices
        // A viewer from another user should still respect a global cap; simplified assumption: allow.

        var participant = new MonitoringSessionParticipant
        {
            ParticipantId = Guid.NewGuid(),
            SessionId = session.SessionId,
            UserId = requestingUserId,
            DeviceId = viewerDeviceId,
            JoinedAt = DateTime.UtcNow,
            Role = MonitoringParticipantRole.Viewer.ToString()
        };
        _db.MonitoringSessionParticipants.Add(participant);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Viewer device {DeviceId} joined monitoring session {SessionId} as viewer", viewerDeviceId, session.SessionId);
        return participant;
    }
}
