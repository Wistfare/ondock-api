using ondock.api.Data.Entities;
using ondock.api.DTOs.Auth;

namespace ondock.api.Services.Interfaces;

public interface IAuthService
{
    Task<RegisterResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> GoogleLoginAsync(OAuthLoginRequest request);
    Task<AuthResponse> AppleLoginAsync(OAuthLoginRequest request);
    Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request);
    Task LogoutAsync(Guid userId, string? deviceId = null);
    Task<bool> RevokeAllSessionsAsync(Guid userId);
    Task<string> GeneratePasswordResetTokenAsync(string email);
    Task ResetPasswordAsync(ResetPasswordRequest request);
    Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request);
    Task<string> GenerateEmailConfirmationTokenAsync(string email);
    Task ConfirmEmailAsync(ConfirmEmailRequest request);

    /// <summary>
    /// Send confirmation code to user's email (code-based confirmation)
    /// </summary>
    Task<ConfirmationCodeResponse> SendConfirmationCodeAsync(string email);

    /// <summary>
    /// Verify the confirmation code entered by user
    /// </summary>
    Task<ConfirmationCodeResponse> VerifyConfirmationCodeAsync(VerifyConfirmationCodeRequest request);

    /// <summary>
    /// Get the current email confirmation method configured
    /// </summary>
    string GetConfirmationMethod();

    Task<User?> GetUserByIdAsync(Guid userId);
    Task DeleteAccountAsync(Guid userId);
}
