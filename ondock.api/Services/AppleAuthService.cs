using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ondock.api.Configuration;
using ondock.api.Exceptions;

namespace ondock.api.Services;

public interface IAppleAuthService
{
    Task<AppleUserInfo> ValidateTokenAsync(string identityToken);
}

public class AppleAuthService : IAppleAuthService
{
    private readonly AppleOAuthSettings _settings;
    private readonly HttpClient _httpClient;
    private readonly ILogger<AppleAuthService> _logger;

    public AppleAuthService(
        IOptions<OAuthSettings> settings,
        HttpClient httpClient,
        ILogger<AppleAuthService> logger)
    {
        _settings = settings.Value.Apple;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<AppleUserInfo> ValidateTokenAsync(string identityToken)
    {
        try
        {
            // Get Apple's public keys
            var appleKeys = await GetApplePublicKeysAsync();
            
            // Decode the JWT header to get the key ID
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(identityToken);
            var kid = jwtToken.Header.Kid;

            if (string.IsNullOrEmpty(kid))
            {
                throw new UnauthorizedException("Invalid Apple token - missing key ID");
            }

            // Find the matching public key
            var appleKey = appleKeys.Keys.FirstOrDefault(k => k.Kid == kid);
            if (appleKey == null)
            {
                _logger.LogWarning("Apple public key not found for kid: {Kid}", kid);
                throw new UnauthorizedException("Invalid Apple token - key not found");
            }

            // Validate the token with all configured audiences (Service ID + Bundle IDs)
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = "https://appleid.apple.com",
                ValidateAudience = true,
                ValidAudiences = _settings.GetAllClientIds(),
                ValidateLifetime = true,
                IssuerSigningKey = BuildRSAKey(appleKey),
                ClockSkew = TimeSpan.FromMinutes(5)
            };

            var principal = handler.ValidateToken(identityToken, validationParameters, out var validatedToken);
            var validatedJwtToken = (JwtSecurityToken)validatedToken;

            // Extract user information from JWT claims
            // Apple uses standard JWT claim names
            var subject = validatedJwtToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value 
                ?? validatedJwtToken.Subject 
                ?? principal.FindFirst("sub")?.Value
                ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                
            var email = validatedJwtToken.Claims.FirstOrDefault(c => c.Type == "email")?.Value
                ?? principal.FindFirst("email")?.Value
                ?? principal.FindFirst(ClaimTypes.Email)?.Value;
                
            var emailVerified = validatedJwtToken.Claims.FirstOrDefault(c => c.Type == "email_verified")?.Value;

            _logger.LogInformation("Apple token validated. Subject: {Subject}, Email: {Email}", subject, email);

            if (string.IsNullOrEmpty(subject))
            {
                _logger.LogError("Apple token missing subject. Available claims: {Claims}", 
                    string.Join(", ", validatedJwtToken.Claims.Select(c => $"{c.Type}={c.Value}")));
                throw new UnauthorizedException("Invalid Apple token - missing subject");
            }

            return new AppleUserInfo
            {
                Subject = subject,
                Email = email ?? string.Empty,
                EmailVerified = emailVerified == "true" || string.IsNullOrEmpty(emailVerified)
            };
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning(ex, "Apple token validation failed");
            throw new UnauthorizedException("Invalid or expired Apple token");
        }
        catch (Exception ex) when (ex is not UnauthorizedException)
        {
            _logger.LogError(ex, "Failed to validate Apple token");
            throw new UnauthorizedException("Invalid Apple token");
        }
    }

    private async Task<ApplePublicKeys> GetApplePublicKeysAsync()
    {
        try
        {
            var response = await _httpClient.GetStringAsync("https://appleid.apple.com/auth/keys");
            var keys = JsonSerializer.Deserialize<ApplePublicKeys>(response, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (keys == null || !keys.Keys.Any())
            {
                throw new UnauthorizedException("Failed to retrieve Apple public keys");
            }

            return keys;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to fetch Apple public keys");
            throw new UnauthorizedException("Failed to validate Apple credentials");
        }
    }

    private static RsaSecurityKey BuildRSAKey(ApplePublicKey key)
    {
        var rsa = RSA.Create();
        rsa.ImportParameters(new RSAParameters
        {
            Modulus = Base64UrlDecode(key.N),
            Exponent = Base64UrlDecode(key.E)
        });
        return new RsaSecurityKey(rsa);
    }

    private static byte[] Base64UrlDecode(string input)
    {
        var base64 = input.Replace('-', '+').Replace('_', '/');
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        return Convert.FromBase64String(base64);
    }
}

public class ApplePublicKeys
{
    public List<ApplePublicKey> Keys { get; set; } = new();
}

public class ApplePublicKey
{
    public string Kty { get; set; } = string.Empty;
    public string Kid { get; set; } = string.Empty;
    public string Use { get; set; } = string.Empty;
    public string Alg { get; set; } = string.Empty;
    public string N { get; set; } = string.Empty;
    public string E { get; set; } = string.Empty;
}

public class AppleUserInfo
{
    public string Subject { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool EmailVerified { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}
