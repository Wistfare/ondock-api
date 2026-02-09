using ondock.api.DTOs.Chat;

namespace ondock.api.Services.Interfaces;

public interface ILocationService
{
    Task<NearbyUsersResponse> GetNearbyUsersAsync(Guid userId, double latitude, double longitude, double speedMph, double? radiusMiles);
    Task UpdateUserLocationAsync(Guid userId, double latitude, double longitude, double speedMph);
}
