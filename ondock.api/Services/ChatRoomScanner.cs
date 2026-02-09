using Microsoft.EntityFrameworkCore;
using ondock.api.Data;
using ondock.api.Services.Interfaces;

namespace ondock.api.Services;

public class ChatRoomScanner : IChatRoomScanner
{
    private readonly OnDockDbContext _db;
    private readonly IChatSettingsProvider _settingsProvider;
    private readonly IChatRealtimeService _realtime;
    private readonly ILogger<ChatRoomScanner> _logger;

    public ChatRoomScanner(
        OnDockDbContext db,
        IChatSettingsProvider settingsProvider,
        IChatRealtimeService realtime,
        ILogger<ChatRoomScanner> logger)
    {
        _db = db;
        _settingsProvider = settingsProvider;
        _realtime = realtime;
        _logger = logger;
    }

    public async Task ScanAsync()
    {
        var ct = CancellationToken.None;
        var settings = _settingsProvider.Get();
        var now = DateTime.UtcNow;

        var chats = await _db.Chats.Where(c => c.ExpiresAt > now).AsNoTracking().ToListAsync(ct);
        foreach (var chat in chats)
        {
            try
            {
                if (chat.ExpiresAt <= now)
                {
                    await EndChatAsync(chat.ChatId, "expired", ct);
                    continue;
                }

                var participants = await _db.ChatParticipants
                    .AsNoTracking()
                    .Where(p => p.ChatId == chat.ChatId)
                    .ToListAsync(ct);

                if (participants.Count < 2)
                {
                    await EndChatAsync(chat.ChatId, "participant_missing", ct);
                    continue;
                }

                var presenceCutoff = now.AddSeconds(-settings.PresenceMaxAgeSeconds);
                if (participants.Any(p => p.LastHeartbeat < presenceCutoff))
                {
                    await EndChatAsync(chat.ChatId, "presence_timeout", ct);
                    continue;
                }

                if (participants.Any(p => p.CurrentSpeed > settings.SpeedThresholdMph))
                {
                    await EndChatAsync(chat.ChatId, "speed_threshold", ct);
                    continue;
                }

                var maxMeters = (double)chat.RadiusMeters;
                if (participants.Any(p => HaversineMeters(p.CurrentLocation.Y, p.CurrentLocation.X, chat.CenterLocation.Y, chat.CenterLocation.X) > maxMeters))
                {
                    await EndChatAsync(chat.ChatId, "out_of_range", ct);
                    continue;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ChatRoomScanner failed scanning chat {ChatId}", chat.ChatId);
            }
        }
    }

    private static double HaversineMeters(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371000.0;
        static double ToRad(double deg) => deg * (Math.PI / 180.0);

        var dLat = ToRad(lat2 - lat1);
        var dLon = ToRad(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private async Task EndChatAsync(string chatId, string reason, CancellationToken ct)
    {
        var chat = await _db.Chats.FirstOrDefaultAsync(c => c.ChatId == chatId, ct);
        if (chat == null)
        {
            return;
        }

        // Mark as expired by setting ExpiresAt to now
        chat.ExpiresAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        await _realtime.SendChatEndedAsync(chatId, new { chatId, reason, timestamp = DateTime.UtcNow });
    }
}
