using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ondock.api.DTOs.Post;
using ondock.api.Services.Interfaces;
using System.Security.Claims;

namespace ondock.api.Controllers;

[ApiController]
[Route("api/posts")]
[Authorize]
public class PostController : ControllerBase
{
    private readonly IPostService _postService;
    private readonly ILogger<PostController> _logger;

    public PostController(IPostService postService, ILogger<PostController> logger)
    {
        _postService = postService;
        _logger = logger;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>
    /// Create a new post (video/photo/text)
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<PostDto>> CreatePost([FromBody] CreatePostDto dto)
    {
        try
        {
            var userId = GetUserId();
            var result = await _postService.CreatePostAsync(userId, dto);
            return CreatedAtAction(nameof(GetPost), new { id = result.PostId }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating road post");
            return StatusCode(500, "Failed to create post");
        }
    }

    /// <summary>
    /// Get a specific post by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PostDto>> GetPost(Guid id)
    {
        var result = await _postService.GetPostAsync(id);
        
        if (result == null)
            return NotFound();

        // Increment view count
        await _postService.IncrementViewCountAsync(id);

        return Ok(result);
    }

    /// <summary>
    /// Get current user's posts
    /// </summary>
    [HttpGet("mine")]
    public async Task<ActionResult<List<PostDto>>> GetMyPosts()
    {
        var userId = GetUserId();
        var result = await _postService.GetUserPostsAsync(userId);
        return Ok(result);
    }

    /// <summary>
    /// Delete own post
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeletePost(Guid id)
    {
        var userId = GetUserId();
        var success = await _postService.DeletePostAsync(id, userId);
        
        if (!success)
            return NotFound();

        return NoContent();
    }

    /// <summary>
    /// Report a post
    /// </summary>
    [HttpPost("{id:guid}/report")]
    public async Task<IActionResult> ReportPost(Guid id)
    {
        var userId = GetUserId();
        var success = await _postService.ReportPostAsync(id, userId);
        
        if (!success)
            return NotFound();

        return NoContent();
    }
}
