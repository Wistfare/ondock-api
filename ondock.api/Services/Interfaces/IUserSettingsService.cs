using ondock.api.DTOs.Settings;

namespace ondock.api.Services.Interfaces;

public interface IUserSettingsService
{
    Task<UserSettingsResponse> GetSettingsAsync(Guid userId);
    Task<UserSettingsResponse> UpdateSettingsAsync(Guid userId, UpdateUserSettingsRequest request);
}
