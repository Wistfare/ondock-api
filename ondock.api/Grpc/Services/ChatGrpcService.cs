using System.Collections.Concurrent;
using System.Security.Claims;
using System.Threading.Channels;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using ondock.api.Services.Interfaces;

// Aliases to resolve ambiguity
using GrpcEmpty = Ondock.Api.Grpc.V1.Empty;
using GrpcChatMessage = Ondock.Api.Grpc.V1.ChatMessage;
using GrpcReportChatRequest = Ondock.Api.Grpc.V1.ReportChatRequest;
using GrpcReportChatResponse = Ondock.Api.Grpc.V1.ReportChatResponse;
using GrpcUpsertChatProfileRequest = Ondock.Api.Grpc.V1.UpsertChatProfileRequest;
using DtoReportChatRequest = ondock.api.DTOs.Chat.ReportChatRequest;
using DtoUpsertChatProfileRequest = ondock.api.DTOs.Chat.UpsertChatProfileRequest;
using DtoChatMessageResponse = ondock.api.DTOs.Chat.ChatMessageResponse;
using Ondock.Api.Grpc.V1;

namespace ondock.api.Grpc.Services;

[Authorize]
public class ChatGrpcService : ChatService.ChatServiceBase
{
    private readonly IChatService _chatService;
    private readonly IUserSettingsService _userSettingsService;
    private readonly IChatRealtimeService _realtimeService;
    private readonly ILocationService _locationService;
    private readonly ILogger<ChatGrpcService> _logger;

    // Track active stream subscriptions: UserId -> Channel for pushing events
    private static readonly ConcurrentDictionary<Guid, Channel<ChatEvent>> ActiveStreams = new();

    public ChatGrpcService(
        IChatService chatService,
        IUserSettingsService userSettingsService,
        IChatRealtimeService realtimeService,
        ILocationService locationService,
        ILogger<ChatGrpcService> logger)
    {
        _chatService = chatService;
        _userSettingsService = userSettingsService;
        _realtimeService = realtimeService;
        _locationService = locationService;
        _logger = logger;
    }

    public override async Task<GetActiveChatsResponse> GetActiveChats(GetActiveChatsRequest request, ServerCallContext context)
    {
        var userId = GetUserId(context);
        double? lat = request.Latitude;
        double? lng = request.Longitude;

        var result = await _chatService.GetActiveChatsAsync(userId, lat, lng);

        var response = new GetActiveChatsResponse();

        foreach (var chat in result.Chats)
        {
            response.Chats.Add(new ChatThread
            {
                ChatId = chat.ChatId,
                Name = chat.OtherDisplayName ?? "",
                ColorName = chat.OtherDisplayColor ?? "",
                LastMessage = chat.LastMessage ?? "",
                LastMessageIsMine = chat.LastMessageIsMe,
                UpdatedAt = chat.LastMessageTimestamp.HasValue 
                    ? Timestamp.FromDateTime(chat.LastMessageTimestamp.Value.ToUniversalTime()) 
                    : Timestamp.FromDateTime(DateTime.UtcNow),
                UnreadCount = 0, // Not in DTO
                DistanceKm = chat.DistanceMiles.HasValue ? chat.DistanceMiles.Value * 1.60934 : 0,
                Status = "driving",
                IsPermanentContact = chat.IsPermanentContact,
                CanMeet = chat.CanMeet,
                CommonInterestsCount = chat.CommonInterestsCount,
                OtherUserId = chat.OtherAnonymousUserId ?? ""
            });
        }

        // Add nearby users if location is provided
        if (lat.HasValue && lng.HasValue)
        {
            var nearbyResult = await _locationService.GetNearbyUsersAsync(userId, lat.Value, lng.Value, 0, 10);
            foreach (var user in nearbyResult.Users)
            {
                response.NearbyUsers.Add(new NearbyUser
                {
                    AnonymousUserId = user.AnonymousUserId ?? "",
                    DisplayName = user.DisplayName ?? "",
                    ColorName = user.Color ?? "",
                    DistanceKm = user.DistanceMiles * 1.60934,
                    CommonInterestsCount = user.CommonInterestsCount,
                    IsPermanentContact = false,
                    IsOnline = user.IsOnline
                });
            }
        }

        return response;
    }

