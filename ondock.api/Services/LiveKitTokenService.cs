using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ondock.api.Configuration;
using ondock.api.Services.Interfaces;

namespace ondock.api.Services;

public class LiveKitTokenService : ILiveKitTokenService
{
    private readonly LiveKitSettings _settings;
    private readonly SymmetricSecurityKey _key;

    public LiveKitTokenService(IOptions<LiveKitSettings> options)
    {
        _settings = options.Value;
        _key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_settings.ApiSecret));
    }

    public string GenerateToken(string roomName, string identity, bool canPublish, bool canSubscribe, TimeSpan? ttl = null)
    {
        if (string.IsNullOrEmpty(_settings.ApiKey) || string.IsNullOrEmpty(_settings.ApiSecret))
        {
            throw new InvalidOperationException("LiveKit API key/secret are not configured.");
        }

        var now = DateTime.UtcNow;
        var expires = now.Add(ttl ?? TimeSpan.FromHours(6));

        var videoGrant = new Dictionary<string, object>
        {
            ["room"] = roomName,
            ["roomJoin"] = true,
            ["canPublish"] = canPublish,
            ["canSubscribe"] = canSubscribe,
        };

        var payload = new JwtPayload(
            issuer: _settings.ApiKey,
            audience: null,
            claims: null,
            notBefore: now,
            expires: expires
        );

        payload["video"] = videoGrant;
        payload["sub"] = identity;

        var header = new JwtHeader(new SigningCredentials(_key, SecurityAlgorithms.HmacSha256));
        var token = new JwtSecurityToken(header, payload);

        var handler = new JwtSecurityTokenHandler();
        return handler.WriteToken(token);
    }
}
