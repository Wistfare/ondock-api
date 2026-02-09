using System.Security.Claims;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using ondock.api.Services.Interfaces;

// Aliases to resolve ambiguity
using GrpcEmpty = Ondock.Api.Grpc.V1.Empty;
using GrpcPermanentContact = Ondock.Api.Grpc.V1.PermanentContact;
using Ondock.Api.Grpc.V1;

namespace ondock.api.Grpc.Services;

[Authorize]
public class ContactsGrpcService : ContactsService.ContactsServiceBase
{
    private readonly IContactService _contactService;
    private readonly ILogger<ContactsGrpcService> _logger;

    public ContactsGrpcService(IContactService contactService, ILogger<ContactsGrpcService> logger)
    {
        _contactService = contactService;
        _logger = logger;
    }

    public override async Task<GenerateQrCodeResponse> GenerateQrCode(GrpcEmpty request, ServerCallContext context)
    {
        var userId = GetUserId(context);

        var result = await _contactService.GenerateQrCodeAsync(userId);

        return new GenerateQrCodeResponse
        {
            QrData = result.QrData,
            ExpiresAt = Timestamp.FromDateTime(result.ExpiresAt.ToUniversalTime())
        };
    }

    public override async Task<ScanQrCodeResponse> ScanQrCode(ScanQrCodeRequest request, ServerCallContext context)
    {
        var userId = GetUserId(context);

        var result = await _contactService.ScanQrCodeAsync(userId, request.QrData);

        return new ScanQrCodeResponse
        {
            Success = result.Success,
            Message = result.Message ?? "",
            ContactId = result.ContactId?.ToString() ?? "",
            ContactDisplayName = result.ContactDisplayName ?? ""
        };
    }

    public override async Task<GetPermanentContactsResponse> GetPermanentContacts(GrpcEmpty request, ServerCallContext context)
    {
        var userId = GetUserId(context);

        var result = await _contactService.GetPermanentContactsAsync(userId);

        var response = new GetPermanentContactsResponse();
        foreach (var contact in result.Contacts)
        {
            response.Contacts.Add(new GrpcPermanentContact
            {
                ContactId = contact.ContactId,
                UserId = contact.UserId.ToString(),
                DisplayName = contact.DisplayName ?? "",
                ColorName = "", // Not in DTO
                AddedAt = Timestamp.FromDateTime(contact.MatchedAt.ToUniversalTime())
            });
        }

        return response;
    }

    public override async Task<IsPermanentContactResponse> IsPermanentContact(IsPermanentContactRequest request, ServerCallContext context)
    {
        var userId = GetUserId(context);

        var result = await _contactService.IsPermanentContactAsync(userId, request.AnonymousUserId);

        return new IsPermanentContactResponse
        {
            IsPermanentContact = result
        };
    }

    public override async Task<GrpcEmpty> RemovePermanentContact(RemovePermanentContactRequest request, ServerCallContext context)
    {
        var userId = GetUserId(context);

        if (!Guid.TryParse(request.ContactId, out var contactId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid contact ID"));
        }

        await _contactService.RemovePermanentContactAsync(userId, contactId);

        return new GrpcEmpty();
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
