using System.Security.Claims;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using ondock.api.Services.Interfaces;

// Aliases to resolve ambiguity
using GrpcEmpty = Ondock.Api.Grpc.V1.Empty;
using GrpcRegisterRequest = Ondock.Api.Grpc.V1.RegisterRequest;
using GrpcLoginRequest = Ondock.Api.Grpc.V1.LoginRequest;
using GrpcOAuthLoginRequest = Ondock.Api.Grpc.V1.OAuthLoginRequest;
using GrpcRefreshTokenRequest = Ondock.Api.Grpc.V1.RefreshTokenRequest;
using GrpcLogoutRequest = Ondock.Api.Grpc.V1.LogoutRequest;
using GrpcForgotPasswordRequest = Ondock.Api.Grpc.V1.ForgotPasswordRequest;
using GrpcResetPasswordRequest = Ondock.Api.Grpc.V1.ResetPasswordRequest;
using GrpcChangePasswordRequest = Ondock.Api.Grpc.V1.ChangePasswordRequest;
using GrpcConfirmEmailRequest = Ondock.Api.Grpc.V1.ConfirmEmailRequest;
using GrpcResendConfirmationEmailRequest = Ondock.Api.Grpc.V1.ResendConfirmationEmailRequest;
using GrpcSendConfirmationCodeRequest = Ondock.Api.Grpc.V1.SendConfirmationCodeRequest;
using GrpcVerifyConfirmationCodeRequest = Ondock.Api.Grpc.V1.VerifyConfirmationCodeRequest;
using GrpcAuthResponse = Ondock.Api.Grpc.V1.AuthResponse;
using GrpcConfirmationCodeResponse = Ondock.Api.Grpc.V1.ConfirmationCodeResponse;
using DtoRegisterRequest = ondock.api.DTOs.Auth.RegisterRequest;
using DtoLoginRequest = ondock.api.DTOs.Auth.LoginRequest;
using DtoOAuthLoginRequest = ondock.api.DTOs.Auth.OAuthLoginRequest;
using DtoRefreshTokenRequest = ondock.api.DTOs.Auth.RefreshTokenRequest;
using DtoResetPasswordRequest = ondock.api.DTOs.Auth.ResetPasswordRequest;
using DtoChangePasswordRequest = ondock.api.DTOs.Auth.ChangePasswordRequest;
using DtoConfirmEmailRequest = ondock.api.DTOs.Auth.ConfirmEmailRequest;
using DtoVerifyConfirmationCodeRequest = ondock.api.DTOs.Auth.VerifyConfirmationCodeRequest;
using DtoAuthResponse = ondock.api.DTOs.Auth.AuthResponse;
using Ondock.Api.Grpc.V1;

namespace ondock.api.Grpc.Services;