    public override async Task<JoinChatResponse> JoinChat(JoinChatRequest request, ServerCallContext context)
    {
        var userId = GetUserId(context);

        var dto = new ondock.api.DTOs.Chat.JoinChatRequest
        {
            TargetAnonymousUserId = request.TargetAnonymousUserId,
            AnnouncementId = string.IsNullOrEmpty(request.AnnouncementId) ? null : request.AnnouncementId
        };

        var result = await _chatService.JoinAsync(userId, dto);

        // Broadcast thread update to both users so their streams refresh
        if (ActiveThreadStreams.TryGetValue(userId, out var userCtx))
        {
            var evt = await BuildThreadEventAsync(userId, userCtx, ChatThreadEventType.ThreadCreated);
            await userCtx.EventChannel.Writer.WriteAsync(evt);
        }
        
        // Also notify the other user if they're streaming
        if (result.OtherUserId.HasValue && ActiveThreadStreams.TryGetValue(result.OtherUserId.Value, out var otherCtx))
        {
            var evt = await BuildThreadEventAsync(result.OtherUserId.Value, otherCtx, ChatThreadEventType.ThreadCreated);
            await otherCtx.EventChannel.Writer.WriteAsync(evt);
        }

        return new JoinChatResponse
        {
            ChatId = result.ChatId,
            IsNew = true, // Assume new for now
            Chat = new ChatThread
            {
                ChatId = result.ChatId,
                Name = result.OtherDisplayName ?? "",
                ColorName = result.OtherDisplayColor ?? ""
            }
        };
    }

    public override async Task<GrpcEmpty> LeaveChat(LeaveChatRequest request, ServerCallContext context)
    {
        var userId = GetUserId(context);

        var dto = new ondock.api.DTOs.Chat.LeaveChatRequest
        {
            ChatId = request.ChatId
        };

        await _chatService.LeaveAsync(userId, dto);
        return new GrpcEmpty();
    }

    public override async Task<GetChatHistoryResponse> GetChatHistory(GetChatHistoryRequest request, ServerCallContext context)
    {
        var userId = GetUserId(context);
        var take = request.Take > 0 ? request.Take : 50;

        var result = await _chatService.GetHistoryAsync(userId, request.ChatId, take);

        var response = new GetChatHistoryResponse();

        foreach (var msg in result.Messages)
        {
            response.Messages.Add(MapChatMessage(msg, userId));
        }

        response.HasMore = result.Messages.Count >= take;
        return response;
    }

    public override async Task<SendMessageResponse> SendMessage(SendMessageRequest request, ServerCallContext context)
    {
        var userId = GetUserId(context);

        var dto = new ondock.api.DTOs.Chat.SendMessageRequest
        {
            ChatId = request.ChatId,
            EncryptedContent = request.EncryptedContent,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            SpeedMph = request.SpeedMph,
            QuotedContent = request.QuotedContent
        };

        var result = await _chatService.SendMessageAsync(userId, dto);

        // Broadcast to other gRPC stream subscribers
        await BroadcastMessageReceived(request.ChatId, result, userId);

        // Also broadcast thread update to update last message in chat list
        await BroadcastThreadUpdateForChat(request.ChatId);

        return new SendMessageResponse
        {
            MessageId = result.MessageId.ToString(),
            Timestamp = Timestamp.FromDateTime(result.Timestamp.ToUniversalTime()),
            ClientMessageId = request.ClientMessageId
        };
    }

    public override async Task<GrpcEmpty> UpdatePresence(UpdatePresenceRequest request, ServerCallContext context)
    {
        var userId = GetUserId(context);

        await _chatService.UpdatePresenceAsync(
            userId,
            request.ChatId,
            request.Latitude,
            request.Longitude,
            request.SpeedMph);

        return new GrpcEmpty();
    }

    public override async Task<GrpcEmpty> MarkAsRead(MarkAsReadRequest request, ServerCallContext context)
    {
        var userId = GetUserId(context);
        await _chatService.MarkMessagesAsReadAsync(userId, request.ChatId);

        // Broadcast read receipt to other participants
        await BroadcastMessagesRead(request.ChatId, userId);

        return new GrpcEmpty();
    }

