using ondock.api.DTOs.Chat;

namespace ondock.api.Services.Interfaces;

public interface IEncryptionService
{
    Task<PublicKeyResponse> RegisterKeyAsync(Guid userId, RegisterKeyRequest request);
    Task<PublicKeyResponse?> GetPublicKeyAsync(Guid userId);
    Task<KeyExchangeResponse> InitiateKeyExchangeAsync(Guid userId, KeyExchangeRequest request);
    Task<IReadOnlyList<KeyExchangeResponse>> GetPendingKeyExchangesAsync(Guid userId, string chatId);
}
