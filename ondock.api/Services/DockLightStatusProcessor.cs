using Microsoft.EntityFrameworkCore;
using ondock.api.Data;
using ondock.api.Data.Entities;
using ondock.api.Services.Interfaces;

namespace ondock.api.Services;

public class DockLightStatusProcessor : IDockLightStatusProcessor
{
    private readonly OnDockDbContext _db;
    private readonly ILogger<DockLightStatusProcessor> _logger;

    public DockLightStatusProcessor(OnDockDbContext db, ILogger<DockLightStatusProcessor> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<DockLightStatusEvent> RecordDeviceStatusAsync(Guid userId, string deviceId, DockLightStatusType status, string? rawData, Guid? sessionId, CancellationToken ct = default)
    {
        var effectiveSessionId = await ResolveActiveSessionAsync(userId, sessionId, ct);
        var entity = CreateBaseEvent(userId, effectiveSessionId, status, deviceId, DockLightStatusSource.Device, rawData, null);
        await PersistAsync(entity, ct);
        return entity;
    }

    public async Task<DockLightStatusEvent> RecordLiveKitStatusAsync(Guid userId, DockLightStatusType status, string reason, Guid? sessionId, CancellationToken ct = default)
    {
        var effectiveSessionId = await ResolveActiveSessionAsync(userId, sessionId, ct);
        var entity = CreateBaseEvent(userId, effectiveSessionId, status, deviceId: "livekit", DockLightStatusSource.LiveKit, rawData: null, reason: reason);
        await PersistAsync(entity, ct);
        return entity;
    }

    public async Task<DockLightStatusEvent> RecordSystemStatusAsync(Guid userId, DockLightStatusType status, string reason, Guid? sessionId, CancellationToken ct = default)
    {
        var effectiveSessionId = await ResolveActiveSessionAsync(userId, sessionId, ct);
        var entity = CreateBaseEvent(userId, effectiveSessionId, status, deviceId: "system", DockLightStatusSource.System, rawData: null, reason: reason);
        await PersistAsync(entity, ct);
        return entity;
    }

    private async Task<Guid?> ResolveActiveSessionAsync(Guid userId, Guid? providedSessionId, CancellationToken ct)
    {
        if (providedSessionId.HasValue)
        {
            return providedSessionId;
        }
        var active = await _db.MonitoringSessions
            .Where(s => s.UserId == userId && s.EndedAt == null)
            .OrderByDescending(s => s.StartedAt)
            .Select(s => s.SessionId)
            .FirstOrDefaultAsync(ct);
        return active == Guid.Empty ? null : active;
    }

    private DockLightStatusEvent CreateBaseEvent(Guid userId, Guid? sessionId, DockLightStatusType status, string deviceId, DockLightStatusSource source, string? rawData, string? reason)
    {
        return new DockLightStatusEvent
        {
            EventId = Guid.NewGuid(),
            UserId = userId,
            MonitoringSessionId = sessionId,
            Status = status,
            DeviceId = deviceId,
            RawDetectionData = rawData,
            Timestamp = DateTime.UtcNow,
            Source = source,
            Reason = reason
        };
    }

    private async Task PersistAsync(DockLightStatusEvent entity, CancellationToken ct)
    {
        // Simple dedupe: if last event for same session & status & source within 2s, skip
        var last = await _db.DockLightStatusEvents
            .Where(e => e.UserId == entity.UserId && e.MonitoringSessionId == entity.MonitoringSessionId)
            .OrderByDescending(e => e.Timestamp)
            .FirstOrDefaultAsync(ct);

        if (last != null && last.Status == entity.Status && last.Source == entity.Source && (DateTime.UtcNow - last.Timestamp) < TimeSpan.FromSeconds(2))
        {
            _logger.LogDebug("Skipping duplicate status within debounce window for user {UserId}", entity.UserId);
            return; // do not persist duplicate
        }

        _db.DockLightStatusEvents.Add(entity);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Recorded {Source} status {Status} for user {UserId} (session {SessionId})", entity.Source, entity.Status, entity.UserId, entity.MonitoringSessionId);
    }
}