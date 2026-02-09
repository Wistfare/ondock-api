using Microsoft.EntityFrameworkCore;
using ondock.api.Data;
using ondock.api.DTOs.Chat;
using ondock.api.Services.Interfaces;

namespace ondock.api.Services;

public class IdentityService : IIdentityService
{
    private readonly OnDockDbContext _db;
    private readonly IChatRealtimeService _realtime;

    public IdentityService(OnDockDbContext db, IChatRealtimeService realtime)
    {
        _db = db;
        _realtime = realtime;
    }

    public async Task<IdentityRevealResponse> RequestIdentityRevealAsync(Guid userId, IdentityRevealRequest request)
    {
        // Verify both users are participants in the chat
        var requesterParticipant = await _db.ChatParticipants
            .FirstOrDefaultAsync(p => p.ChatId == request.ChatId && p.UserId == userId);

        var targetParticipant = await _db.ChatParticipants
            .FirstOrDefaultAsync(p => p.ChatId == request.ChatId && p.UserId == request.TargetUserId);

        if (requesterParticipant == null || targetParticipant == null)
        {
            throw new InvalidOperationException("Both users must be participants in the chat");
        }

        // Check if there's already a UserMatch for this pair
        var existingMatch = await _db.UserMatches
            .FirstOrDefaultAsync(m => m.ChatId == request.ChatId &&
                ((m.User1Id == userId && m.User2Id == request.TargetUserId) ||
                 (m.User1Id == request.TargetUserId && m.User2Id == userId)));

        if (existingMatch == null)
        {
            // Create new match with reveal request
            existingMatch = new Data.Entities.UserMatch
            {
                MatchId = Guid.NewGuid(),
                User1Id = userId,
                User2Id = request.TargetUserId,
                ChatId = request.ChatId,
                User1Revealed = true,
                User2Revealed = false,
                InitiatedAt = DateTime.UtcNow
            };
            _db.UserMatches.Add(existingMatch);
        }
        else
        {
            // Update existing match
            if (existingMatch.User1Id == userId)
            {
                existingMatch.User1Revealed = true;
            }
            else
            {
                existingMatch.User2Revealed = true;
            }
        }

        await _db.SaveChangesAsync();

        var response = await BuildRevealResponseAsync(existingMatch, userId);

        // Notify target user via SignalR
        await _realtime.SendIdentityRevealRequestedAsync(request.ChatId, request.TargetUserId, new
        {
            chatId = request.ChatId,
            requesterId = userId,
            targetUserId = request.TargetUserId,
            timestamp = DateTime.UtcNow
        });

        return response;
    }

    public async Task<IdentityRevealResponse> AcceptIdentityRevealAsync(Guid userId, string chatId, Guid requesterId)
    {
        var match = await _db.UserMatches
            .FirstOrDefaultAsync(m => m.ChatId == chatId &&
                ((m.User1Id == userId && m.User2Id == requesterId) ||
                 (m.User1Id == requesterId && m.User2Id == userId)));

        if (match == null)
        {
            throw new InvalidOperationException("No pending reveal request found");
        }

        // Mark this user as revealed
        if (match.User1Id == userId)
        {
            match.User1Revealed = true;
        }
        else
        {
            match.User2Revealed = true;
        }

        await _db.SaveChangesAsync();

        var response = await BuildRevealResponseAsync(match, userId);

        // If both revealed, notify both users
        if (match.User1Revealed && match.User2Revealed)
        {
            await _realtime.SendIdentityRevealedAsync(chatId, new
            {
                chatId,
                user1Id = match.User1Id,
                user2Id = match.User2Id,
                bothRevealed = true,
                timestamp = DateTime.UtcNow
            });
        }

        return response;
    }

    public async Task<IdentityRevealResponse?> GetIdentityRevealStatusAsync(Guid userId, string chatId, Guid otherUserId)
    {
        var match = await _db.UserMatches
            .FirstOrDefaultAsync(m => m.ChatId == chatId &&
                ((m.User1Id == userId && m.User2Id == otherUserId) ||
                 (m.User1Id == otherUserId && m.User2Id == userId)));

        if (match == null)
        {
            return null;
        }

        return await BuildRevealResponseAsync(match, userId);
    }

    public async Task<IReadOnlyList<IdentityRevealResponse>> GetPendingRevealRequestsAsync(Guid userId)
    {
        var matches = await _db.UserMatches
            .Where(m => (m.User1Id == userId && m.User2Revealed && !m.User1Revealed) ||
                        (m.User2Id == userId && m.User1Revealed && !m.User2Revealed))
            .ToListAsync();

        var responses = new List<IdentityRevealResponse>();
        foreach (var match in matches)
        {
            responses.Add(await BuildRevealResponseAsync(match, userId));
        }

        return responses;
    }

    private async Task<IdentityRevealResponse> BuildRevealResponseAsync(Data.Entities.UserMatch match, Guid currentUserId)
    {
        var isUser1 = match.User1Id == currentUserId;
        var requesterId = isUser1 ? match.User1Id : match.User2Id;
        var targetUserId = isUser1 ? match.User2Id : match.User1Id;
        var requesterRevealed = isUser1 ? match.User1Revealed : match.User2Revealed;
        var targetRevealed = isUser1 ? match.User2Revealed : match.User1Revealed;

        RevealedIdentityDto? requesterIdentity = null;
        RevealedIdentityDto? targetIdentity = null;

        // Only include identity info if revealed
        if (requesterRevealed)
        {
            var requester = await _db.Users.FindAsync(requesterId);
            if (requester != null)
            {
                requesterIdentity = new RevealedIdentityDto
                {
                    UserId = requester.Id,
                    DisplayName = $"{requester.FirstName} {requester.LastName}".Trim(),
                    Email = requester.Email,
                    PhoneNumber = requester.PhoneNumber
                };
            }
        }

        if (targetRevealed)
        {
            var target = await _db.Users.FindAsync(targetUserId);
            if (target != null)
            {
                targetIdentity = new RevealedIdentityDto
                {
                    UserId = target.Id,
                    DisplayName = $"{target.FirstName} {target.LastName}".Trim(),
                    Email = target.Email,
                    PhoneNumber = target.PhoneNumber
                };
            }
        }

        return new IdentityRevealResponse
        {
            ChatId = match.ChatId ?? string.Empty,
            RequesterId = requesterId,
            TargetUserId = targetUserId,
            RequesterRevealed = requesterRevealed,
            TargetRevealed = targetRevealed,
            BothRevealed = requesterRevealed && targetRevealed,
            RequesterIdentity = requesterIdentity,
            TargetIdentity = targetIdentity,
            RequestedAt = match.InitiatedAt
        };
    }
}
