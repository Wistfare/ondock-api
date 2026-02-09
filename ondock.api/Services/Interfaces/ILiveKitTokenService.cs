using System;

namespace ondock.api.Services.Interfaces;

public interface ILiveKitTokenService
{
    string GenerateToken(string roomName, string identity, bool canPublish, bool canSubscribe, TimeSpan? ttl = null);
}