public class AuthGrpcService : AuthService.AuthServiceBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthGrpcService> _logger;

    public AuthGrpcService(IAuthService authService, ILogger<AuthGrpcService> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    public override async Task<RegisterResponse> Register(GrpcRegisterRequest request, ServerCallContext context)
    {
        var dto = new DtoRegisterRequest
        {
            Email = request.Email,
            Password = request.Password,
            FirstName = request.FirstName,
            LastName = request.LastName,
            PhoneNumber = request.PhoneNumber
        };

        var result = await _authService.RegisterAsync(dto);
        _logger.LogInformation("gRPC: User registered successfully: {Email}", request.Email);

        return new RegisterResponse
        {
            UserId = "", // Not returned by service
            Email = request.Email,
            RequiresEmailConfirmation = true, // Assume email confirmation required
            Message = result.Message ?? "Registration successful"
        };
    }

    public override async Task<GrpcAuthResponse> Login(GrpcLoginRequest request, ServerCallContext context)
    {
        var dto = new DtoLoginRequest
        {
            Email = request.Email,
            Password = request.Password,
            DeviceId = request.DeviceId
        };

        var result = await _authService.LoginAsync(dto);
        _logger.LogInformation("gRPC: User logged in: {Email}", request.Email);

        return MapAuthResponse(result);
    }

    public override async Task<GrpcAuthResponse> OAuthLogin(GrpcOAuthLoginRequest request, ServerCallContext context)
    {
        var dto = new DtoOAuthLoginRequest
        {
            IdToken = request.IdToken,
            Provider = request.Provider,
            DeviceId = request.DeviceId
        };

        DtoAuthResponse result;
        if (request.Provider.Equals("Google", StringComparison.OrdinalIgnoreCase))
        {
            result = await _authService.GoogleLoginAsync(dto);
        }
        else if (request.Provider.Equals("Apple", StringComparison.OrdinalIgnoreCase))
        {
            result = await _authService.AppleLoginAsync(dto);
        }
        else
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Unsupported OAuth provider"));
        }

        _logger.LogInformation("gRPC: User logged in with {Provider}", request.Provider);
        return MapAuthResponse(result);
    }

    public override async Task<GrpcAuthResponse> RefreshToken(GrpcRefreshTokenRequest request, ServerCallContext context)
    {
        var dto = new DtoRefreshTokenRequest
        {
            RefreshToken = request.RefreshToken
        };

        var result = await _authService.RefreshTokenAsync(dto);
        _logger.LogInformation("gRPC: Token refreshed");

        return MapAuthResponse(result);
    }

    [Authorize]
    public override async Task<GrpcEmpty> Logout(GrpcLogoutRequest request, ServerCallContext context)
    {
        var userId = GetUserId(context);
        var deviceId = string.IsNullOrEmpty(request.DeviceId) ? null : request.DeviceId;

        await _authService.LogoutAsync(userId, deviceId);
        _logger.LogInformation("gRPC: User logged out: {UserId}", userId);

        return new GrpcEmpty();
    }

    [Authorize]
    public override async Task<GrpcEmpty> RevokeAllSessions(GrpcEmpty request, ServerCallContext context)
    {
        var userId = GetUserId(context);
        await _authService.RevokeAllSessionsAsync(userId);
        _logger.LogInformation("gRPC: All sessions revoked for user: {UserId}", userId);

        return new GrpcEmpty();
    }

    public override async Task<SuccessResponse> ForgotPassword(GrpcForgotPasswordRequest request, ServerCallContext context)
    {
        await _authService.GeneratePasswordResetTokenAsync(request.Email);
        _logger.LogInformation("gRPC: Password reset requested for: {Email}", request.Email);

        return new SuccessResponse
        {
            Success = true,
            Message = "If an account exists with this email, a password reset link has been sent."
        };
    }

    public override async Task<SuccessResponse> ResetPassword(GrpcResetPasswordRequest request, ServerCallContext context)
    {
        var dto = new DtoResetPasswordRequest
        {
            Email = request.Email,
            Token = request.Token,
            NewPassword = request.NewPassword
        };

        await _authService.ResetPasswordAsync(dto);
        _logger.LogInformation("gRPC: Password reset for: {Email}", request.Email);

        return new SuccessResponse
        {
            Success = true,
            Message = "Password has been reset successfully."
        };
    }

    [Authorize]
    public override async Task<SuccessResponse> ChangePassword(GrpcChangePasswordRequest request, ServerCallContext context)
    {
        var userId = GetUserId(context);
        var dto = new DtoChangePasswordRequest
        {
            CurrentPassword = request.CurrentPassword,
            NewPassword = request.NewPassword
        };

        await _authService.ChangePasswordAsync(userId, dto);
        _logger.LogInformation("gRPC: Password changed for user: {UserId}", userId);

        return new SuccessResponse
        {
            Success = true,
            Message = "Password changed successfully."
        };
    }

    public override async Task<SuccessResponse> ConfirmEmail(GrpcConfirmEmailRequest request, ServerCallContext context)
    {
        var dto = new DtoConfirmEmailRequest
        {
            Email = request.Email,
            Token = request.Token
        };

        await _authService.ConfirmEmailAsync(dto);
        _logger.LogInformation("gRPC: Email confirmed for: {Email}", request.Email);

        return new SuccessResponse
        {
            Success = true,
            Message = "Email confirmed successfully."
        };
    }

    public override async Task<SuccessResponse> ResendConfirmationEmail(GrpcResendConfirmationEmailRequest request, ServerCallContext context)
    {
        await _authService.GenerateEmailConfirmationTokenAsync(request.Email);
        _logger.LogInformation("gRPC: Confirmation email resent for: {Email}", request.Email);

        return new SuccessResponse
        {
            Success = true,
            Message = "If an account exists with this email and is not yet confirmed, a confirmation link has been sent."
        };
    }

    public override async Task<GrpcConfirmationCodeResponse> SendConfirmationCode(GrpcSendConfirmationCodeRequest request, ServerCallContext context)
    {
        var result = await _authService.SendConfirmationCodeAsync(request.Email);
        _logger.LogInformation("gRPC: Confirmation code sent for: {Email}", request.Email);

        return new GrpcConfirmationCodeResponse
        {
            Success = result.Success,
            Message = result.Message,
            EmailConfirmed = false
        };
    }

    public override async Task<GrpcConfirmationCodeResponse> VerifyConfirmationCode(GrpcVerifyConfirmationCodeRequest request, ServerCallContext context)
    {
        var dto = new DtoVerifyConfirmationCodeRequest
        {
            Email = request.Email,
            Code = request.Code
        };

        var result = await _authService.VerifyConfirmationCodeAsync(dto);
        _logger.LogInformation("gRPC: Confirmation code verified for: {Email}", request.Email);

        return new GrpcConfirmationCodeResponse
        {
            Success = result.Success,
            Message = result.Message,
            EmailConfirmed = result.Success // Assume email is confirmed if verification succeeded
        };
    }

    [Authorize]
    public override async Task<UserResponse> GetCurrentUser(GrpcEmpty request, ServerCallContext context)
    {
        var userId = GetUserId(context);
        var user = await _authService.GetUserByIdAsync(userId);

        if (user == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "User not found"));
        }

        return new UserResponse
        {
            UserId = user.Id.ToString(),
            Email = user.Email ?? "",
            FirstName = user.FirstName ?? "",
            LastName = user.LastName ?? "",
            PhoneNumber = user.PhoneNumber ?? "",
            EmailConfirmed = user.EmailConfirmed,
            TwoFactorEnabled = user.TwoFactorEnabled,
            OauthProvider = user.OAuthProvider ?? ""
        };
    }

    [Authorize]
    public override async Task<SuccessResponse> DeleteAccount(GrpcEmpty request, ServerCallContext context)
    {
        var userId = GetUserId(context);
        await _authService.DeleteAccountAsync(userId);
        _logger.LogInformation("gRPC: Account deleted for user: {UserId}", userId);

        return new SuccessResponse
        {
            Success = true,
            Message = "Account deleted successfully."
        };
    }

    private static GrpcAuthResponse MapAuthResponse(DtoAuthResponse result)
    {
        return new GrpcAuthResponse
        {
            AccessToken = result.AccessToken,
            RefreshToken = result.RefreshToken,
            ExpiresAt = Timestamp.FromDateTime(result.ExpiresAt.ToUniversalTime()),
            UserId = result.User.UserId.ToString(),
            Email = result.User.Email ?? "",
            FirstName = result.User.FirstName ?? "",
            LastName = result.User.LastName ?? "",
            EmailConfirmed = result.User.EmailConfirmed
        };
    }

    private Guid GetUserId(ServerCallContext context)
    {
        var userIdClaim = context.GetHttpContext().User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new RpcException(new Status(StatusCode.Unauthenticated, "Invalid authentication token"));
        }
        return userId;
    }
}
