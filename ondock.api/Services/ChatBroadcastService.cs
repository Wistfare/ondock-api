using Google.Protobuf.WellKnownTypes;
using ondock.api.DTOs.Chat;
using ondock.api.Grpc.Services;
using ondock.api.Services.Interfaces;
using Ondock.Api.Grpc.V1;

namespace ondock.api.Services;

/// <summary>
/// Service for broadcasting chat events to gRPC stream subscribers
/// </summary>
public class ChatBroadcastService : IChatBroadcastService
{
    private readonly IChatService _chatService;
    private readonly ILogger<ChatBroadcastService> _logger;

    public ChatBroadcastService(IChatService chatService, ILogger<ChatBroadcastService> logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    public async Task BroadcastMessageAsync(string chatId, ChatMessageResponse message, Guid senderUserId)
    {
        try
        {
            // Get all participants in this chat
            var participantUserIds = await _chatService.GetChatParticipantUserIdsAsync(chatId);

            // Broadcast to all participants except sender
            foreach (var participantUserId in participantUserIds)
            {
                if (participantUserId == senderUserId) continue;

                // Build the chat message for gRPC with correct IsMine flag for this recipient
                var grpcMessage = new ChatMessage
                {
                    MessageId = message.MessageId.ToString(),
                    ChatId = chatId,
                    SenderId = message.SenderTemporaryUserId ?? "",
                    SenderDisplayName = message.SenderDisplayName ?? "",
                    SenderColor = message.SenderDisplayColor ?? "",
                    IsMine = message.SenderUserId == participantUserId,
                    Content = message.EncryptedContent ?? "",
                    MessageType = MapMessageType(message.MessageType),
                    CreatedAt = Timestamp.FromDateTime(message.Timestamp.ToUniversalTime()),
                    MediaUrl = message.MediaUrl ?? "",
                    MediaFilename = message.MediaFileName ?? "",
                    MediaSizeBytes = message.MediaFileSize ?? 0,
                    MediaDurationSeconds = message.MediaDurationSeconds ?? 0
                };

                var chatEvent = new ChatEvent
                {
                    EventId = Guid.NewGuid().ToString(),
                    Timestamp = Timestamp.FromDateTime(DateTime.UtcNow),
                    MessageReceived = new MessageReceivedEvent
                    {
                        Message = grpcMessage
                    }
                };

                await ChatGrpcService.PushEventToUser(participantUserId, chatEvent);
                _logger.LogDebug("Broadcast message to user {UserId} for chat {ChatId}", participantUserId, chatId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast message for chat {ChatId}", chatId);
        }
    }

    public async Task BroadcastThreadUpdateAsync(string chatId)
    {
        try
        {
            // Get all participants in this chat
            var participantUserIds = await _chatService.GetChatParticipantUserIdsAsync(chatId);

            // For each participant, trigger a thread refresh via the static method
            foreach (var participantUserId in participantUserIds)
            {
                var evt = new ChatThreadEvent
                {
                    EventType = ChatThreadEventType.ThreadUpdated,
                    Timestamp = Timestamp.FromDateTime(DateTime.UtcNow)
                };

                await ChatGrpcService.BroadcastThreadUpdateAsync(participantUserId, evt);
                _logger.LogDebug("Broadcast thread update to user {UserId} for chat {ChatId}", participantUserId, chatId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast thread update for chat {ChatId}", chatId);
        }
    }

    public async Task BroadcastMessagesReadAsync(string chatId, Guid readerUserId)
    {
        try
        {
            var anonymousUserId = "anon_" + readerUserId.ToString("N").Substring(0, 8);

            var chatEvent = new ChatEvent
            {
                EventId = Guid.NewGuid().ToString(),
                Timestamp = Timestamp.FromDateTime(DateTime.UtcNow),
                MessagesRead = new MessagesReadEvent
                {
                    ChatId = chatId,
                    ReaderId = anonymousUserId,
                    ReadAt = Timestamp.FromDateTime(DateTime.UtcNow)
                }
            };

            // Get all participants and broadcast to others
            var participantUserIds = await _chatService.GetChatParticipantUserIdsAsync(chatId);
            
            foreach (var participantUserId in participantUserIds)
            {
                if (participantUserId == readerUserId) continue;

                await ChatGrpcService.PushEventToUser(participantUserId, chatEvent);
                _logger.LogDebug("Broadcast read receipt to user {UserId} for chat {ChatId}", participantUserId, chatId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast read receipts for chat {ChatId}", chatId);
        }
    }

    private static MessageType MapMessageType(Data.Entities.MessageType type)
    {
        return type switch
        {
            Data.Entities.MessageType.Text => MessageType.Text,
            Data.Entities.MessageType.Audio => MessageType.Audio,
            Data.Entities.MessageType.Image => MessageType.Image,
            Data.Entities.MessageType.Video => MessageType.Video,
            Data.Entities.MessageType.File => MessageType.File,
            _ => MessageType.Text
        };
    }
}
