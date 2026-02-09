using System.Security.Cryptography;
using Google.Apis.Auth;
using Mapster;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ondock.api.Configuration;
using ondock.api.Data;
using ondock.api.Data.Entities;
using ondock.api.DTOs.Auth;
using ondock.api.Exceptions;
using ondock.api.Services.Interfaces;

namespace ondock.api.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly OnDockDbContext _context;
    private readonly OAuthSettings _oAuthSettings;
    private readonly JwtSettings _jwtSettings;
    private readonly EmailSettings _emailSettings;
    private readonly IAppleAuthService _appleAuthService;
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        IJwtTokenService jwtTokenService,
        OnDockDbContext context,
        IOptions<OAuthSettings> oAuthSettings,
        IOptions<JwtSettings> jwtSettings,
        IOptions<EmailSettings> emailSettings,
        IAppleAuthService appleAuthService,
        IEmailService emailService,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtTokenService = jwtTokenService;
        _context = context;
        _oAuthSettings = oAuthSettings.Value;
        _jwtSettings = jwtSettings.Value;
        _emailSettings = emailSettings.Value;
        _appleAuthService = appleAuthService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
    {
        // Check if user already exists
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            throw new ConflictException("An account with this email already exists");
        }

        // Create new user
        var user = new User
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            PhoneNumber = request.PhoneNumber,
            CreatedAt = DateTime.UtcNow,
            EmailConfirmed = false // Set to false, require email confirmation in production
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            // Convert Identity errors to validation errors
            var validationErrors = result.Errors
                .GroupBy(e => e.Code)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.Description).ToArray()
                );

            throw new BadRequestException("Registration failed due to validation errors", validationErrors);
        }

        // Send email confirmation based on configured method (link or code)
        if (_emailSettings.UseConfirmationCode)
        {
            await SendConfirmationCodeAsync(user.Email!);
        }
        else
        {
            await GenerateEmailConfirmationTokenAsync(user.Email!);
        }

        // Generate tokens and create session
        // Note: User will need to confirm email to access all features
        return new RegisterResponse
        {
            Message = _emailSettings.UseConfirmationCode
                ? "Registration successful. Please check your email for a confirmation code."
                : "Registration successful. Please check your email to confirm your email address."
        };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        // Find user by email
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            // Use generic message to prevent user enumeration
            throw new UnauthorizedException("Invalid credentials");
        }

        // Check password
        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            throw new UnauthorizedException("Your account has been temporarily locked due to multiple failed login attempts. Please try again later.");
        }

        if (!result.Succeeded)
        {
            // Use generic message to prevent user enumeration
            throw new UnauthorizedException("Invalid credentials");
        }

        // Check if email is confirmed (only for non-OAuth users)
        if (!user.EmailConfirmed && string.IsNullOrEmpty(user.OAuthProvider))
        {
            throw new UnauthorizedException("Please confirm your email address before logging in. Check your inbox for the confirmation email.");
        }

        // Update last login
        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        // Generate tokens and create session
        return await GenerateAuthResponseAsync(user, request.DeviceId);
    }

    public async Task<AuthResponse> GoogleLoginAsync(OAuthLoginRequest request)
    {
        try
        {
            // Log configured client IDs for debugging
            var clientIds = _oAuthSettings.Google.GetAllClientIds().ToList();
            _logger.LogInformation("Validating Google token with {Count} client IDs: {ClientIds}",
                clientIds.Count, string.Join(", ", clientIds));

            // Validate Google ID token with all configured client IDs (Web, iOS, Android)
            var payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = _oAuthSettings.Google.GetAllClientIds()
            });

            // Log payload info for debugging
            _logger.LogInformation("Google token validated. Email: {Email}, Subject: {Subject}, Audience: {Audience}",
                payload.Email ?? "NULL", payload.Subject, payload.Audience);

            // Use email from token if available, otherwise use email from request
            var email = payload.Email;
            if (string.IsNullOrEmpty(email))
            {
                _logger.LogWarning("Google token payload has no email, using email from request. Subject: {Subject}", payload.Subject);
                email = request.Email;
            }

            // Final validation: email must be available from either token or request
            if (string.IsNullOrEmpty(email))
            {
                _logger.LogError("No email available from token or request. Subject: {Subject}, Audience: {Audience}",
                    payload.Subject, payload.Audience);
                throw new UnauthorizedException("Google account email not available. Please ensure email permission is granted.");
            }

            _logger.LogInformation("Using email: {Email} for Google OAuth user", email);

            // Find or create user
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                // Create new user with Google OAuth
                user = new User
                {
                    UserName = email,
                    Email = email,
                    FirstName = payload.GivenName,
                    LastName = payload.FamilyName,
                    EmailConfirmed = payload.EmailVerified,
                    OAuthProvider = "Google",
                    OAuthId = payload.Subject,
                    CreatedAt = DateTime.UtcNow
                };

                var createResult = await _userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    var validationErrors = createResult.Errors
                        .GroupBy(e => e.Code)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Select(e => e.Description).ToArray()
                        );

                    throw new BadRequestException("Failed to create user account", validationErrors);
                }
            }
            else
            {
                // Update OAuth info if not set
                if (string.IsNullOrEmpty(user.OAuthProvider))
                {
                    user.OAuthProvider = "Google";
                    user.OAuthId = payload.Subject;

                    // Update name if not set and available from Google
                    if (string.IsNullOrEmpty(user.FirstName) && !string.IsNullOrEmpty(payload.GivenName))
                    {
                        user.FirstName = payload.GivenName;
                    }
                    if (string.IsNullOrEmpty(user.LastName) && !string.IsNullOrEmpty(payload.FamilyName))
                    {
                        user.LastName = payload.FamilyName;
                    }

                    await _userManager.UpdateAsync(user);
                }
            }

            // Update last login
            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            // Generate tokens and create session
            return await GenerateAuthResponseAsync(user, request.DeviceId);
        }
        catch (InvalidJwtException ex)
        {
            _logger.LogError(ex, "Invalid JWT from Google - Reason: {Reason}, Message: {Message}", ex.GetType().Name, ex.Message);
            throw new UnauthorizedException("Invalid or expired Google authentication token");
        }
        catch (Exception ex) when (ex is not UnauthorizedException && ex is not BadRequestException)
        {
            // Log the actual error but throw generic message to prevent information leakage
            _logger.LogError(ex, "Google authentication failed - Type: {ExceptionType}, Message: {Message}, StackTrace: {StackTrace}",
                ex.GetType().FullName, ex.Message, ex.StackTrace);
            throw new UnauthorizedException("Google authentication failed. Please try again.");
        }
    }

    public async Task<AuthResponse> AppleLoginAsync(OAuthLoginRequest request)
    {
        try
        {
            // Validate Apple ID token
            var appleUser = await _appleAuthService.ValidateTokenAsync(request.IdToken);

            // Apple sometimes doesn't return email on subsequent logins
            // Try to find user by OAuth ID first
            User? user = null;

            if (!string.IsNullOrEmpty(appleUser.Subject))
            {
                user = await _context.Users
                    .FirstOrDefaultAsync(u => u.OAuthProvider == "Apple" && u.OAuthId == appleUser.Subject);
            }

            // If not found by OAuth ID and email is provided, try email
            if (user == null && !string.IsNullOrEmpty(appleUser.Email))
            {
                user = await _userManager.FindByEmailAsync(appleUser.Email);
            }

            if (user == null)
            {
                // Create new user with Apple OAuth
                // Apple may not always provide email, generate a placeholder if needed
                var email = !string.IsNullOrEmpty(appleUser.Email)
                    ? appleUser.Email
                    : $"{appleUser.Subject}@appleid.privaterelay.com";

                user = new User
                {
                    UserName = email,
                    Email = email,
                    FirstName = appleUser.FirstName,
                    LastName = appleUser.LastName,
                    EmailConfirmed = appleUser.EmailVerified,
                    OAuthProvider = "Apple",
                    OAuthId = appleUser.Subject,
                    CreatedAt = DateTime.UtcNow
                };

                var createResult = await _userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    var validationErrors = createResult.Errors
                        .GroupBy(e => e.Code)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Select(e => e.Description).ToArray()
                        );

                    throw new BadRequestException("Failed to create user account", validationErrors);
                }
            }
            else
            {
                // Update OAuth info if not set
                if (string.IsNullOrEmpty(user.OAuthProvider) || user.OAuthProvider != "Apple")
                {
                    user.OAuthProvider = "Apple";
                    user.OAuthId = appleUser.Subject;

                    // Update email if provided and user doesn't have one
                    if (!string.IsNullOrEmpty(appleUser.Email) &&
                        (string.IsNullOrEmpty(user.Email) || user.Email.Contains("@appleid.privaterelay.com")))
                    {
                        user.Email = appleUser.Email;
                        user.UserName = appleUser.Email;
                        user.EmailConfirmed = appleUser.EmailVerified;
                    }

                    // Update name if not set and available from Apple
                    if (string.IsNullOrEmpty(user.FirstName) && !string.IsNullOrEmpty(appleUser.FirstName))
                    {
                        user.FirstName = appleUser.FirstName;
                    }
                    if (string.IsNullOrEmpty(user.LastName) && !string.IsNullOrEmpty(appleUser.LastName))
                    {
                        user.LastName = appleUser.LastName;
                    }

                    await _userManager.UpdateAsync(user);
                }
            }

            // Update last login
            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            // Generate tokens and create session
            return await GenerateAuthResponseAsync(user, request.DeviceId);
        }
        catch (Exception ex) when (ex is not UnauthorizedException && ex is not BadRequestException)
        {
            _logger.LogError(ex, "Apple authentication failed for token validation");
            throw new UnauthorizedException("Apple authentication failed. Please try again.");
        }
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request)
    {
        // Find session by refresh token
        var session = await _context.UserSessions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.RefreshToken == request.RefreshToken);

        if (session == null)
        {
            _logger.LogWarning("Refresh token not found in database");
            throw new UnauthorizedException("Invalid or expired session. Please log in again.");
        }

        if (session.ExpiresAt < DateTime.UtcNow)
        {
            _logger.LogWarning("Refresh token expired. SessionId: {SessionId}, ExpiresAt: {ExpiresAt}, CurrentTime: {CurrentTime}",
                session.SessionId, session.ExpiresAt, DateTime.UtcNow);

            // Clean up expired session
            _context.UserSessions.Remove(session);
            await _context.SaveChangesAsync();

            throw new UnauthorizedException("Invalid or expired session. Please log in again.");
        }

        var user = session.User;

        // Generate new access token
        var accessToken = _jwtTokenService.GenerateAccessToken(user);
        var newRefreshToken = _jwtTokenService.GenerateRefreshToken();

        // Update session with new refresh token
        session.RefreshToken = newRefreshToken;
        session.ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);
        session.LastUsedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Token refreshed successfully for user {UserId}, SessionId: {SessionId}",
            user.Id, session.SessionId);

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = _jwtTokenService.GetTokenExpiration(),
            User = MapToUserInfo(user)
        };
    }

    public async Task LogoutAsync(Guid userId, string? deviceId = null)
    {
        if (!string.IsNullOrEmpty(deviceId))
        {
            // Logout specific device
            var session = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.UserId == userId && s.DeviceId == deviceId);

            if (session != null)
            {
                _context.UserSessions.Remove(session);
                await _context.SaveChangesAsync();
            }
        }
        else
        {
            // Logout all devices
            await RevokeAllSessionsAsync(userId);
        }
    }

    public async Task<bool> RevokeAllSessionsAsync(Guid userId)
    {
        var sessions = await _context.UserSessions
            .Where(s => s.UserId == userId)
            .ToListAsync();

        if (sessions.Any())
        {
            _context.UserSessions.RemoveRange(sessions);
            await _context.SaveChangesAsync();
            return true;
        }

        return false;
    }

    // Private helper methods
    private async Task<AuthResponse> GenerateAuthResponseAsync(User user, string? deviceId)
    {
        // Generate tokens
        var accessToken = _jwtTokenService.GenerateAccessToken(user);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();

        // Create session
        var session = new UserSession
        {
            SessionId = Guid.NewGuid(),
            UserId = user.Id,
            RefreshToken = refreshToken,
            DeviceId = deviceId ?? "unknown",
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
            CreatedAt = DateTime.UtcNow,
            LastUsedAt = DateTime.UtcNow
        };

        _context.UserSessions.Add(session);
        await _context.SaveChangesAsync();

        // Prepare message for unconfirmed emails
        string? message = null;
        if (!user.EmailConfirmed && string.IsNullOrEmpty(user.OAuthProvider))
        {
            message = "Please check your email and confirm your email address to access all features. If you didn't receive the email, you can request a new confirmation link.";
        }

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = _jwtTokenService.GetTokenExpiration(),
            User = MapToUserInfo(user),
            Message = message
        };
    }

    public async Task<string> GeneratePasswordResetTokenAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);

        // Don't reveal if user exists or not (security best practice)
        if (user == null)
        {
            // Return a generic success message to prevent user enumeration
            // Don't send any email if user doesn't exist
            _logger.LogWarning("Password reset requested for non-existent email: {Email}", email);
            return "success"; // Generic success response
        }

        // Don't allow password reset for OAuth users
        if (!string.IsNullOrEmpty(user.OAuthProvider))
        {
            throw new BadRequestException($"This account uses {user.OAuthProvider} sign-in. Please use {user.OAuthProvider} to sign in.");
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);

        // Send password reset email in background using Hangfire
        try
        {
            Hangfire.BackgroundJob.Enqueue(() =>
                _emailService.SendPasswordResetEmailAsync(user.Email!, token, user.UserName ?? user.Email!));

            _logger.LogInformation("Password reset email queued for {Email}", email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to queue password reset email for {Email}", email);
            // Don't throw - still return success to user
        }

        // Don't return the actual token - user will get it via email
        return "success";
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            throw new NotFoundException("Invalid password reset request");
        }

        // Don't allow password reset for OAuth users
        if (!string.IsNullOrEmpty(user.OAuthProvider))
        {
            throw new BadRequestException($"This account uses {user.OAuthProvider} sign-in. Password reset is not available.");
        }

        var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);

        if (!result.Succeeded)
        {
            var validationErrors = result.Errors
                .GroupBy(e => e.Code)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.Description).ToArray()
                );

            throw new BadRequestException("Password reset failed due to validation errors", validationErrors);
        }

        // Revoke all sessions for security
        await RevokeAllSessionsAsync(user.Id);
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            throw new NotFoundException("User not found");
        }

        // Don't allow password change for OAuth users
        if (!string.IsNullOrEmpty(user.OAuthProvider))
        {
            throw new BadRequestException($"This account uses {user.OAuthProvider} sign-in. Password change is not available.");
        }

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);

        if (!result.Succeeded)
        {
            var validationErrors = result.Errors
                .GroupBy(e => e.Code)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.Description).ToArray()
                );

            throw new BadRequestException("Password change failed due to validation errors", validationErrors);
        }

        // Revoke all other sessions for security (keep current session active)
        // This would require passing the current session ID to exclude it
        // For now, revoke all sessions - user will need to login again
        await RevokeAllSessionsAsync(user.Id);
    }

    public async Task<string> GenerateEmailConfirmationTokenAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);

        // Don't reveal if user exists or not (security best practice)
        if (user == null)
        {
            // Return a generic success message to prevent user enumeration
            _logger.LogWarning("Email confirmation requested for non-existent email: {Email}", email);
            return "success";
        }

        // Check if email is already confirmed
        if (user.EmailConfirmed)
        {
            _logger.LogInformation("Email confirmation requested for already confirmed email: {Email}", email);
            return "success"; // Don't send email if already confirmed
        }

        // Don't allow email confirmation for OAuth users (they're auto-confirmed)
        if (!string.IsNullOrEmpty(user.OAuthProvider))
        {
            _logger.LogInformation("Email confirmation requested for OAuth user: {Email}", email);
            return "success";
        }

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

        // Send email confirmation email in background using Hangfire
        try
        {
            Hangfire.BackgroundJob.Enqueue(() =>
                _emailService.SendEmailConfirmationAsync(user.Email!, token, user.UserName ?? user.Email!));

            _logger.LogInformation("Email confirmation email queued for {Email}", email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to queue email confirmation email for {Email}", email);
            // Don't throw - still return success to user
        }

        // Don't return the actual token - user will get it via email
        return "success";
    }

    public async Task ConfirmEmailAsync(ConfirmEmailRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            throw new NotFoundException("Invalid email confirmation request");
        }

        // Don't allow email confirmation for OAuth users
        if (!string.IsNullOrEmpty(user.OAuthProvider))
        {
            throw new BadRequestException($"This account uses {user.OAuthProvider} sign-in. Email confirmation is not required.");
        }

        // Check if already confirmed
        if (user.EmailConfirmed)
        {
            _logger.LogInformation("Email already confirmed for: {Email}", request.Email);
            return; // Already confirmed, no need to throw error
        }

        var result = await _userManager.ConfirmEmailAsync(user, request.Token);

        if (!result.Succeeded)
        {
            var validationErrors = result.Errors
                .GroupBy(e => e.Code)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.Description).ToArray()
                );

            throw new BadRequestException("Email confirmation failed. The token may be invalid or expired.", validationErrors);
        }

        _logger.LogInformation("Email confirmed successfully for: {Email}", request.Email);
    }

    public async Task<User?> GetUserByIdAsync(Guid userId)
    {
        return await _userManager.FindByIdAsync(userId.ToString());
    }

    public async Task DeleteAccountAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            throw new NotFoundException("User not found");
        }

        // First, revoke all active sessions
        await RevokeAllSessionsAsync(userId);

        // Delete the user account
        var result = await _userManager.DeleteAsync(user);

        if (!result.Succeeded)
        {
            var validationErrors = result.Errors
                .GroupBy(e => e.Code)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.Description).ToArray()
                );

            throw new BadRequestException("Failed to delete account", validationErrors);
        }

        _logger.LogInformation("User account deleted: {UserId}, Email: {Email}", userId, user.Email);
    }

    public string GetConfirmationMethod()
    {
        return _emailSettings.UseConfirmationCode ? "code" : "link";
    }

    public async Task<ConfirmationCodeResponse> SendConfirmationCodeAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);

        // Don't reveal if user exists or not (security best practice)
        if (user == null)
        {
            _logger.LogWarning("Confirmation code requested for non-existent email: {Email}", email);
            return new ConfirmationCodeResponse
            {
                Success = true,
                Message = "If an account exists with this email and is not yet confirmed, a confirmation code has been sent.",
                Method = "code",
                ExpiresInMinutes = _emailSettings.ConfirmationCodeExpirationMinutes
            };
        }

        // Check if email is already confirmed
        if (user.EmailConfirmed)
        {
            _logger.LogInformation("Confirmation code requested for already confirmed email: {Email}", email);
            return new ConfirmationCodeResponse
            {
                Success = true,
                Message = "Email is already confirmed.",
                Method = "code"
            };
        }

        // Don't allow for OAuth users
        if (!string.IsNullOrEmpty(user.OAuthProvider))
        {
            _logger.LogInformation("Confirmation code requested for OAuth user: {Email}", email);
            return new ConfirmationCodeResponse
            {
                Success = true,
                Message = "If an account exists with this email and is not yet confirmed, a confirmation code has been sent.",
                Method = "code"
            };
        }

        // Check if a valid (non-expired, non-used) code already exists
        var validCode = await _context.EmailConfirmationCodes
            .Where(c => c.UserId == user.Id && !c.IsUsed && c.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync();

        if (validCode != null)
        {
            // A valid code already exists - check cooldown
            var cooldownMinutes = 1;
            var timeSinceLastCode = DateTime.UtcNow - validCode.CreatedAt;
            if (timeSinceLastCode.TotalMinutes < cooldownMinutes)
            {
                var remainingSeconds = (int)(cooldownMinutes * 60 - timeSinceLastCode.TotalSeconds);
                _logger.LogInformation("Confirmation code cooldown active for {Email}. {Seconds}s remaining", email, remainingSeconds);
                throw new BadRequestException($"Please wait {remainingSeconds} seconds before requesting another confirmation code.");
            }
        }

        // Count total codes sent (used or not) - block after 3 codes until admin unblocks
        var totalCodesSent = await _context.EmailConfirmationCodes
            .CountAsync(c => c.UserId == user.Id);

        if (totalCodesSent >= 3)
        {
            _logger.LogWarning("User {Email} blocked from sending confirmation codes - exceeded 3 codes total", email);
            throw new BadRequestException("Too many confirmation code requests. Please contact support to unblock your account.");
        }

        // Invalidate any existing valid code (only one valid code per account)
        if (validCode != null)
        {
            validCode.IsUsed = true;
            validCode.UsedAt = DateTime.UtcNow;
        }

        // Generate new confirmation code
        var code = GenerateConfirmationCode(_emailSettings.ConfirmationCodeLength);
        var expiresAt = DateTime.UtcNow.AddMinutes(_emailSettings.ConfirmationCodeExpirationMinutes);

        var confirmationCode = new EmailConfirmationCode
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Code = code,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt,
            IsUsed = false,
            AttemptCount = 0
        };

        _context.EmailConfirmationCodes.Add(confirmationCode);
        await _context.SaveChangesAsync();

        // Send confirmation code email
        try
        {
            Hangfire.BackgroundJob.Enqueue(() =>
                _emailService.SendEmailConfirmationCodeAsync(
                    user.Email!,
                    code,
                    user.UserName ?? user.Email!,
                    _emailSettings.ConfirmationCodeExpirationMinutes));

            _logger.LogInformation("Confirmation code email queued for {Email}", email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to queue confirmation code email for {Email}", email);
        }

        return new ConfirmationCodeResponse
        {
            Success = true,
            Message = "Confirmation code sent to your email address.",
            Method = "code",
            ExpiresInMinutes = _emailSettings.ConfirmationCodeExpirationMinutes
        };
    }

    public async Task<ConfirmationCodeResponse> VerifyConfirmationCodeAsync(VerifyConfirmationCodeRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            throw new NotFoundException("Invalid confirmation request");
        }

        // Don't allow for OAuth users
        if (!string.IsNullOrEmpty(user.OAuthProvider))
        {
            throw new BadRequestException($"This account uses {user.OAuthProvider} sign-in. Email confirmation is not required.");
        }

        // Check if already confirmed
        if (user.EmailConfirmed)
        {
            return new ConfirmationCodeResponse
            {
                Success = true,
                Message = "Email is already confirmed.",
                Method = "code"
            };
        }

        // Find the most recent valid confirmation code for this user
        var confirmationCode = await _context.EmailConfirmationCodes
            .Where(c => c.UserId == user.Id && !c.IsUsed)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync();

        if (confirmationCode == null)
        {
            throw new BadRequestException("No confirmation code found. Please request a new code.");
        }

        // Check if code is expired
        if (confirmationCode.ExpiresAt < DateTime.UtcNow)
        {
            confirmationCode.IsUsed = true;
            confirmationCode.UsedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            throw new BadRequestException("Confirmation code has expired. Please request a new code.");
        }

        // Increment attempt count
        confirmationCode.AttemptCount++;

        // Check for too many attempts (max 5)
        if (confirmationCode.AttemptCount > 5)
        {
            confirmationCode.IsUsed = true;
            confirmationCode.UsedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            throw new BadRequestException("Too many failed attempts. Please request a new code.");
        }

        // Verify the code (case-insensitive comparison)
        if (!string.Equals(confirmationCode.Code, request.Code.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            await _context.SaveChangesAsync();
            throw new BadRequestException($"Invalid confirmation code. {5 - confirmationCode.AttemptCount} attempts remaining.");
        }

        // Mark code as used
        confirmationCode.IsUsed = true;
        confirmationCode.UsedAt = DateTime.UtcNow;

        // Confirm the email
        user.EmailConfirmed = true;
        await _userManager.UpdateAsync(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Email confirmed successfully via code for: {Email}", request.Email);

        return new ConfirmationCodeResponse
        {
            Success = true,
            Message = "Email confirmed successfully! You can now login to access your account.",
            Method = "code"
        };
    }

    private static string GenerateConfirmationCode(int length)
    {
        const string digits = "0123456789";
        var code = new char[length];

        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[length];
        rng.GetBytes(bytes);

        for (int i = 0; i < length; i++)
        {
            code[i] = digits[bytes[i] % digits.Length];
        }

        return new string(code);
    }

    private static UserInfo MapToUserInfo(User user)
    {
        // Use Mapster for mapping
        return user.Adapt<UserInfo>();
    }
}
