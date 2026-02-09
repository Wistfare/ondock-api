using Microsoft.EntityFrameworkCore;
using ondock.api.Data;
using ondock.api.Data.Entities;
using ondock.api.DTOs.Monitoring;
using ondock.api.Services.Interfaces;

namespace ondock.api.Services;

public class DockLightMonitoringService : IDockLightMonitoringService
{
    private readonly OnDockDbContext _db;
    private readonly IFcmNotificationService _fcm;
    private readonly ILiveKitService _liveKit;
    private readonly IDockLightStatusProcessor _processor;
    private readonly ILogger<DockLightMonitoringService> _logger;

    public DockLightMonitoringService(OnDockDbContext db, IFcmNotificationService fcm, ILiveKitService liveKit, IDockLightStatusProcessor processor, ILogger<DockLightMonitoringService> logger)
    {
        _db = db;
        _fcm = fcm;
        _liveKit = liveKit;
        _processor = processor;
        _logger = logger;
    }

    public async Task<DockLightStatusEvent> UpdateStatusAsync(Guid userId, DockLightStatusUpdateRequest request, CancellationToken ct = default)
    {
        // Require an active monitoring session
        var activeSession = await _db.MonitoringSessions
            .Where(s => s.UserId == userId && s.EndedAt == null)
            .OrderByDescending(s => s.StartedAt)
            .FirstOrDefaultAsync(ct);
        if (activeSession == null)
        {
            throw new InvalidOperationException("No active monitoring session. Start a session before posting status.");
        }

        var entity = await _processor.RecordDeviceStatusAsync(userId, request.DeviceId, request.Status, request.RawDetectionData, activeSession.SessionId, ct);

        // Fire and forget notifications (but await sequentially for now for reliability)
        try
        {
            await _fcm.SendDockLightStatusAsync(userId, entity.Status, entity.DeviceId, entity.Source, entity.Reason, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send FCM dock light status notification for user {UserId}", userId);
        }

        try
        {
            await _liveKit.PublishDockLightStatusAsync(userId, entity.Status, entity.DeviceId, entity.Source, entity.Reason, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish LiveKit dock light status for user {UserId}", userId);
        }

        return entity;
    }

    public async Task<DockLightStatusEvent?> GetLatestStatusAsync(Guid userId, CancellationToken ct = default)
    {
        // Latest event from the active session if present, else overall
        var activeSessionId = await _db.MonitoringSessions
            .Where(s => s.UserId == userId && s.EndedAt == null)
            .Select(s => s.SessionId)
            .FirstOrDefaultAsync(ct);

        IQueryable<DockLightStatusEvent> query = _db.DockLightStatusEvents.Where(e => e.UserId == userId);
        if (activeSessionId != Guid.Empty)
        {
            query = query.Where(e => e.MonitoringSessionId == activeSessionId);
        }
        return await query.OrderByDescending(e => e.Timestamp).FirstOrDefaultAsync(ct);
    }
}