    public override async Task<GrpcReportChatResponse> ReportChat(GrpcReportChatRequest request, ServerCallContext context)
    {
        var userId = GetUserId(context);

        var dto = new DtoReportChatRequest
        {
            ChatId = request.ChatId,
            TargetAnonymousUserId = request.TargetAnonymousUserId,
            Reason = request.Reason,
            Details = request.Details
        };

        var result = await _chatService.ReportChatAsync(userId, dto);

        return new GrpcReportChatResponse
        {
            ReportId = result.ReportId,
            Message = "Report submitted successfully"
        };
    }

    public override async Task<ChatProfileResponse> GetChatProfile(GrpcEmpty request, ServerCallContext context)
    {
        var userId = GetUserId(context);
        var profile = await _chatService.GetProfileAsync(userId);

        var response = new ChatProfileResponse
        {
            HasProfile = profile.HasProfile,
            DisplayName = "", // Display name not in current DTO
            ColorName = "", // Color name not in current DTO
            AnonymousUserId = userId.ToString().Substring(0, 8), // Generate from userId
            VehicleTypeId = profile.VehicleType?.VehicleTypeId.ToString() ?? "",
            VehicleBrandId = profile.VehicleBrand?.BrandId.ToString() ?? "",
            CustomVehicleType = "",
            CustomVehicleBrand = ""
        };

        // Add categoryIds if category exists
        if (profile.Category?.CategoryId != null)
        {
            response.CategoryIds.Add(profile.Category.CategoryId.ToString());
        }

        // Add subCategoryIds if subcategory exists
        if (profile.SubCategory?.SubCategoryId != null)
        {
            response.SubCategoryIds.Add(profile.SubCategory.SubCategoryId.ToString());
        }

        return response;
    }

    public override async Task<ChatProfileResponse> UpsertChatProfile(GrpcUpsertChatProfileRequest request, ServerCallContext context)
    {
        var userId = GetUserId(context);

        var dto = new DtoUpsertChatProfileRequest
        {
            DisplayName = request.DisplayName,
            ColorName = request.ColorName,
            CategoryIds = request.CategoryIds.ToList(),
            SubCategoryIds = request.SubCategoryIds.ToList(),
            VehicleTypeId = request.VehicleTypeId,
            VehicleBrandId = request.VehicleBrandId,
            CustomVehicleType = request.CustomVehicleType,
            CustomVehicleBrand = request.CustomVehicleBrand
        };

        var profile = await _chatService.UpsertProfileAsync(userId, dto);

        return new ChatProfileResponse
        {
            HasProfile = profile.HasProfile,
            DisplayName = request.DisplayName ?? "",
            ColorName = request.ColorName ?? "",
            AnonymousUserId = userId.ToString().Substring(0, 8)
        };
    }

    public override Task<DndStatusResponse> GetDndStatus(GrpcEmpty request, ServerCallContext context)
    {
        // DND status not yet implemented in user settings service
        // Return default disabled status
        return Task.FromResult(new DndStatusResponse
        {
            IsEnabled = false
        });
    }

    public override Task<DndStatusResponse> SetDndStatus(SetDndStatusRequest request, ServerCallContext context)
    {
        // DND status not yet implemented in user settings service
        // Return the requested status as if it was set
        var response = new DndStatusResponse
        {
            IsEnabled = request.IsEnabled
        };

        if (request.IsEnabled && request.DurationMinutes > 0)
        {
            response.EnabledUntil = Timestamp.FromDateTime(DateTime.UtcNow.AddMinutes(request.DurationMinutes));
        }

        return Task.FromResult(response);
    }

