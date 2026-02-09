using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ondock.api.DTOs.Chat;
using ondock.api.Services.Interfaces;

namespace ondock.api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Obsolete("Use gRPC EncryptionService instead. This REST controller is deprecated.")]
public class EncryptionController : ControllerBase
{
    private readonly IEncryptionService _encryptionService;

    public EncryptionController(IEncryptionService encryptionService)
    {
        _encryptionService = encryptionService;
    }

    [HttpPost("keys")]
    [ProducesResponseType(typeof(PublicKeyResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<PublicKeyResponse>> RegisterKey([FromBody] RegisterKeyRequest request)
    {
        var userId = GetUserId();
        var response = await _encryptionService.RegisterKeyAsync(userId, request);
        return Ok(response);
    }

    [HttpGet("keys/{userId:guid}")]
    [ProducesResponseType(typeof(PublicKeyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PublicKeyResponse>> GetPublicKey(Guid userId)
    {
        var key = await _encryptionService.GetPublicKeyAsync(userId);
        if (key == null)
        {
            return NotFound();
        }
        return Ok(key);
    }

    [HttpGet("keys/me")]
    [ProducesResponseType(typeof(PublicKeyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PublicKeyResponse>> GetMyPublicKey()
    {
        var userId = GetUserId();
        var key = await _encryptionService.GetPublicKeyAsync(userId);
        if (key == null)
        {
            return NotFound();
        }
        return Ok(key);
    }

    [HttpPost("exchange")]
    [ProducesResponseType(typeof(KeyExchangeResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<KeyExchangeResponse>> InitiateKeyExchange([FromBody] KeyExchangeRequest request)
    {
        var userId = GetUserId();
        var response = await _encryptionService.InitiateKeyExchangeAsync(userId, request);
        return Ok(response);
    }

    [HttpGet("exchange/{chatId}")]
    [ProducesResponseType(typeof(IReadOnlyList<KeyExchangeResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<KeyExchangeResponse>>> GetPendingKeyExchanges(string chatId)
    {
        var userId = GetUserId();
        var exchanges = await _encryptionService.GetPendingKeyExchangesAsync(userId, chatId);
        return Ok(exchanges);
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user token");
        }
        return userId;
    }
}
