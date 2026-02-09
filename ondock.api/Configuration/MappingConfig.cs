using Mapster;
using ondock.api.Data.Entities;
using ondock.api.DTOs.Auth;

namespace ondock.api.Configuration;

public static class MappingConfig
{
    public static void RegisterMappings()
    {
        // User to UserInfo mapping
        TypeAdapterConfig<User, UserInfo>
            .NewConfig()
            .Map(dest => dest.UserId, src => src.Id)
            .Map(dest => dest.Email, src => src.Email ?? string.Empty)
            .Map(dest => dest.FirstName, src => src.FirstName)
            .Map(dest => dest.LastName, src => src.LastName)
            .Map(dest => dest.PhoneNumber, src => src.PhoneNumber)
            .Map(dest => dest.EmailConfirmed, src => src.EmailConfirmed)
            .Map(dest => dest.TwoFactorEnabled, src => src.TwoFactorEnabled)
            .Map(dest => dest.OAuthProvider, src => src.OAuthProvider);
    }
}
