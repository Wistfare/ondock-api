using Microsoft.EntityFrameworkCore;
using ondock.api.Data;
using ondock.api.Data.Entities;
using ondock.api.DTOs.Chat;
using ondock.api.Services.Interfaces;

namespace ondock.api.Services;

public class EncryptionService : IEncryptionService
{
    private readonly OnDockDbContext _db;

    public EncryptionService(OnDockDbContext db)
    {
        _db = db;
    }

    public async Task<PublicKeyResponse> RegisterKeyAsync(Guid userId, RegisterKeyRequest request)
    {
        var now = DateTime.UtcNow;

        // Check if user already has an active key
        var existingKey = await _db.UserEncryptionKeys
            .FirstOrDefaultAsync(k => k.UserId == userId && k.IsActive);

        if (existingKey != null)
        {
            // Revoke existing key and create new one
            existingKey.IsActive = false;
            existingKey.RevokedAt = now;
        }

        // Create new key
        var newKey = new UserEncryptionKey
        {
            KeyId = Guid.NewGuid(),
            UserId = userId,
            PublicKey = request.PublicKey,
            KeyType = request.KeyType ?? "RSA-2048",
            IsActive = true,
            CreatedAt = now
        };
        _db.UserEncryptionKeys.Add(newKey);

        await _db.SaveChangesAsync();

        return new PublicKeyResponse
        {
            UserId = userId,
            PublicKey = request.PublicKey,
            KeyType = request.KeyType ?? "RSA-2048"
        };
    }

    public async Task<PublicKeyResponse?> GetPublicKeyAsync(Guid userId)
    {
        var key = await _db.UserEncryptionKeys
            .AsNoTracking()
            .FirstOrDefaultAsync(k => k.UserId == userId && k.IsActive);

        if (key == null) return null;

        return new PublicKeyResponse
        {
            UserId = key.UserId,
            PublicKey = key.PublicKey,
            KeyType = key.KeyType
        };
    }

    public async Task<KeyExchangeResponse> InitiateKeyExchangeAsync(Guid userId, KeyExchangeRequest request)
    {
        var now = DateTime.UtcNow;

        // Verify user is a participant in this chat
        var isParticipant = await _db.ChatParticipants
            .AnyAsync(p => p.ChatId == request.ChatId && p.UserId == userId);

        if (!isParticipant)
        {
            throw new InvalidOperationException("User is not a participant in this chat");
        }

        // Check if exchange already exists
        var existingExchange = await _db.ChatKeyExchanges
            .FirstOrDefaultAsync(e => e.ChatId == request.ChatId &&
                                      e.FromUserId == userId &&
                                      e.ToUserId == request.ToUserId);

        if (existingExchange != null)
        {
            // Update existing exchange
            existingExchange.EncryptedSessionKey = request.EncryptedSessionKey;
        }
        else
        {
            // Create new exchange
            var exchange = new ChatKeyExchange
            {
                ExchangeId = Guid.NewGuid(),
                ChatId = request.ChatId,
                FromUserId = userId,
                ToUserId = request.ToUserId,
                EncryptedSessionKey = request.EncryptedSessionKey,
                CreatedAt = now
            };
            _db.ChatKeyExchanges.Add(exchange);
        }

        await _db.SaveChangesAsync();

        return new KeyExchangeResponse
        {
            ChatId = request.ChatId,
            InitiatorUserId = userId,
            TargetUserId = request.ToUserId,
            EncryptedKey = request.EncryptedSessionKey,
            CreatedAt = now
        };
    }

    public async Task<IReadOnlyList<KeyExchangeResponse>> GetPendingKeyExchangesAsync(Guid userId, string chatId)
    {
        var exchanges = await _db.ChatKeyExchanges
            .AsNoTracking()
            .Where(e => e.ChatId == chatId && e.ToUserId == userId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();

        return exchanges.Select(e => new KeyExchangeResponse
        {
            ChatId = e.ChatId,
            InitiatorUserId = e.FromUserId,
            TargetUserId = e.ToUserId,
            EncryptedKey = e.EncryptedSessionKey,
            CreatedAt = e.CreatedAt
        }).ToList();
    }
}
