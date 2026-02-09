using Microsoft.Extensions.Options;
using ondock.api.Configuration;
using ondock.api.Services.Interfaces;

namespace ondock.api.Services;

public class ChatSettingsProvider : IChatSettingsProvider
{
    private readonly object _lock = new();
    private ChatSettings _settings;

    public ChatSettingsProvider(IOptions<ChatSettings> options)
    {
        _settings = Clone(options.Value);
    }

    public ChatSettings Get()
    {
        lock (_lock)
        {
            return Clone(_settings);
        }
    }

    public void Update(ChatSettings settings)
    {
        lock (_lock)
        {
            _settings = Clone(settings);
        }
    }

    private static ChatSettings Clone(ChatSettings s)
    {
        return new ChatSettings
        {
            DefaultRadiusMiles = s.DefaultRadiusMiles,
            MaxRadiusMiles = s.MaxRadiusMiles,
            SpeedThresholdMph = s.SpeedThresholdMph,
            RoomExpiryMinutes = s.RoomExpiryMinutes,
            PresenceMaxAgeSeconds = s.PresenceMaxAgeSeconds,
            DeleteRoomOnAnyExit = s.DeleteRoomOnAnyExit,
            NearbyUserColor = s.NearbyUserColor,
            RestrictedColors = new List<string>(s.RestrictedColors ?? new List<string>()),
            RandomColorPalette = new List<string>(s.RandomColorPalette ?? new List<string>()),
            BypassNearbyRestrictions = s.BypassNearbyRestrictions
        };
    }
}
