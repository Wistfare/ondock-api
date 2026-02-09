using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using System.IO;
using Microsoft.EntityFrameworkCore;
using ondock.api.Data;
using ondock.api.Data.Entities;
using ondock.api.DTOs.LoadView;
using ondock.api.DTOs.Notifications;
using ondock.api.Services.Interfaces;

namespace ondock.api.Services;

public class FcmNotificationService : IFcmNotificationService
{
    private readonly OnDockDbContext _db;
    private readonly ILogger<FcmNotificationService> _logger;
    private readonly IMagicPushService _magicPush;
    private static bool _initialized;

    public FcmNotificationService(OnDockDbContext db, ILogger<FcmNotificationService> logger, IMagicPushService magicPush, IConfiguration configuration)
    {
        _db = db;
        _logger = logger;
        _magicPush = magicPush;

        if (!_initialized)
        {
            try
            {
                // Expecting a GOOGLE_APPLICATION_CREDENTIALS environment variable or inline JSON in config
                if (FirebaseApp.DefaultInstance == null)
                {
                    var serviceAccountJson = configuration["Firebase:ServiceAccountJson"];
                    var serviceAccountPath = configuration["Firebase:ServiceAccountPath"];
                    var projectId = configuration["Firebase:ProjectId"];

                    _logger.LogInformation("Initializing Firebase Admin SDK. ProjectId={ProjectId}, HasJson={HasJson}, HasPath={HasPath}, PathExists={PathExists}",
                        projectId ?? "(not set)",
                        !string.IsNullOrWhiteSpace(serviceAccountJson),
                        !string.IsNullOrWhiteSpace(serviceAccountPath),
                        !string.IsNullOrWhiteSpace(serviceAccountPath) && File.Exists(serviceAccountPath));

                    GoogleCredential credential;
                    if (!string.IsNullOrWhiteSpace(serviceAccountJson))
                    {
                        // Handle escaped newlines in private_key when set via environment variable
                        var fixedJson = serviceAccountJson.Replace("\\n", "\n");
                        credential = GoogleCredential.FromJson(fixedJson);
                        _logger.LogInformation("Firebase initialized from inline ServiceAccountJson");
                    }
                    else if (!string.IsNullOrWhiteSpace(serviceAccountPath) && File.Exists(serviceAccountPath))
                    {
                        credential = GoogleCredential.FromFile(serviceAccountPath);
                        _logger.LogInformation("Firebase initialized from file: {Path}", serviceAccountPath);
                    }
                    else
                    {
                        _logger.LogWarning("No Firebase service account configured. Trying Application Default Credentials (ADC)...");
                        credential = GoogleCredential.GetApplicationDefault();
                        _logger.LogInformation("Firebase initialized from Application Default Credentials");
                    }

                    FirebaseApp.Create(new AppOptions
                    {
                        Credential = credential,
                        ProjectId = string.IsNullOrWhiteSpace(projectId) ? null : projectId,
                    });
                }
                _initialized = true;
                _logger.LogInformation("Firebase Admin SDK initialized successfully. FCM notifications enabled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Firebase initialization FAILED. FCM notifications will NOT work. Configure Firebase:ServiceAccountPath or Firebase:ServiceAccountJson in appsettings.");
            }
        }
    }

    public async Task RegisterDeviceTokenAsync(Guid userId, RegisterDeviceTokenRequest request, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        var platform = Platform.Android;
        if (!string.IsNullOrEmpty(request.Platform) &&
            Enum.TryParse<Platform>(request.Platform, true, out var parsed))
        {
            platform = parsed;
        }

        var existingByToken = await _db.DeviceTokens
            .FirstOrDefaultAsync(t => t.Token == request.Token, ct);

        if (existingByToken != null)
        {
            existingByToken.UserId = userId;
            existingByToken.DeviceId = request.DeviceId;
            existingByToken.Platform = platform;
            existingByToken.IsPrimary = request.IsPrimary;
            existingByToken.LastUsedAt = now;
            await _db.SaveChangesAsync(ct);

            await TryRegisterMagicPushAsync(userId, request.DeviceId, platform, request.Token);
            return;
        }

        var existingByDevice = await _db.DeviceTokens
            .FirstOrDefaultAsync(t => t.UserId == userId && t.DeviceId == request.DeviceId, ct);

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
            _db.DeviceTokens.Add(token);
        }

