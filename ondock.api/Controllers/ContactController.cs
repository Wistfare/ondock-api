using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ondock.api.DTOs.Contact;
using ondock.api.Services.Interfaces;
using System.Security.Claims;

namespace ondock.api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Obsolete("Use gRPC ContactsService instead. This REST controller is deprecated.")]
public class ContactController : ControllerBase
{
    private readonly IContactService _contactService;

    public ContactController(IContactService contactService)
    {
        _contactService = contactService;
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }

    [HttpPost("generate-qr")]
    [ProducesResponseType(typeof(GenerateQRCodeResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<GenerateQRCodeResponse>> GenerateQRCode([FromQuery] Guid? chatId = null)
    {
        var userId = GetUserId();
        var result = await _contactService.GenerateQRCodeAsync(userId, chatId);
        return Ok(result);
    }

    [HttpPost("scan-qr")]
    [ProducesResponseType(typeof(ScanQRCodeResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ScanQRCodeResponse>> ScanQRCode([FromBody] ScanQRCodeRequest request)
    {
        var userId = GetUserId();
        var result = await _contactService.ScanQRCodeAsync(userId, request);
        return Ok(result);
    }

    [HttpGet("permanent")]
    [ProducesResponseType(typeof(PermanentContactsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<PermanentContactsResponse>> GetPermanentContacts()
    {
        var userId = GetUserId();
        var result = await _contactService.GetPermanentContactsAsync(userId);
        return Ok(result);
    }

    [HttpGet("is-permanent/{otherUserId}")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    public async Task<ActionResult<bool>> IsPermanentContact(Guid otherUserId)
    {
        var userId = GetUserId();
        var result = await _contactService.IsPermanentContactAsync(userId, otherUserId);
        return Ok(result);
    }
}
