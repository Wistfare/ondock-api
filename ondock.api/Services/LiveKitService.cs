using System.Text.Json;
using Google.Protobuf;
using Livekit.Server.Sdk.Dotnet;
using Microsoft.Extensions.Options;
using ondock.api.Configuration;
using ondock.api.Data.Entities;
using ondock.api.Services.Interfaces;

namespace ondock.api.Services;

public class LiveKitService : ILiveKitService
{
    private readonly ILogger<LiveKitService> _logger;
    private readonly LiveKitSettings _settings;

    public LiveKitService(ILogger<LiveKitService> logger, IOptions<LiveKitSettings> settings)
    {
        _logger = logger;
        _settings = settings.Value;
    }

    public async Task PublishDockLightStatusAsync(Guid userId, DockLightStatusType status, string deviceId, DockLightStatusSource source, string? reason, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.Url) || string.IsNullOrWhiteSpace(_settings.ApiKey) || string.IsNullOrWhiteSpace(_settings.ApiSecret))
        {
            _logger.LogDebug("LiveKit settings missing; skipping real-time publish.");
            return;
        }

        // Compose room name (one room per user - can evolve later to multi-user room)
        var roomName = $"{_settings.DefaultRoomPrefix}-user-{userId}";

        // Create a client instance (could be pooled later)
        var client = new RoomServiceClient(_settings.Url, _settings.ApiKey, _settings.ApiSecret);

        // Prepare payload
        var payload = new
        {
            type = "dock_light_status",
            userId,
            deviceId,
            status = status.ToString(),
            source = source.ToString(),
            reason,
            timestamp = DateTime.UtcNow
        };
        var json = JsonSerializer.Serialize(payload);

        var sendDataRequest = new SendDataRequest
        {
            Room = roomName,
            Data = ByteString.CopyFromUtf8(json),
            Kind = DataPacket.Types.Kind.Reliable
        };

        try
        {
            await client.SendData(sendDataRequest);
            _logger.LogInformation("LiveKit data sent to room {Room}: {Json}", roomName, json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send LiveKit data for user {UserId}", userId);
        }
    }

    public Task<LiveKitAccessTokenResponse> GenerateAccessTokenAsync(string roomName, string identity, bool isPublisher, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey) || string.IsNullOrWhiteSpace(_settings.ApiSecret))
        {
            throw new InvalidOperationException("LiveKit settings not configured.");
        }

        // Generate proper LiveKit JWT token using the SDK
        var ttl = TimeSpan.FromHours(1);
        var expires = DateTime.UtcNow.Add(ttl);

        // Configure grants based on publisher/viewer role
        var grants = new VideoGrants
        {
            RoomJoin = true,
            Room = roomName,
            CanPublish = isPublisher,
            CanSubscribe = true,
            CanPublishData = isPublisher
        };

        var token = new AccessToken(_settings.ApiKey, _settings.ApiSecret)
            .WithIdentity(identity)
            .WithGrants(grants)
            .WithTtl(ttl)
            .ToJwt();

        _logger.LogDebug("Generated LiveKit token for identity {Identity} in room {Room} (Publisher: {IsPublisher})", identity, roomName, isPublisher);

        return Task.FromResult(new LiveKitAccessTokenResponse
        {
            Room = roomName,
            Identity = identity,
            Token = token,
            ExpiresAt = expires
        });
    }
}