        await _db.SaveChangesAsync(ct);
        await TryRegisterMagicPushAsync(userId, request.DeviceId, platform, request.Token);
    }

    public async Task SendChatMessageAsync(IEnumerable<Guid> recipientUserIds, string chatId, Guid messageId, string title, string body, MessageType messageType, CancellationToken ct = default)
    {
        if (!_initialized)
        {
            _logger.LogWarning("FCM: Skipping chat notification - Firebase not initialized. ChatId={ChatId}, MessageId={MessageId}", chatId, messageId);
            return;
        }

        var recipientIds = recipientUserIds
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToArray();

        if (recipientIds.Length == 0)
        {
            _logger.LogDebug("FCM: No recipients for chat notification. ChatId={ChatId}", chatId);
            return;
        }

        _logger.LogInformation("FCM: Sending chat notification to {RecipientCount} recipients. ChatId={ChatId}, MessageId={MessageId}", 
            recipientIds.Length, chatId, messageId);

        var tokens = await _db.DeviceTokens
            .Where(t => recipientIds.Contains(t.UserId) && t.Token != null)
            .Select(t => t.Token!)
            .Distinct()
            .ToListAsync(ct);

        if (!tokens.Any())
        {
            _logger.LogWarning("FCM: No device tokens found for recipients. ChatId={ChatId}, RecipientIds={RecipientIds}", 
                chatId, string.Join(",", recipientIds));
            return;
        }

        _logger.LogInformation("FCM: Found {TokenCount} device tokens for chat notification. ChatId={ChatId}", tokens.Count, chatId);

        var data = new Dictionary<string, string>
        {
            ["type"] = "chat_message",
            ["chatId"] = chatId,
            ["messageId"] = messageId.ToString(),
            ["messageType"] = messageType.ToString(),
        };

        await SendToTokensAsync(tokens, title, body, data, ct);
    }

    public async Task SendDockLightStatusAsync(Guid userId, DockLightStatusType status, string deviceId, DockLightStatusSource source, string? reason, CancellationToken ct = default)
    {
        if (!_initialized)
        {
            _logger.LogDebug("Skipping FCM send; Firebase not initialized.");
            return;
        }

        // Fetch all device tokens for the user sessions (assuming tokens stored in DeviceTokens table)
        var tokens = await _db.DeviceTokens
            .Where(t => t.UserId == userId)
            .Select(t => t.Token)
            .Where(t => t != null)
            .Distinct()
            .ToListAsync(ct);

        if (!tokens.Any())
        {
            _logger.LogDebug("No device tokens found for user {UserId}; skipping notification", userId);
            return;
        }

        var title = "Dock Light Update";
        var body = status switch
        {
            DockLightStatusType.Red => "Red light detected",
            DockLightStatusType.Green => "Green light detected",
            DockLightStatusType.Obstacle => "Obstacle detected",
            DockLightStatusType.ConnectionLost => "Connection lost",
            _ => "Status changed"
        };

        var data = new Dictionary<string, string>
        {
            ["type"] = "dock_light_status",
            ["status"] = status.ToString(),
            ["deviceId"] = deviceId,
            ["source"] = source.ToString(),
            ["timestamp"] = DateTime.UtcNow.ToString("O")
        };
        if (!string.IsNullOrWhiteSpace(reason))
        {
            data["reason"] = reason!;
        }

        await SendToTokensAsync(tokens, title, body, data, ct);
    }

    public async Task SendLoadViewRequestNearbyAsync(Guid userId, LoadViewRequestDto request, double distanceMeters, CancellationToken ct = default)
    {
        if (!_initialized) return;

        var tokens = await GetUserTokensAsync(userId, ct);
        if (!tokens.Any()) return;

        var distanceText = distanceMeters < 1000 
            ? $"{distanceMeters:F0}m away" 
            : $"{distanceMeters / 1000:F1}km away";

        var title = "Road View Request Nearby";
        var body = $"Someone {distanceText} is requesting a road view. Tap to respond.";

        var data = new Dictionary<string, string>
        {
            ["type"] = "loadview_request",
            ["requestId"] = request.RequestId.ToString(),
            ["latitude"] = request.Latitude.ToString(),
            ["longitude"] = request.Longitude.ToString(),
            ["distance"] = distanceMeters.ToString("F0"),
            ["urgency"] = request.UrgencyLevel.ToString()
        };

        await SendToTokensAsync(tokens, title, body, data, ct);
    }

    public async Task SendLoadViewResponseReceivedAsync(Guid userId, LoadViewResponseDto response, CancellationToken ct = default)
    {
        if (!_initialized) return;

        var tokens = await GetUserTokensAsync(userId, ct);
        if (!tokens.Any()) return;

        var title = "Road View Response Received";
        var body = response.ResponderName != null 
            ? $"{response.ResponderName} responded to your road view request"
            : "Someone responded to your road view request";

        var data = new Dictionary<string, string>
        {
            ["type"] = "loadview_response",
            ["requestId"] = response.RequestId.ToString(),
            ["responseId"] = response.ResponseId.ToString(),
            ["isLiveStream"] = response.IsLiveStream.ToString()
        };

        await SendToTokensAsync(tokens, title, body, data, ct);
    }

    public async Task SendLoadViewLiveStreamStartedAsync(Guid userId, LoadViewResponseDto response, CancellationToken ct = default)
    {
        if (!_initialized) return;

        var tokens = await GetUserTokensAsync(userId, ct);
        if (!tokens.Any()) return;

        var title = "Live Road View Started";
        var body = response.ResponderName != null 
            ? $"{response.ResponderName} started a live stream for your request"
            : "Someone started a live stream for your request";

        var data = new Dictionary<string, string>
        {
            ["type"] = "loadview_live",
            ["requestId"] = response.RequestId.ToString(),
            ["responseId"] = response.ResponseId.ToString(),
            ["roomName"] = response.LiveKitRoomName ?? ""
        };

        await SendToTokensAsync(tokens, title, body, data, ct);
    }

    private async Task<List<string>> GetUserTokensAsync(Guid userId, CancellationToken ct)
    {
        return await _db.DeviceTokens
            .Where(t => t.UserId == userId && t.Token != null)
            .Select(t => t.Token!)
            .Distinct()
            .ToListAsync(ct);
    }

    private async Task TryRegisterMagicPushAsync(Guid userId, string deviceId, Platform platform, string token)
    {
        try
        {
            await _magicPush.RegisterDeviceAsync(userId, deviceId, platform, token);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MagicPush device registration failed for user {UserId} device {DeviceId}", userId, deviceId);
        }
    }

    private async Task SendToTokensAsync(List<string> tokens, string title, string body, Dictionary<string, string> data, CancellationToken ct)
    {
        foreach (var token in tokens)
        {
            var message = new FirebaseAdmin.Messaging.Message
            {
                Token = token,
                Notification = new Notification
                {
                    Title = title,
                    Body = body
                },
                Data = data
            };

            try
            {
                var response = await FirebaseMessaging.DefaultInstance.SendAsync(message, ct);
                _logger.LogDebug("Sent FCM notification: {Response}", response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send FCM message to token {Token}", token);
            }
        }
    }
}