    // === STREAMING RPC - Real-time chat events ===
    public override async Task StreamChatEvents(StreamChatEventsRequest request, IServerStreamWriter<ChatEvent> responseStream, ServerCallContext context)
    {
        var userId = GetUserId(context);
        _logger.LogInformation("gRPC: User {UserId} started streaming chat events", userId);

        // Create a channel for this user's events
        var channel = Channel.CreateUnbounded<ChatEvent>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        ActiveStreams[userId] = channel;

        try
        {
            // Read from channel and write to gRPC stream
            await foreach (var chatEvent in channel.Reader.ReadAllAsync(context.CancellationToken))
            {
                await responseStream.WriteAsync(chatEvent);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("gRPC: User {UserId} disconnected from chat stream", userId);
        }
        finally
        {
            ActiveStreams.TryRemove(userId, out _);
            channel.Writer.Complete();
        }
    }

    public override async Task<GrpcEmpty> SendTypingIndicator(TypingIndicatorRequest request, ServerCallContext context)
    {
        var userId = GetUserId(context);
        var anonymousUserId = "anon_" + userId.ToString("N").Substring(0, 8);

        var chatEvent = new ChatEvent
        {
            EventId = Guid.NewGuid().ToString(),
            Timestamp = Timestamp.FromDateTime(DateTime.UtcNow)
        };

        if (request.IsTyping)
        {
            chatEvent.TypingStarted = new TypingStartedEvent
            {
                ChatId = request.ChatId,
                ParticipantId = anonymousUserId,
                DisplayName = "User"
            };
        }
        else
        {
            chatEvent.TypingStopped = new TypingStoppedEvent
            {
                ChatId = request.ChatId,
                ParticipantId = anonymousUserId
            };
        }

        // Broadcast to other participants in the chat
        await BroadcastTypingIndicator(request.ChatId, chatEvent, userId);

        return new GrpcEmpty();
    }

    // === Helper methods for broadcasting events ===

    private async Task BroadcastMessageReceived(string chatId, DtoChatMessageResponse message, Guid senderUserId)
    {
        // Get all participants in this chat
        var participantUserIds = await _chatService.GetChatParticipantUserIdsAsync(chatId);

        foreach (var participantUserId in participantUserIds)
        {
            // Don't send to the sender - they already have the message from the API response
            if (participantUserId == senderUserId)
                continue;

            // Create event with IsMine set correctly for this recipient
            var chatEvent = new ChatEvent
            {
                EventId = Guid.NewGuid().ToString(),
                Timestamp = Timestamp.FromDateTime(DateTime.UtcNow),
                MessageReceived = new MessageReceivedEvent
                {
                    Message = new GrpcChatMessage
                    {
                        MessageId = message.MessageId.ToString(),
                        ChatId = chatId,
                        SenderId = message.SenderTemporaryUserId ?? "",
                        SenderDisplayName = message.SenderDisplayName ?? "",
                        SenderColor = message.SenderDisplayColor ?? "",
                        IsMine = message.SenderUserId == participantUserId,
                        Content = message.EncryptedContent ?? "",
                        MessageType = MapMessageType(message.MessageType.ToString()),
                        CreatedAt = Timestamp.FromDateTime(message.Timestamp.ToUniversalTime())
                    }
                }
            };

            if (ActiveStreams.TryGetValue(participantUserId, out var channel))
            {
                await channel.Writer.WriteAsync(chatEvent);
            }
        }
    }

    private async Task BroadcastMessagesRead(string chatId, Guid readerUserId)
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

        await BroadcastToChatParticipants(chatId, chatEvent, readerUserId);
    }

    private async Task BroadcastTypingIndicator(string chatId, ChatEvent chatEvent, Guid senderUserId)
    {
        await BroadcastToChatParticipants(chatId, chatEvent, senderUserId);
    }

    private async Task BroadcastToChatParticipants(string chatId, ChatEvent chatEvent, Guid excludeUserId)
    {
        // Get chat participants from the chat service
        var participantUserIds = await _chatService.GetChatParticipantUserIdsAsync(chatId);

        foreach (var participantUserId in participantUserIds)
        {
            if (participantUserId == excludeUserId)
                continue;

            if (ActiveStreams.TryGetValue(participantUserId, out var channel))
            {
                await channel.Writer.WriteAsync(chatEvent);
            }
        }
    }

    // Static method for external services to push events (e.g., from REST API or background jobs)
    public static async Task PushEventToUser(Guid userId, ChatEvent chatEvent)
    {
        if (ActiveStreams.TryGetValue(userId, out var channel))
        {
            await channel.Writer.WriteAsync(chatEvent);
        }
    }

