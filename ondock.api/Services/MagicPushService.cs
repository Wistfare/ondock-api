using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using ondock.api.Configuration;
using ondock.api.Data;
using ondock.api.Data.Entities;
using ondock.api.Services.Interfaces;

namespace ondock.api.Services;

public class MagicPushService : IMagicPushService
{
    private readonly HttpClient _httpClient;
    private readonly MagicPushSettings _settings;
    private readonly ILogger<MagicPushService> _logger;
    private readonly OnDockDbContext _db;

    public MagicPushService(
        IHttpClientFactory httpClientFactory,
        IOptions<MagicPushSettings> options,
        ILogger<MagicPushService> logger,
        OnDockDbContext db)
    {
        _httpClient = httpClientFactory.CreateClient("magicpush");
        _settings = options.Value;
        _logger = logger;
        _db = db;
    }

    public async Task SendMonitoringCompletedAsync(Guid userId, Guid sessionId)
    {
        if (string.IsNullOrWhiteSpace(_settings.BaseUrl) || string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            _logger.LogWarning("MagicPush not configured; skipping monitoring completed push.");
            return;
        }

        var recipientHashes = await GetMagicPushRecipientHashesForUserAsync(userId);
        if (recipientHashes.Count == 0)
        {
            return;
        }

        var url = new Uri(new Uri(_settings.BaseUrl, UriKind.Absolute), "/api/v1/rest/send");

        var payload = new
        {
            title = "Monitoring completed",
            message = "Monitoring completed on another device.",
            recipients = string.Join(",", recipientHashes)
        };

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(payload, options: new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                }),
            };

            request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {_settings.ApiKey}");

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("MagicPush monitoring_completed send failed: {Status} {Body}", (int)response.StatusCode, body);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send monitoring_completed notification via MagicPush for user {UserId} session {SessionId}", userId, sessionId);
        }
    }

    public async Task<string?> RegisterDeviceAsync(Guid userId, string deviceId, Platform platform, string token)
    {
        if (string.IsNullOrWhiteSpace(_settings.BaseUrl) || string.IsNullOrWhiteSpace(_settings.AppHash))
        {
            _logger.LogWarning("MagicPush BaseUrl/AppHash not configured; skipping device registration.");
            return null;
        }

        if (string.IsNullOrWhiteSpace(deviceId) || string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        try
        {
            var profile = await _db.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
            if (profile == null)
            {
                // Get default category for new profile
                var defaultCategory = await _db.ProfileCategories.FirstOrDefaultAsync();
                if (defaultCategory == null)
                {
                    _logger.LogWarning("No profile categories exist, cannot create profile for push registration");
                    return null;
                }
                profile = new UserProfile
                {
                    ProfileId = Guid.NewGuid(),
                    UserId = userId,
                    CategoryId = defaultCategory.CategoryId,
                    Preferences = null,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _db.UserProfiles.Add(profile);
                await _db.SaveChangesAsync();
            }

            var existingAppUserHash = ReadMagicPushAppUserHash(profile.Preferences, deviceId);

            string? appUserHash;
            if (string.IsNullOrWhiteSpace(existingAppUserHash))
            {
                appUserHash = await CreateAppUserAsync(platform, token);
            }
            else
            {
                appUserHash = await UpdateAppUserAsync(existingAppUserHash, platform, token);
            }

            if (string.IsNullOrWhiteSpace(appUserHash))
            {
                return null;
            }

            profile.Preferences = WriteMagicPushAppUserHash(profile.Preferences, deviceId, appUserHash);
            profile.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return appUserHash;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MagicPush device registration failed for user {UserId} device {DeviceId}", userId, deviceId);
            return null;
        }
    }

    public async Task SendChatMessagePushAsync(Guid chatId, IReadOnlyList<Guid> recipientUserIds)
    {
        if (string.IsNullOrWhiteSpace(_settings.BaseUrl) || string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            return;
        }

        if (recipientUserIds.Count == 0)
        {
            return;
        }

        var recipientHashes = await GetMagicPushRecipientHashesForUsersAsync(recipientUserIds);
        if (recipientHashes.Count == 0)
        {
            return;
        }

        var url = new Uri(new Uri(_settings.BaseUrl, UriKind.Absolute), "/api/v1/rest/send");
        var payload = new
        {
            title = _settings.ChatMessageTitle ?? "New message",
            message = "You have a new message in OnDock.",
            action = $"chat:{chatId}",
            recipients = string.Join(",", recipientHashes)
        };

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(payload, options: new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                }),
            };

            request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {_settings.ApiKey}");

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("MagicPush chat message send failed: {Status} {Body}", (int)response.StatusCode, body);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send chat message push via MagicPush for chat {ChatId}", chatId);
        }
    }

    private async Task<string?> CreateAppUserAsync(Platform platform, string token)
    {
        if (string.IsNullOrWhiteSpace(_settings.BaseUrl) || string.IsNullOrWhiteSpace(_settings.AppHash))
        {
            return null;
        }

        var url = new Uri(new Uri(_settings.BaseUrl, UriKind.Absolute), $"/api/v1/sdk/app-user/{_settings.AppHash}");
        var payload = new Dictionary<string, string>();
        if (platform == Platform.iOS)
        {
            payload["apn_device_token"] = token;
        }
        else
        {
            payload["fcm_token"] = token;
        }

        try
        {
            var res = await _httpClient.PostAsJsonAsync(url, payload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            });

            if (!res.IsSuccessStatusCode)
            {
                var body = await res.Content.ReadAsStringAsync();
                _logger.LogWarning("MagicPush create app user failed: {Status} {Body}", (int)res.StatusCode, body);
                return null;
            }

            var json = await res.Content.ReadAsStringAsync();
            return TryReadHashFromJson(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MagicPush create app user failed");
            return null;
        }
    }

    private async Task<string?> UpdateAppUserAsync(string appUserHash, Platform platform, string token)
    {
        if (string.IsNullOrWhiteSpace(_settings.BaseUrl) || string.IsNullOrWhiteSpace(_settings.AppHash))
        {
            return null;
        }

        var url = new Uri(new Uri(_settings.BaseUrl, UriKind.Absolute), $"/api/v1/sdk/app-user/{_settings.AppHash}/{appUserHash}");
        var payload = new Dictionary<string, string>();
        if (platform == Platform.iOS)
        {
            payload["apn_device_token"] = token;
        }
        else
        {
            payload["fcm_token"] = token;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Put, url)
            {
                Content = JsonContent.Create(payload, options: new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                }),
            };

            var res = await _httpClient.SendAsync(request);

            if (!res.IsSuccessStatusCode)
            {
                var body = await res.Content.ReadAsStringAsync();
                _logger.LogWarning("MagicPush update app user failed: {Status} {Body}", (int)res.StatusCode, body);
                return null;
            }

            var json = await res.Content.ReadAsStringAsync();
            return TryReadHashFromJson(json) ?? appUserHash;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MagicPush update app user failed");
            return null;
        }
    }

    private static string? TryReadHashFromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return null;
            if (doc.RootElement.TryGetProperty("hash", out var hashEl) && hashEl.ValueKind == JsonValueKind.String)
            {
                return hashEl.GetString();
            }
        }
        catch
        {
            // ignore
        }

        return null;
    }

    private async Task<HashSet<string>> GetMagicPushRecipientHashesForUsersAsync(IReadOnlyList<Guid> userIds)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (userIds.Count == 0)
        {
            return set;
        }

        var profiles = await _db.UserProfiles
            .AsNoTracking()
            .Where(p => userIds.Contains(p.UserId))
            .Select(p => p.Preferences)
            .ToListAsync();

        foreach (var prefs in profiles)
        {
            foreach (var h in ReadAllMagicPushAppUserHashes(prefs))
            {
                if (!string.IsNullOrWhiteSpace(h))
                {
                    set.Add(h);
                }
            }
        }

        return set;
    }

    private async Task<HashSet<string>> GetMagicPushRecipientHashesForUserAsync(Guid userId)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var prefs = await _db.UserProfiles
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .Select(p => p.Preferences)
            .FirstOrDefaultAsync();

        if (prefs == null)
        {
            return set;
        }

        foreach (var h in ReadAllMagicPushAppUserHashes(prefs))
        {
            if (!string.IsNullOrWhiteSpace(h))
            {
                set.Add(h);
            }
        }

        return set;
    }

    private static string? ReadMagicPushAppUserHash(string? prefsJson, string deviceId)
    {
        if (string.IsNullOrWhiteSpace(prefsJson))
        {
            return null;
        }

        try
        {
            var root = JsonNode.Parse(prefsJson) as JsonObject;
            var magicPush = root?["magicPush"] as JsonObject;
            var hashes = magicPush?["appUserHashes"] as JsonObject;
            return hashes?[deviceId]?.GetValue<string>();
        }
        catch
        {
            return null;
        }
    }

    private static IReadOnlyList<string> ReadAllMagicPushAppUserHashes(string? prefsJson)
    {
        var list = new List<string>();
        if (string.IsNullOrWhiteSpace(prefsJson))
        {
            return list;
        }

        try
        {
            var root = JsonNode.Parse(prefsJson) as JsonObject;
            var magicPush = root?["magicPush"] as JsonObject;
            var hashes = magicPush?["appUserHashes"] as JsonObject;
            if (hashes == null)
            {
                return list;
            }

            foreach (var kv in hashes)
            {
                if (kv.Value == null) continue;
                var v = kv.Value.GetValue<string?>();
                if (!string.IsNullOrWhiteSpace(v))
                {
                    list.Add(v);
                }
            }
        }
        catch
        {
            // ignore
        }

        return list;
    }

    private static string WriteMagicPushAppUserHash(string? prefsJson, string deviceId, string appUserHash)
    {
        JsonObject root;
        try
        {
            root = (JsonNode.Parse(prefsJson ?? "{}") as JsonObject) ?? new JsonObject();
        }
        catch
        {
            root = new JsonObject();
        }

        var magicPush = root["magicPush"] as JsonObject ?? new JsonObject();
        var hashes = magicPush["appUserHashes"] as JsonObject ?? new JsonObject();
        hashes[deviceId] = appUserHash;
        magicPush["appUserHashes"] = hashes;
        root["magicPush"] = magicPush;
        return root.ToJsonString();
    }
}
