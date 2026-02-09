using ondock.api.DTOs.Chat;

namespace ondock.api.Services.Interfaces;

/// <summary>
/// Service for broadcasting chat events to gRPC stream subscribers
/// </summary>
public interface IChatBroadcastService
{
    /// <summary>
    /// Broadcast a new message to all chat participants via gRPC streams
    /// </summary>
    Task BroadcastMessageAsync(string chatId, ChatMessageResponse message, Guid senderUserId);

    /// <summary>
    /// Broadcast a thread update (e.g., last message changed) to update chat lists
    /// </summary>
    Task BroadcastThreadUpdateAsync(string chatId);

    /// <summary>
    /// Broadcast read receipts to chat participants
    /// </summary>
    Task BroadcastMessagesReadAsync(string chatId, Guid readerUserId);
}