    private static GrpcChatMessage MapChatMessage(DtoChatMessageResponse msg, Guid? viewerUserId = null)
    {
        // Determine if this message is from the viewer
        var isMine = viewerUserId.HasValue && msg.SenderUserId == viewerUserId.Value;
        
        var chatMessage = new GrpcChatMessage
        {
            MessageId = msg.MessageId.ToString(),
            ChatId = msg.ChatId ?? "",
            SenderId = msg.SenderTemporaryUserId ?? "",
            SenderDisplayName = msg.SenderDisplayName ?? "",
            SenderColor = msg.SenderDisplayColor ?? "",
            IsMine = isMine,
            Content = msg.EncryptedContent ?? "",
            MessageType = MapMessageType(msg.MessageType.ToString()),
            CreatedAt = Timestamp.FromDateTime(msg.Timestamp.ToUniversalTime())
        };

        if (msg.ReadAt.HasValue)
        {
            chatMessage.ReadAt = Timestamp.FromDateTime(msg.ReadAt.Value.ToUniversalTime());
        }

        if (!string.IsNullOrEmpty(msg.MediaUrl))
        {
            chatMessage.MediaUrl = msg.MediaUrl;
        }

        if (!string.IsNullOrEmpty(msg.MediaFileName))
        {
            chatMessage.MediaFilename = msg.MediaFileName;
        }

        if (msg.MediaFileSize.HasValue)
        {
            chatMessage.MediaSizeBytes = msg.MediaFileSize.Value;
        }

        if (msg.MediaDurationSeconds.HasValue)
        {
            chatMessage.MediaDurationSeconds = msg.MediaDurationSeconds;
        }

        if (!string.IsNullOrEmpty(msg.QuotedContent))
        {
            chatMessage.QuotedContent = msg.QuotedContent;
        }

        return chatMessage;
    }

    private static MessageType MapMessageType(string? type)
    {
        return type?.ToLowerInvariant() switch
        {
            "text" => MessageType.Text,
            "image" => MessageType.Image,
            "audio" => MessageType.Audio,
            "video" => MessageType.Video,
            "file" => MessageType.File,
            "system" => MessageType.System,
            _ => MessageType.Text
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

    // ============ CHAT THREAD STREAMING ============
    
    private class ThreadStreamContext
    {
        public Channel<ChatThreadEvent> EventChannel { get; set; } = null!;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double RadiusMiles { get; set; }
        public HashSet<string> KnownThreadIds { get; } = new();
    }

    private static readonly ConcurrentDictionary<Guid, ThreadStreamContext> ActiveThreadStreams = new();

    public override async Task StreamChatThreads(
        StreamChatThreadsRequest request,
        IServerStreamWriter<ChatThreadEvent> responseStream,
        ServerCallContext context)
    {
        var userId = GetUserId(context);
        var radiusMiles = 50.0; // Default radius, will use request.RadiusMiles when proto is regenerated

        _logger.LogInformation(
            "[ThreadStream] User {UserId} starting stream at ({Lat}, {Lng}), radius={Radius}mi",
            userId, request.Latitude, request.Longitude, radiusMiles);

        var streamContext = new ThreadStreamContext
        {
            EventChannel = Channel.CreateUnbounded<ChatThreadEvent>(),
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            RadiusMiles = radiusMiles
        };

        ActiveThreadStreams[userId] = streamContext;

        try
        {
            // Send initial batch
            var initialEvent = await BuildThreadEventAsync(userId, streamContext, ChatThreadEventType.Initial);
            await responseStream.WriteAsync(initialEvent);

            // Track known threads
            foreach (var thread in initialEvent.Threads)
            {
                streamContext.KnownThreadIds.Add(thread.ChatId);
            }

            // Periodic refresh task (every 30 seconds for nearby users updates)
            var refreshCts = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken);
            _ = Task.Run(async () =>
            {
                while (!refreshCts.Token.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(30), refreshCts.Token);
                    try
                    {
                        var refreshEvent = await BuildThreadEventAsync(userId, streamContext, ChatThreadEventType.NearbyUpdated);
                        await streamContext.EventChannel.Writer.WriteAsync(refreshEvent, refreshCts.Token);
                    }
                    catch (OperationCanceledException) { break; }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "[ThreadStream] Error during refresh for user {UserId}", userId);
                    }
                }
            }, refreshCts.Token);

