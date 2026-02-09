using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ondock.api.DTOs.Auth;
using ondock.api.DTOs.Common;
using ondock.api.Services.Interfaces;
using System.Security.Claims;

namespace ondock.api.Controllers;

[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
[Obsolete("Use gRPC AuthService instead. This REST controller is deprecated.")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> _logger)
    {
        _authService = authService;
        this._logger = _logger;
    }

    /// <summary>
    /// Register a new user with email and password
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RegisterResponse>> Register([FromBody] RegisterRequest request)
    {
        var response = await _authService.RegisterAsync(request);
        _logger.LogInformation("User registered successfully: {Email}", request.Email);

        return CreatedAtAction(
            nameof(GetCurrentUser),
            new { },
            response
        );
    }

    /// <summary>
    /// Login with email and password
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        var response = await _authService.LoginAsync(request);
        _logger.LogInformation("User logged in successfully: {Email}", request.Email);
        return Ok(response);
    }

    /// <summary>
    /// Login with Google OAuth
    /// </summary>
    [HttpPost("oauth/google")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> GoogleLogin([FromBody] OAuthLoginRequest request)
    {
        var response = await _authService.GoogleLoginAsync(request);
        _logger.LogInformation("User logged in with Google successfully");
        return Ok(response);
    }

    /// <summary>
    /// Login with Apple Sign-In
    /// </summary>
    [HttpPost("oauth/apple")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> AppleLogin([FromBody] OAuthLoginRequest request)
    {
        var response = await _authService.AppleLoginAsync(request);
        _logger.LogInformation("User logged in with Apple successfully");
        return Ok(response);
    }

    /// <summary>
    /// Apple Sign-In callback endpoint (for server-side flow)
    /// </summary>
    [HttpPost("apple/callback")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> AppleCallback([FromForm] AppleCallbackRequest request)
    {
        // This endpoint handles the server-side callback from Apple
        // Apple sends the authorization code and id_token as form data

        if (string.IsNullOrEmpty(request.IdToken))
        {
            _logger.LogWarning("Apple callback received without id_token");
            throw new ondock.api.Exceptions.BadRequestException("Missing id_token in callback");
        }

        var oauthRequest = new OAuthLoginRequest
        {
            IdToken = request.IdToken,
            Provider = "Apple",
            DeviceId = "web"
        };

        var response = await _authService.AppleLoginAsync(oauthRequest);
        _logger.LogInformation("User logged in via Apple callback successfully");

        // For web flow, you might want to redirect to a success page with the tokens
        // For now, return JSON response
        return Ok(response);
    }

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var response = await _authService.RefreshTokenAsync(request);
        _logger.LogInformation("Token refreshed successfully");
        return Ok(response);
    }

    /// <summary>
    /// Logout from current device or all devices
    /// </summary>
    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout([FromQuery] string? deviceId = null)
    {
        var userId = GetUserId();

        await _authService.LogoutAsync(userId, deviceId);

        _logger.LogInformation("User logged out: {UserId}, DeviceId: {DeviceId}", userId, deviceId ?? "all");
        return NoContent();
    }

    /// <summary>
    /// Revoke all active sessions for the current user
    /// </summary>
    [Authorize]
    [HttpPost("revoke-all-sessions")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RevokeAllSessions()
    {
        var userId = GetUserId();

        await _authService.RevokeAllSessionsAsync(userId);

        _logger.LogInformation("All sessions revoked for user: {UserId}", userId);
        return NoContent();
    }

    /// <summary>
    /// Request password reset token - sends email with reset link
    /// </summary>
    [HttpPost("forgot-password")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        await _authService.GeneratePasswordResetTokenAsync(request.Email);

        _logger.LogInformation("Password reset requested for: {Email}", request.Email);

        // Always return the same message to prevent user enumeration
        return Ok(new
        {
            message = "If an account exists with this email, a password reset link has been sent. Please check your inbox and spam folder."
        });
    }

    /// <summary>
    /// Reset password using reset token
    /// </summary>
    [HttpPost("reset-password")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        await _authService.ResetPasswordAsync(request);

        _logger.LogInformation("Password reset successfully for: {Email}", request.Email);

        return Ok(new
        {
            message = "Password has been reset successfully. Please login with your new password."
        });
    }

    /// <summary>
    /// Change password for authenticated user
    /// </summary>
    [Authorize]
    [HttpPost("change-password")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = GetUserId();

        await _authService.ChangePasswordAsync(userId, request);

        _logger.LogInformation("Password changed successfully for user: {UserId}", userId);

        return Ok(new
        {
            message = "Password changed successfully. All sessions have been revoked. Please login again."
        });
    }

    /// <summary>
    /// Confirm email address using confirmation token sent to email
    /// </summary>
    [HttpPost("confirm-email")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequest request)
    {
        await _authService.ConfirmEmailAsync(request);

        _logger.LogInformation("Email confirmed successfully for: {Email}", request.Email);

        return Ok(new
        {
            message = "Email confirmed successfully! You can now login to access your account.",
            email = request.Email
        });
    }

    /// <summary>
    /// Resend email confirmation link - sends email with confirmation token
    /// </summary>
    [HttpPost("resend-confirmation-email")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResendConfirmationEmail([FromBody] ResendConfirmationEmailRequest request)
    {
        await _authService.GenerateEmailConfirmationTokenAsync(request.Email);

        _logger.LogInformation("Email confirmation requested for: {Email}", request.Email);

        // Always return the same message to prevent user enumeration
        return Ok(new
        {
            message = "If an account exists with this email and is not yet confirmed, a confirmation link has been sent. Please check your inbox and spam folder."
        });
    }

    /// <summary>
    /// Send email confirmation code (code-based confirmation)
    /// </summary>
    [HttpPost("send-confirmation-code")]
    [ProducesResponseType(typeof(ConfirmationCodeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ConfirmationCodeResponse>> SendConfirmationCode([FromBody] SendConfirmationCodeRequest request)
    {
        var response = await _authService.SendConfirmationCodeAsync(request.Email);

        _logger.LogInformation("Confirmation code requested for: {Email}", request.Email);

        return Ok(response);
    }

    /// <summary>
    /// Verify email confirmation code (code-based confirmation)
    /// </summary>
    [HttpPost("verify-confirmation-code")]
    [ProducesResponseType(typeof(ConfirmationCodeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ConfirmationCodeResponse>> VerifyConfirmationCode([FromBody] VerifyConfirmationCodeRequest request)
    {
        var response = await _authService.VerifyConfirmationCodeAsync(request);

        _logger.LogInformation("Confirmation code verified for: {Email}", request.Email);

        return Ok(response);
    }

    /// <summary>
    /// Get the current email confirmation method (link or code)
    /// </summary>
    [HttpGet("confirmation-method")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public IActionResult GetConfirmationMethod()
    {
        var method = _authService.GetConfirmationMethod();
        return Ok(new { method });
    }

    /// <summary>
    /// Get current authenticated user information
    /// </summary>
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetCurrentUser()
    {
        var userId = GetUserId();
        var user = await _authService.GetUserByIdAsync(userId);

        if (user == null)
        {
            throw new ondock.api.Exceptions.NotFoundException("User not found");
        }

        return Ok(new
        {
            userId = user.Id,
            email = user.Email,
            firstName = user.FirstName,
            lastName = user.LastName,
            phoneNumber = user.PhoneNumber,
            emailConfirmed = user.EmailConfirmed,
            twoFactorEnabled = user.TwoFactorEnabled,
            oauthProvider = user.OAuthProvider,
            claims = User.Claims.Select(c => new { c.Type, c.Value })
        });
    }

    /// <summary>
    /// Delete current user account (requires confirmation)
    /// </summary>
    [Authorize]
    [HttpDelete("delete-account")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAccount()
    {
        var userId = GetUserId();

        await _authService.DeleteAccountAsync(userId);

        _logger.LogInformation("User account deleted successfully: {UserId}", userId);

        return Ok(new
        {
            message = "Your account has been permanently deleted. We're sorry to see you go."
        });
    }

    // Helper method to get user ID from claims
    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new ondock.api.Exceptions.UnauthorizedException("Invalid authentication token");
        }
        return userId;
    }
}
