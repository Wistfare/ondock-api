using ondock.api.DTOs.Contact;

namespace ondock.api.Services.Interfaces;

public interface IContactService
{
    Task<GenerateQRCodeResponse> GenerateQRCodeAsync(Guid userId, Guid? chatId);
    Task<GenerateQrCodeResult> GenerateQrCodeAsync(Guid userId);
    Task<ScanQRCodeResponse> ScanQRCodeAsync(Guid userId, ScanQRCodeRequest request);
    Task<ScanQrCodeResult> ScanQrCodeAsync(Guid userId, string qrData);
    Task<PermanentContactsResponse> GetPermanentContactsAsync(Guid userId);
    Task<IReadOnlyList<PermanentContactDto>> GetPermanentContactsListAsync(Guid userId);
    Task<bool> IsPermanentContactAsync(Guid userId1, Guid userId2);
    Task<bool> IsPermanentContactAsync(Guid userId, string anonymousUserId);
    Task RemovePermanentContactAsync(Guid userId, Guid contactId);
}

public record GenerateQrCodeResult(string QrData, DateTime ExpiresAt);
public record ScanQrCodeResult(bool Success, string? Message, Guid? ContactId, string? ContactDisplayName);
public record PermanentContactDto(Guid ContactId, Guid UserId, string? DisplayName, string? ColorName, DateTime AddedAt);
