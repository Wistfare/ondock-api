using ondock.api.DTOs.Chat;

namespace ondock.api.Services.Interfaces;

public interface IIdentityService
{
    Task<IdentityRevealResponse> RequestIdentityRevealAsync(Guid userId, IdentityRevealRequest request);
    Task<IdentityRevealResponse> AcceptIdentityRevealAsync(Guid userId, string chatId, Guid requesterId);
    Task<IdentityRevealResponse?> GetIdentityRevealStatusAsync(Guid userId, string chatId, Guid otherUserId);
    Task<IReadOnlyList<IdentityRevealResponse>> GetPendingRevealRequestsAsync(Guid userId);
}
