using System.Security.Claims;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using ondock.api.Services.Interfaces;

using GrpcEmpty = Ondock.Api.Grpc.V1.Empty;
using GrpcRequestRevealRequest = Ondock.Api.Grpc.V1.RequestRevealRequest;
using GrpcAcceptRevealRequest = Ondock.Api.Grpc.V1.AcceptRevealRequest;
using GrpcDeclineRevealRequest = Ondock.Api.Grpc.V1.DeclineRevealRequest;
using GrpcGetRevealStatusRequest = Ondock.Api.Grpc.V1.GetRevealStatusRequest;
using GrpcIdentityRevealResponse = Ondock.Api.Grpc.V1.IdentityRevealResponse;
using GrpcPendingRevealRequestsResponse = Ondock.Api.Grpc.V1.PendingRevealRequestsResponse;

using DtoIdentityRevealRequest = ondock.api.DTOs.Chat.IdentityRevealRequest;

namespace ondock.api.Grpc.Services;

[Authorize]
public class IdentityGrpcService : Ondock.Api.Grpc.V1.IdentityService.IdentityServiceBase
{
    private readonly IIdentityService _identityService;
    private readonly ILogger<IdentityGrpcService> _logger;

    public IdentityGrpcService(
        IIdentityService identityService,
        ILogger<IdentityGrpcService> logger)
    {
        _identityService = identityService;
        _logger = logger;
    }

    public override async Task<GrpcIdentityRevealResponse> RequestReveal(GrpcRequestRevealRequest request, ServerCallContext context)
    {
        var userId = GetUserId(context);

        var dto = new DtoIdentityRevealRequest
        {
            ChatId = request.ChatId,
            TargetUserId = Guid.TryParse(request.TargetAnonymousUserId, out var targetId) ? targetId : Guid.Empty
        };

        var result = await _identityService.RequestIdentityRevealAsync(userId, dto);

        return MapRevealResponse(result);
    }

    public override async Task<GrpcIdentityRevealResponse> AcceptReveal(GrpcAcceptRevealRequest request, ServerCallContext context)
    {
        var userId = GetUserId(context);

        if (!Guid.TryParse(request.RequesterId, out var requesterId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid requester ID"));
        }

        var result = await _identityService.AcceptIdentityRevealAsync(userId, request.ChatId, requesterId);

        return MapRevealResponse(result);
    }

    public override Task<GrpcEmpty> DeclineReveal(GrpcDeclineRevealRequest request, ServerCallContext context)
    {
        // DeclineIdentityRevealAsync is not in the interface
        // This is a stub - decline functionality may need to be added to IIdentityService
        _logger.LogWarning("DeclineReveal called but not implemented in IIdentityService");
        return Task.FromResult(new GrpcEmpty());
    }

    public override async Task<GrpcIdentityRevealResponse> GetRevealStatus(GrpcGetRevealStatusRequest request, ServerCallContext context)
    {
        var userId = GetUserId(context);

        if (!Guid.TryParse(request.OtherUserId, out var otherUserId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid other user ID"));
        }

        var result = await _identityService.GetIdentityRevealStatusAsync(userId, request.ChatId, otherUserId);

        if (result == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Identity reveal status not found"));
        }

        return MapRevealResponse(result);
    }

    public override async Task<GrpcPendingRevealRequestsResponse> GetPendingRequests(GrpcEmpty request, ServerCallContext context)
    {
        var userId = GetUserId(context);

        var results = await _identityService.GetPendingRevealRequestsAsync(userId);

        var response = new GrpcPendingRevealRequestsResponse();
        foreach (var result in results)
        {
            response.Requests.Add(MapRevealResponse(result));
        }

        return response;
    }

    private static GrpcIdentityRevealResponse MapRevealResponse(ondock.api.DTOs.Chat.IdentityRevealResponse result)
    {
        // Determine status based on reveal flags
        string status = "pending";
        if (result.BothRevealed) status = "accepted";
        else if (result.RequesterRevealed || result.TargetRevealed) status = "partial";

        var response = new GrpcIdentityRevealResponse
        {
            ChatId = result.ChatId ?? "",
            Status = status,
            RequesterId = result.RequesterId.ToString(),
            TargetId = result.TargetUserId.ToString(),
            RevealedFirstName = result.TargetIdentity?.DisplayName?.Split(' ').FirstOrDefault() ?? "",
            RevealedLastName = result.TargetIdentity?.DisplayName?.Split(' ').Skip(1).FirstOrDefault() ?? "",
            RevealedEmail = result.TargetIdentity?.Email ?? "",
            RevealedPhone = result.TargetIdentity?.PhoneNumber ?? "",
            RequestedAt = Timestamp.FromDateTime(result.RequestedAt.ToUniversalTime())
        };

        return response;
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
