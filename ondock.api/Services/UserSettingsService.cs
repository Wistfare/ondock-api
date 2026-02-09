using Microsoft.EntityFrameworkCore;
using ondock.api.Data;
using ondock.api.Data.Entities;
using ondock.api.DTOs.Settings;
using ondock.api.Services.Interfaces;

namespace ondock.api.Services;

public class UserSettingsService : IUserSettingsService
{
    private readonly OnDockDbContext _db;

    public UserSettingsService(OnDockDbContext db)
    {
        _db = db;
    }

    public async Task<UserSettingsResponse> GetSettingsAsync(Guid userId)
    {
        var settings = await _db.UserSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (settings == null)
        {
            // Create default settings for user
            settings = await CreateDefaultSettingsAsync(userId);
        }

        return MapToResponse(settings);
    }

    public async Task<UserSettingsResponse> UpdateSettingsAsync(Guid userId, UpdateUserSettingsRequest request)
    {
        var settings = await _db.UserSettings.FirstOrDefaultAsync(s => s.UserId == userId);

        if (settings == null)
        {
            settings = await CreateDefaultSettingsAsync(userId);
        }

        // Apply partial updates (only update fields that are provided)
        if (request.DndEnabled.HasValue)
            settings.DndEnabled = request.DndEnabled.Value;
        if (request.DndScheduleStart != null)
            settings.DndScheduleStart = request.DndScheduleStart;
        if (request.DndScheduleEnd != null)
            settings.DndScheduleEnd = request.DndScheduleEnd;
        if (request.DndScheduleEnabled.HasValue)
            settings.DndScheduleEnabled = request.DndScheduleEnabled.Value;

        if (request.NotifyChat.HasValue)
            settings.NotifyChat = request.NotifyChat.Value;
        if (request.NotifyAnnouncements.HasValue)
            settings.NotifyAnnouncements = request.NotifyAnnouncements.Value;
        if (request.NotifyNearbyUsers.HasValue)
            settings.NotifyNearbyUsers = request.NotifyNearbyUsers.Value;
        if (request.NotifyDockStatus.HasValue)
            settings.NotifyDockStatus = request.NotifyDockStatus.Value;
        if (request.NotifyRoadAlerts.HasValue)
            settings.NotifyRoadAlerts = request.NotifyRoadAlerts.Value;
        if (request.NotificationSound.HasValue)
            settings.NotificationSound = request.NotificationSound.Value;
        if (request.NotificationVibration.HasValue)
            settings.NotificationVibration = request.NotificationVibration.Value;

        if (request.VisibleToNearby.HasValue)
            settings.VisibleToNearby = request.VisibleToNearby.Value;
        if (request.ShareLocationInChat.HasValue)
            settings.ShareLocationInChat = request.ShareLocationInChat.Value;
        if (request.ShowOnlineStatus.HasValue)
            settings.ShowOnlineStatus = request.ShowOnlineStatus.Value;
        if (request.ShowReadReceipts.HasValue)
            settings.ShowReadReceipts = request.ShowReadReceipts.Value;

        if (request.DefaultChatRadiusMiles.HasValue)
            settings.DefaultChatRadiusMiles = Math.Clamp(request.DefaultChatRadiusMiles.Value, 0.1, 1.0);
        if (request.AutoJoinNearbyChats.HasValue)
            settings.AutoJoinNearbyChats = request.AutoJoinNearbyChats.Value;

        settings.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return MapToResponse(settings);
    }

    private async Task<UserSettings> CreateDefaultSettingsAsync(Guid userId)
    {
        var settings = new UserSettings
        {
            SettingsId = Guid.NewGuid(),
            UserId = userId,
            DndEnabled = false,
            DndScheduleEnabled = false,
            NotifyChat = true,
            NotifyAnnouncements = true,
            NotifyNearbyUsers = true,
            NotifyDockStatus = true,
            NotifyRoadAlerts = true,
            NotificationSound = true,
            NotificationVibration = true,
            VisibleToNearby = true,
            ShareLocationInChat = true,
            ShowOnlineStatus = true,
            ShowReadReceipts = true,
            DefaultChatRadiusMiles = 0.5,
            AutoJoinNearbyChats = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.UserSettings.Add(settings);
        await _db.SaveChangesAsync();

        return settings;
    }

    private static UserSettingsResponse MapToResponse(UserSettings settings)
    {
        return new UserSettingsResponse
        {
            DndEnabled = settings.DndEnabled,
            DndScheduleStart = settings.DndScheduleStart,
            DndScheduleEnd = settings.DndScheduleEnd,
            DndScheduleEnabled = settings.DndScheduleEnabled,
            NotifyChat = settings.NotifyChat,
            NotifyAnnouncements = settings.NotifyAnnouncements,
            NotifyNearbyUsers = settings.NotifyNearbyUsers,
            NotifyDockStatus = settings.NotifyDockStatus,
            NotifyRoadAlerts = settings.NotifyRoadAlerts,
            NotificationSound = settings.NotificationSound,
            NotificationVibration = settings.NotificationVibration,
            VisibleToNearby = settings.VisibleToNearby,
            ShareLocationInChat = settings.ShareLocationInChat,
            ShowOnlineStatus = settings.ShowOnlineStatus,
            ShowReadReceipts = settings.ShowReadReceipts,
            DefaultChatRadiusMiles = settings.DefaultChatRadiusMiles,
            AutoJoinNearbyChats = settings.AutoJoinNearbyChats,
            UpdatedAt = settings.UpdatedAt
        };
    }
}
