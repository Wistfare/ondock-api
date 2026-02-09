using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ondock.api.Configuration;
using ondock.api.Data;
using ondock.api.Data.Entities;
using ondock.api.Services.Interfaces;
using ondock.api.Utilities;

namespace ondock.api.Services;

public class InactivityScanner : IInactivityScanner
{
    private readonly OnDockDbContext _db;
    private readonly IDockLightStatusProcessor _processor;
    private readonly MonitoringSettings _settings;
    private readonly ILogger<InactivityScanner> _logger;

    public InactivityScanner(OnDockDbContext db, IDockLightStatusProcessor processor, IOptions<MonitoringSettings> settings, ILogger<InactivityScanner> logger)
    {
        _db = db;
        _processor = processor;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task ScanAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        // Get active sessions and their last event
        var sessions = await _db.MonitoringSessions
            .Where(s => s.EndedAt == null)
            .Select(s => new
            {
                s.SessionId,
                s.UserId,
                LastEvent = s.Events.OrderByDescending(e => e.Timestamp).FirstOrDefault(),
                ParticipantDevices = s.Participants.Select(p => p.DeviceId).Distinct().ToList()
            })
            .ToListAsync(ct);

        foreach (var s in sessions)
        {
            var lastEventTime = s.LastEvent?.Timestamp;
            var inactivityStale = lastEventTime == null || (now - lastEventTime) > TimeSpan.FromSeconds(_settings.DataInactivityTimeoutSeconds);

            if (inactivityStale)
            {
                await _processor.RecordSystemStatusAsync(s.UserId, DockLightStatusType.ConnectionLost, DockLightStatusReasons.DeviceInactive, s.SessionId, ct);
                _logger.LogInformation("Recorded device inactivity for user {UserId} session {SessionId}", s.UserId, s.SessionId);
            }
        }
    }
}