using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using ondock.api.Data;
using ondock.api.Data.Entities;
using ondock.api.DTOs.Contact;
using ondock.api.Exceptions;
using ondock.api.Services.Interfaces;

namespace ondock.api.Services;

public class ContactService : IContactService
{
    private readonly OnDockDbContext _db;
    private readonly IChatSettingsProvider _settingsProvider;
    private static readonly Dictionary<string, (Guid UserId, string? ChatId, DateTime ExpiresAt)> _pendingTokens = new();
    private static readonly object _tokenLock = new();

    public ContactService(OnDockDbContext db, IChatSettingsProvider settingsProvider)
    {
        _db = db;
        _settingsProvider = settingsProvider;
    }

    public async Task<GenerateQRCodeResponse> GenerateQRCodeAsync(Guid userId, Guid? chatIdGuid)
    {
        var chatId = chatIdGuid?.ToString();
        // If chatId is provided, verify user is a participant in this chat
        if (!string.IsNullOrEmpty(chatId))
        {
            var isParticipant = await _db.ChatParticipants
                .AnyAsync(p => p.ChatId == chatId && p.UserId == userId);

            if (!isParticipant)
            {
                throw new ForbiddenException("You are not a participant in this chat");
            }
        }

        // Generate a unique token
        var token = GenerateSecureToken();
        var expiresAt = DateTime.UtcNow.AddMinutes(5); // Token valid for 5 minutes

        lock (_tokenLock)
        {
            // Clean up expired tokens
            var expiredKeys = _pendingTokens
                .Where(kvp => kvp.Value.ExpiresAt < DateTime.UtcNow)
                .Select(kvp => kvp.Key)
                .ToList();
            foreach (var key in expiredKeys)
            {
                _pendingTokens.Remove(key);
            }

            _pendingTokens[token] = (userId, chatId, expiresAt);
        }

        // QR code content is just the token - the mobile app will encode this
        return new GenerateQRCodeResponse
        {
            QRCode = $"ondock://contact/{token}",
            Token = token,
            ExpiresAt = expiresAt
        };
    }

    public async Task<ScanQRCodeResponse> ScanQRCodeAsync(Guid userId, ScanQRCodeRequest request)
    {
        (Guid targetUserId, string? chatId, DateTime expiresAt) tokenData;

        lock (_tokenLock)
        {
            if (!_pendingTokens.TryGetValue(request.Token, out tokenData))
            {
                return new ScanQRCodeResponse
                {
                    Success = false,
                    Message = "Invalid or expired QR code"
                };
            }

            if (tokenData.expiresAt < DateTime.UtcNow)
            {
                _pendingTokens.Remove(request.Token);
                return new ScanQRCodeResponse
                {
                    Success = false,
                    Message = "QR code has expired"
                };
            }

            // Remove the token after use (one-time use)
            _pendingTokens.Remove(request.Token);
        }

        // Can't scan your own QR code
        if (tokenData.targetUserId == userId)
        {
            return new ScanQRCodeResponse
            {
                Success = false,
                Message = "You cannot scan your own QR code"
            };
        }

        // Verify scanner is also a participant in the same chat
        var isParticipant = string.IsNullOrEmpty(tokenData.chatId) || await _db.ChatParticipants
            .AnyAsync(p => p.ChatId == tokenData.chatId && p.UserId == userId);

        if (!isParticipant)
        {
            return new ScanQRCodeResponse
            {
                Success = false,
                Message = "You must be in the same chat to exchange contacts"
            };
        }

        // Check if already matched
        var existingMatch = await GetExistingMatchAsync(userId, tokenData.targetUserId);
        if (existingMatch != null)
        {
            return new ScanQRCodeResponse
            {
                Success = true,
                ContactId = existingMatch.MatchId.ToString(),
                DisplayName = "Contact",
                Message = "You are already connected with this user"
            };
        }

        // Create the permanent contact match
        // When users scan QR codes in person, they are revealing their identities to each other
        var qrCodeHash = ComputeHash(request.Token);
        var match = new UserMatch
        {
            MatchId = Guid.NewGuid(),
            User1Id = userId < tokenData.targetUserId ? userId : tokenData.targetUserId,
            User2Id = userId < tokenData.targetUserId ? tokenData.targetUserId : userId,
            QRCodeHash = qrCodeHash,
            Status = MatchStatus.Confirmed,
            User1Revealed = true, // Auto-reveal when QR is scanned - they met in person
            User2Revealed = true, // Auto-reveal when QR is scanned - they met in person
            ChatId = tokenData.chatId,
            InitiatedAt = DateTime.UtcNow,
            ConfirmedAt = DateTime.UtcNow
        };

        _db.UserMatches.Add(match);
        await _db.SaveChangesAsync();

        return new ScanQRCodeResponse
        {
            Success = true,
            ContactId = match.MatchId.ToString(),
            DisplayName = "New Contact",
            Message = "Contact added successfully! You can now chat without restrictions."
        };
    }

