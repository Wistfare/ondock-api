using System.Security.Claims;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using ondock.api.Services.Interfaces;

// Aliases to resolve ambiguity
using GrpcEmpty = Ondock.Api.Grpc.V1.Empty;
using GrpcRegisterDeviceTokenRequest = Ondock.Api.Grpc.V1.RegisterDeviceTokenRequest;
using GrpcUnregisterDeviceTokenRequest = Ondock.Api.Grpc.V1.UnregisterDeviceTokenRequest;
using DtoRegisterDeviceTokenRequest = ondock.api.DTOs.Notifications.RegisterDeviceTokenRequest;
using Ondock.Api.Grpc.V1;

namespace ondock.api.Grpc.Services;

[Authorize]
public class NotificationsGrpcService : NotificationsService.NotificationsServiceBase
{
    private readonly IFcmNotificationService _fcmNotificationService;
    private readonly ILogger<NotificationsGrpcService> _logger;

    public NotificationsGrpcService(IFcmNotificationService fcmNotificationService, ILogger<NotificationsGrpcService> logger)
    {
        _fcmNotificationService = fcmNotificationService;
        _logger = logger;
    }

    public override async Task<GrpcEmpty> RegisterDeviceToken(GrpcRegisterDeviceTokenRequest request, ServerCallContext context)
    {
        var userId = GetUserId(context);

        var dto = new DtoRegisterDeviceTokenRequest
        {
            Token = request.DeviceToken,
            DeviceId = request.DeviceId,
            Platform = request.Platform
        };

        await _fcmNotificationService.RegisterDeviceTokenAsync(userId, dto);
        _logger.LogInformation("gRPC: Device token registered for user {UserId}, platform {Platform}", userId, request.Platform);

        return new GrpcEmpty();
    }

    public override Task<GrpcEmpty> UnregisterDeviceToken(GrpcUnregisterDeviceTokenRequest request, ServerCallContext context)
    {
        var userId = GetUserId(context);
        _logger.LogInformation("gRPC: Device token unregistered for user {UserId}, deviceId {DeviceId}", userId, request.DeviceId);

        // Note: Implement unregister in IFcmNotificationService if needed
        return Task.FromResult(new GrpcEmpty());
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