            // Read from channel and write to stream
            await foreach (var evt in streamContext.EventChannel.Reader.ReadAllAsync(context.CancellationToken))
            {
                await responseStream.WriteAsync(evt);
            }

            refreshCts.Cancel();
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("[ThreadStream] User {UserId} stream cancelled", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ThreadStream] Error for user {UserId}", userId);
            throw new RpcException(new Status(StatusCode.Internal, "Stream error"));
        }
        finally
        {
            ActiveThreadStreams.TryRemove(userId, out _);
            _logger.LogInformation("[ThreadStream] User {UserId} stream ended", userId);
        }
    }

    private async Task<ChatThreadEvent> BuildThreadEventAsync(Guid userId, ThreadStreamContext ctx, ChatThreadEventType eventType)
    {
        var result = await _chatService.GetActiveChatsAsync(userId, ctx.Latitude, ctx.Longitude);

        var evt = new ChatThreadEvent
        {
            Timestamp = Timestamp.FromDateTime(DateTime.UtcNow),
            EventType = eventType
        };

        // Track existing chat user IDs to avoid duplicates
        var chatUserIds = new HashSet<string>();

        foreach (var chat in result.Chats)
        {
            chatUserIds.Add(chat.OtherAnonymousUserId ?? "");
            evt.Threads.Add(new ChatThread
            {
                ChatId = chat.ChatId,
                Name = chat.OtherDisplayName ?? "",
                ColorName = chat.OtherDisplayColor ?? "",
                LastMessage = chat.LastMessage ?? "",
                LastMessageIsMine = chat.LastMessageIsMe,
                UpdatedAt = chat.LastMessageTimestamp.HasValue
                    ? Timestamp.FromDateTime(chat.LastMessageTimestamp.Value.ToUniversalTime())
                    : Timestamp.FromDateTime(DateTime.UtcNow),
                UnreadCount = 0,
                DistanceKm = chat.DistanceMiles.HasValue ? chat.DistanceMiles.Value * 1.60934 : 0,
                Status = "driving",
                IsPermanentContact = chat.IsPermanentContact,
                CanMeet = chat.CanMeet,
                CommonInterestsCount = chat.CommonInterestsCount,
                OtherUserId = chat.OtherAnonymousUserId ?? ""
            });
        }

        // Add nearby users (those without active chats)
        if (ctx.Latitude != 0 || ctx.Longitude != 0)
        {
            var nearbyResult = await _locationService.GetNearbyUsersAsync(userId, ctx.Latitude, ctx.Longitude, 0, 10);
            foreach (var user in nearbyResult.Users)
            {
                // Skip users who already have active chats
                if (chatUserIds.Contains(user.AnonymousUserId ?? "")) continue;

                evt.NearbyUsers.Add(new NearbyUser
                {
                    AnonymousUserId = user.AnonymousUserId ?? "",
                    DisplayName = user.DisplayName ?? "",
                    ColorName = user.Color ?? "",
                    DistanceKm = user.DistanceMiles * 1.60934,
                    CommonInterestsCount = user.CommonInterestsCount,
                    IsPermanentContact = false,
                    IsOnline = user.IsOnline
                });
            }
        }

        return evt;
    }

    /// <summary>
    /// Broadcast thread update to a specific user's stream (called when messages are sent, chats created, etc.)
    /// </summary>
    public static async Task BroadcastThreadUpdateAsync(Guid userId, ChatThreadEvent evt)
    {
        if (ActiveThreadStreams.TryGetValue(userId, out var ctx))
        {
            await ctx.EventChannel.Writer.WriteAsync(evt);
        }
    }

    /// <summary>
    /// Broadcast thread update to all participants of a chat
    /// </summary>
    private async Task BroadcastThreadUpdateForChat(string chatId)
    {
        // Get all participants of the chat
        var participants = await _chatService.GetChatParticipantUserIdsAsync(chatId);
        
        foreach (var participantId in participants)
        {
            if (ActiveThreadStreams.TryGetValue(participantId, out var ctx))
            {
                try
                {
                    var evt = await BuildThreadEventAsync(participantId, ctx, ChatThreadEventType.ThreadUpdated);
                    await ctx.EventChannel.Writer.WriteAsync(evt);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to broadcast thread update to user {UserId}", participantId);
                }
            }
        }
    }
}
