using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ondock.api.Configuration;
using ondock.api.Data;
using ondock.api.Data.Entities;
using ondock.api.DTOs.Notifications;
using ondock.api.Services.Interfaces;

namespace ondock.api.Services;

public class FirebaseMessagingService : IFirebaseMessagingService
{
    private readonly OnDockDbContext _context;
    private readonly FirebaseSettings _settings;
    private readonly ILogger<FirebaseMessagingService> _logger;
    private readonly IMagicPushService _magicPush;
    private static readonly HttpClient HttpClient = new();

    public FirebaseMessagingService(
        OnDockDbContext context,
        IOptions<FirebaseSettings> options,
        ILogger<FirebaseMessagingService> logger,
        IMagicPushService magicPush)
    {
        _context = context;
        _settings = options.Value;
        _logger = logger;
        _magicPush = magicPush;
    }

    public async Task SendStatusChangeNotificationAsync(Guid userId, Guid sessionId, string deviceId, int previousStatus, int newStatus)
    {
        if (string.IsNullOrEmpty(_settings.ServerKey))
        {
            _logger.LogWarning("Firebase server key not configured; skipping status change notification.");
            return;
        }

        var tokens = await _context.DeviceTokens
            .Where(t => t.UserId == userId)
            .Select(t => t.Token)
            .ToListAsync();

        if (!tokens.Any())
        {
            _logger.LogInformation("No device tokens found for user {UserId}", userId);
            return;
        }

        string StatusLabel(int s) => s switch
        {
            1 => "LOADING",
            2 => "COMPLETED",
            _ => "NO LIGHT"
        };

        var title = "Monitoring status changed";
        var body = $"{StatusLabel(previousStatus)} → {StatusLabel(newStatus)}";

        var data = new Dictionary<string, string>
        {
            ["action"] = "statusChange",
            ["sessionId"] = sessionId.ToString(),
            ["deviceId"] = deviceId,
            ["previousStatus"] = previousStatus.ToString(),
            ["newStatus"] = newStatus.ToString(),
        };

        var payload = new
        {
            registration_ids = tokens,
            priority = "high",
            data,
            notification = new
            {
                title,
                body,
            }
        };

        var json = JsonSerializer.Serialize(payload);
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://fcm.googleapis.com/fcm/send");
        request.Headers.TryAddWithoutValidation("Authorization", $"key={_settings.ServerKey}");
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await HttpClient.SendAsync(request);
        var success = response.IsSuccessStatusCode;

        try
        {
            var log = new NotificationLog
            {
                NotificationId = Guid.NewGuid(),
                UserId = userId,
                Type = NotificationType.SystemAnnouncement,
                Title = title,
                Body = body,
                Data = JsonSerializer.Serialize(data),
                SentAt = DateTime.UtcNow,
                DeviceCount = tokens.Count,
                DeliveryStatus = success ? DeliveryStatus.Sent : DeliveryStatus.Failed
            };
            _context.NotificationLogs.Add(log);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log status change notification for user {UserId}", userId);
        }
    }

    public async Task RegisterDeviceTokenAsync(Guid userId, RegisterDeviceTokenRequest request)
    {
        var now = DateTime.UtcNow;

        var platform = Platform.Android;
        if (!string.IsNullOrEmpty(request.Platform) &&
            Enum.TryParse<Platform>(request.Platform, true, out var parsed))
        {
            platform = parsed;
        }

        // Check if this token already exists (for any user/device) due to unique constraint on Token
        var existingByToken = await _context.DeviceTokens
            .FirstOrDefaultAsync(t => t.Token == request.Token);

        if (existingByToken != null)
        {
            // Token already exists - update it to point to current user/device
            existingByToken.UserId = userId;
            existingByToken.DeviceId = request.DeviceId;
            existingByToken.Platform = platform;
            existingByToken.IsPrimary = request.IsPrimary;
            existingByToken.LastUsedAt = now;
            await _context.SaveChangesAsync();

            try
            {
                await _magicPush.RegisterDeviceAsync(userId, request.DeviceId, platform, request.Token);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MagicPush device registration failed for user {UserId} device {DeviceId}", userId, request.DeviceId);
            }
            return;
        }

        // Check if user+device combo exists with a different token
        var existingByDevice = await _context.DeviceTokens
            .FirstOrDefaultAsync(t => t.UserId == userId && t.DeviceId == request.DeviceId);

        if (existingByDevice != null)
        {
            existingByDevice.Token = request.Token;
            existingByDevice.Platform = platform;
            existingByDevice.IsPrimary = request.IsPrimary;
            existingByDevice.LastUsedAt = now;
        }
        else
        {
            var token = new DeviceToken
            {
                TokenId = Guid.NewGuid(),
                UserId = userId,
                DeviceId = request.DeviceId,
                Token = request.Token,
                Platform = platform,
                IsPrimary = request.IsPrimary,
                RegisteredAt = now,
                LastUsedAt = now,
            };
            _context.DeviceTokens.Add(token);
        }

        await _context.SaveChangesAsync();

        try
        {
            await _magicPush.RegisterDeviceAsync(userId, request.DeviceId, platform, request.Token);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MagicPush device registration failed for user {UserId} device {DeviceId}", userId, request.DeviceId);
        }
    }

    public async Task SendStartStreamingNotificationAsync(Guid userId, string deviceId, Guid sessionId, string requesterDeviceId)
    {
        if (string.IsNullOrEmpty(_settings.ServerKey))
        {
            _logger.LogWarning("Firebase server key not configured; skipping push notification.");
            return;
        }

        var tokens = await _context.DeviceTokens
            .Where(t => t.UserId == userId && t.DeviceId == deviceId)
            .Select(t => t.Token)
            .ToListAsync();

        if (!tokens.Any())
        {
            _logger.LogInformation("No device tokens found for user {UserId} / device {DeviceId}", userId, deviceId);
            return;
        }

        var data = new Dictionary<string, string>
        {
            ["action"] = "startStreaming",
            ["sessionId"] = sessionId.ToString(),
            ["requesterDeviceId"] = requesterDeviceId,
        };

        var payload = new
        {
            registration_ids = tokens,
            priority = "high",
            data,
            notification = new
            {
                title = "Monitoring stream requested",
                body = "A viewer has requested to see your monitoring stream.",
            }
        };

        var json = JsonSerializer.Serialize(payload);
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://fcm.googleapis.com/fcm/send");
        request.Headers.TryAddWithoutValidation("Authorization", $"key={_settings.ServerKey}");
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await HttpClient.SendAsync(request);
        var success = response.IsSuccessStatusCode;

        try
        {
            var log = new NotificationLog
            {
                NotificationId = Guid.NewGuid(),
                UserId = userId,
                Type = NotificationType.SystemAnnouncement,
                Title = "Monitoring stream requested",
                Body = "A viewer has requested to see your monitoring stream.",
                Data = JsonSerializer.Serialize(data),
                SentAt = DateTime.UtcNow,
                DeviceCount = tokens.Count,
                DeliveryStatus = success ? DeliveryStatus.Sent : DeliveryStatus.Failed
            };
            _context.NotificationLogs.Add(log);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log notification for user {UserId}", userId);
        }
    }
}
