using System.Security.Claims;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using ondock.api.Services.Interfaces;

using GrpcEmpty = Ondock.Api.Grpc.V1.Empty;
using GrpcRegisterKeyRequest = Ondock.Api.Grpc.V1.RegisterKeyRequest;
using GrpcGetPublicKeyRequest = Ondock.Api.Grpc.V1.GetPublicKeyRequest;
using GrpcPublicKeyResponse = Ondock.Api.Grpc.V1.PublicKeyResponse;
using GrpcInitiateKeyExchangeRequest = Ondock.Api.Grpc.V1.InitiateKeyExchangeRequest;
using GrpcGetPendingKeyExchangesRequest = Ondock.Api.Grpc.V1.GetPendingKeyExchangesRequest;
using GrpcKeyExchangeResponse = Ondock.Api.Grpc.V1.KeyExchangeResponse;
using GrpcPendingKeyExchangesResponse = Ondock.Api.Grpc.V1.PendingKeyExchangesResponse;

using DtoRegisterKeyRequest = ondock.api.DTOs.Chat.RegisterKeyRequest;
using DtoKeyExchangeRequest = ondock.api.DTOs.Chat.KeyExchangeRequest;

namespace ondock.api.Grpc.Services;

[Authorize]
public class EncryptionGrpcService : Ondock.Api.Grpc.V1.EncryptionService.EncryptionServiceBase
{
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<EncryptionGrpcService> _logger;

    public EncryptionGrpcService(
        IEncryptionService encryptionService,
        ILogger<EncryptionGrpcService> logger)
    {
        _encryptionService = encryptionService;
        _logger = logger;
    }

    public override async Task<GrpcPublicKeyResponse> RegisterKey(GrpcRegisterKeyRequest request, ServerCallContext context)
    {
        var userId = GetUserId(context);

        var dto = new DtoRegisterKeyRequest
        {
            PublicKey = request.PublicKey,
            KeyType = request.KeyType
        };

        var result = await _encryptionService.RegisterKeyAsync(userId, dto);

        return MapPublicKeyResponse(result);
    }

    public override async Task<GrpcPublicKeyResponse> GetPublicKey(GrpcGetPublicKeyRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.UserId, out var targetUserId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid user ID"));
        }

        var result = await _encryptionService.GetPublicKeyAsync(targetUserId);

        if (result == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Public key not found"));
        }

        return MapPublicKeyResponse(result);
    }

    public override async Task<GrpcPublicKeyResponse> GetMyPublicKey(GrpcEmpty request, ServerCallContext context)
    {
        var userId = GetUserId(context);

        var result = await _encryptionService.GetPublicKeyAsync(userId);

        if (result == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Public key not found"));
        }

        return MapPublicKeyResponse(result);
    }

    public override async Task<GrpcKeyExchangeResponse> InitiateKeyExchange(GrpcInitiateKeyExchangeRequest request, ServerCallContext context)
    {
        var userId = GetUserId(context);

        var dto = new DtoKeyExchangeRequest
        {
            ChatId = request.ChatId,
            ToUserId = Guid.Parse(request.TargetUserId),
            EncryptedSessionKey = request.EphemeralPublicKey
        };

        var result = await _encryptionService.InitiateKeyExchangeAsync(userId, dto);

        return MapKeyExchangeResponse(result);
    }

    public override async Task<GrpcPendingKeyExchangesResponse> GetPendingKeyExchanges(GrpcGetPendingKeyExchangesRequest request, ServerCallContext context)
    {
        var userId = GetUserId(context);

        var results = await _encryptionService.GetPendingKeyExchangesAsync(userId, request.ChatId);

        var response = new GrpcPendingKeyExchangesResponse();
        foreach (var result in results)
        {
            response.Exchanges.Add(MapKeyExchangeResponse(result));
        }

        return response;
    }

    private static GrpcPublicKeyResponse MapPublicKeyResponse(ondock.api.DTOs.Chat.PublicKeyResponse result)
    {
        var response = new GrpcPublicKeyResponse
        {
            UserId = result.UserId.ToString(),
            PublicKey = result.PublicKey ?? "",
            KeyType = result.KeyType ?? "",
            CreatedAt = Timestamp.FromDateTime(result.CreatedAt.ToUniversalTime())
        };

        return response;
    }

    private static GrpcKeyExchangeResponse MapKeyExchangeResponse(ondock.api.DTOs.Chat.KeyExchangeResponse result)
    {
        var response = new GrpcKeyExchangeResponse
        {
            ExchangeId = Guid.NewGuid().ToString(), // No ExchangeId in DTO, generate one
            ChatId = result.ChatId ?? "",
            InitiatorId = result.InitiatorUserId.ToString(),
            TargetId = result.TargetUserId.ToString(),
            EphemeralPublicKey = result.EncryptedKey ?? "",
            Status = "completed",
            CreatedAt = Timestamp.FromDateTime(result.CreatedAt.ToUniversalTime())
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
