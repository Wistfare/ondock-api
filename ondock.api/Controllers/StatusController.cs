using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ondock.api.Services.Interfaces;

namespace ondock.api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StatusController : ControllerBase
{
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<StatusController> _logger;

    public StatusController(
        IFileStorageService fileStorageService,
        ILogger<StatusController> logger)
    {
        _fileStorageService = fileStorageService;
        _logger = logger;
    }

    /// <summary>
    /// Upload media (image or video) for a status post
    /// </summary>
    [HttpPost("media")]
    [ProducesResponseType(typeof(StatusMediaUploadResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(50 * 1024 * 1024)] // 50MB limit
    public async Task<ActionResult<StatusMediaUploadResponse>> UploadMedia([FromForm] StatusMediaUploadRequest request)
    {
        var userId = GetUserId();

        if (request.File == null || request.File.Length == 0)
        {
            return BadRequest(new { error = "No file provided" });
        }

        var contentType = request.File.ContentType.ToLowerInvariant();
        string folder;
        string mediaType;

        if (contentType.StartsWith("image/"))
        {
            folder = "images";
            mediaType = "image";
        }
        else if (contentType.StartsWith("video/"))
        {
            folder = "videos";
            mediaType = "video";
        }
        else
        {
            return BadRequest(new { error = "Only image and video files are supported for status posts" });
        }

        try
        {
            // Upload main media file
            await using var stream = request.File.OpenReadStream();
            var mediaUrl = await _fileStorageService.UploadAsync(
                stream,
                request.File.FileName,
                request.File.ContentType,
                $"status/{userId}/{folder}"
            );

            // Upload thumbnail if provided (for videos)
            string? thumbnailUrl = null;
            if (request.Thumbnail != null && request.Thumbnail.Length > 0)
            {
                await using var thumbStream = request.Thumbnail.OpenReadStream();
                thumbnailUrl = await _fileStorageService.UploadAsync(
                    thumbStream,
                    request.Thumbnail.FileName,
                    request.Thumbnail.ContentType,
                    $"status/{userId}/thumbnails"
                );
            }

            _logger.LogInformation("Status media uploaded for user {UserId}: {MediaUrl}", userId, mediaUrl);

            return CreatedAtAction(nameof(UploadMedia), new StatusMediaUploadResponse
            {
                MediaUrl = mediaUrl,
                ThumbnailUrl = thumbnailUrl,
                MediaType = mediaType,
                FileName = request.File.FileName,
                FileSize = request.File.Length,
                DurationSeconds = request.DurationSeconds
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload status media for user {UserId}", userId);
            return StatusCode(500, new { error = "Failed to upload media" });
        }
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid authentication token");
        }
        return userId;
    }
}

public class StatusMediaUploadRequest
{
    [Required]
    public IFormFile File { get; set; } = null!;
    
    public IFormFile? Thumbnail { get; set; }
    
    public int? DurationSeconds { get; set; }
}

public class StatusMediaUploadResponse
{
    public string MediaUrl { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public string MediaType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public int? DurationSeconds { get; set; }
}