    public async Task<PermanentContactsResponse> GetPermanentContactsAsync(Guid userId)
    {
        var matches = await _db.UserMatches
            .AsNoTracking()
            .Where(m => m.User1Id == userId || m.User2Id == userId)
            .Include(m => m.User1)
            .Include(m => m.User2)
            .OrderByDescending(m => m.InitiatedAt)
            .ToListAsync();

        var contacts = matches.Select(m =>
        {
            var otherUser = m.User1Id == userId ? m.User2 : m.User1;
            // Build display name from FirstName/LastName or use anonymous
            var realName = string.IsNullOrWhiteSpace(otherUser.FirstName)
                ? null
                : $"{otherUser.FirstName} {otherUser.LastName}".Trim();
            return new ondock.api.DTOs.Contact.PermanentContactDto
            {
                ContactId = m.MatchId.ToString(),
                UserId = otherUser.Id,
                DisplayName = (m.User1Revealed && m.User2Revealed)
                    ? realName ?? "Driver"
                    : MakeAnonymousDisplayName(otherUser.Id),
                AvatarUrl = null, // User entity doesn't have AvatarUrl
                MatchedAt = m.InitiatedAt,
                IdentityRevealed = m.User1Revealed && m.User2Revealed
            };
        }).ToList();

        return new PermanentContactsResponse
        {
            Count = contacts.Count(),
            Contacts = contacts
        };
    }

    public async Task<bool> IsPermanentContactAsync(Guid userId1, Guid userId2)
    {
        var match = await GetExistingMatchAsync(userId1, userId2);
        return match != null;
    }

    private async Task<UserMatch?> GetExistingMatchAsync(Guid userId1, Guid userId2)
    {
        var (u1, u2) = userId1 < userId2 ? (userId1, userId2) : (userId2, userId1);
        return await _db.UserMatches
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.User1Id == u1 && m.User2Id == u2);
    }

    private static string GenerateSecureToken()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }

    private static string ComputeHash(string input)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }

    private static string MakeAnonymousDisplayName(Guid userId)
    {
        var hash = userId.GetHashCode();
        var adjectives = new[] { "Swift", "Steady", "Reliable", "Friendly", "Cool", "Brave", "Calm", "Sharp" };
        var nouns = new[] { "Hauler", "Trucker", "Driver", "Rider", "Mover", "Roller", "Cruiser", "Pilot" };
        var adj = adjectives[Math.Abs(hash) % adjectives.Length];
        var noun = nouns[Math.Abs(hash / adjectives.Length) % nouns.Length];
        var num = Math.Abs(hash % 1000);
        return $"{adj} {noun} {num}";
    }

    #region gRPC Support Methods

    public async Task<GenerateQrCodeResult> GenerateQrCodeAsync(Guid userId)
    {
        var result = await GenerateQRCodeAsync(userId, null);
        return new GenerateQrCodeResult(result.QRCode, result.ExpiresAt);
    }

    public async Task<ScanQrCodeResult> ScanQrCodeAsync(Guid userId, string qrData)
    {
        // Extract token from QR data (format: ondock://contact/{token})
        var token = qrData;
        if (qrData.StartsWith("ondock://contact/"))
        {
            token = qrData.Substring("ondock://contact/".Length);
        }

        var result = await ScanQRCodeAsync(userId, new ScanQRCodeRequest { Token = token });
        
        Guid? contactId = null;
        if (!string.IsNullOrEmpty(result.ContactId) && Guid.TryParse(result.ContactId, out var parsedId))
        {
            contactId = parsedId;
        }

        return new ScanQrCodeResult(result.Success, result.Message, contactId, result.DisplayName);
    }

    public async Task<IReadOnlyList<Interfaces.PermanentContactDto>> GetPermanentContactsListAsync(Guid userId)
    {
        var response = await GetPermanentContactsAsync(userId);
        return response.Contacts.Select(c => new Interfaces.PermanentContactDto(
            Guid.TryParse(c.ContactId, out var cid) ? cid : Guid.Empty,
            c.UserId,
            c.DisplayName,
            null, // ColorName not stored in existing DTO
            c.MatchedAt
        )).ToList();
    }

    public async Task<bool> IsPermanentContactAsync(Guid userId, string anonymousUserId)
    {
        // Parse the anonymous user ID to get the actual user ID
        if (!anonymousUserId.StartsWith("anon_"))
        {
            return false;
        }

        var prefix = anonymousUserId.Substring(5);
        
        // Look up user by GUID prefix match
        var targetUser = await _db.Users
            .FirstOrDefaultAsync(u => u.Id.ToString().Replace("-", "").StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

        if (targetUser == null)
        {
            return false;
        }

        return await IsPermanentContactAsync(userId, targetUser.Id);
    }

    public async Task RemovePermanentContactAsync(Guid userId, Guid contactId)
    {
        var match = await _db.UserMatches
            .FirstOrDefaultAsync(m => m.MatchId == contactId && (m.User1Id == userId || m.User2Id == userId));

        if (match == null)
        {
            throw new NotFoundException("Contact not found");
        }

        _db.UserMatches.Remove(match);
        await _db.SaveChangesAsync();
    }

    #endregion
}
