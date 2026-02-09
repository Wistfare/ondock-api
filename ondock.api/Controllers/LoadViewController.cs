using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ondock.api.DTOs.LoadView;
using ondock.api.Services.Interfaces;
using System.Security.Claims;

namespace ondock.api.Controllers;

[ApiController]
[Route("api/loadview")]
[Authorize]
public class LoadViewController : ControllerBase
{
    private readonly ILoadViewService _loadViewService;
    private readonly ILogger<LoadViewController> _logger;

    public LoadViewController(ILoadViewService loadViewService, ILogger<LoadViewController> logger)
    {
        _loadViewService = loadViewService;
        _logger = logger;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>
    /// Create a new LoadView request
    /// </summary>
    [HttpPost("requests")]
    public async Task<ActionResult<LoadViewRequestDto>> CreateRequest([FromBody] CreateLoadViewRequestDto dto)
    {
        try
        {
            var userId = GetUserId();
            var result = await _loadViewService.CreateRequestAsync(userId, dto);
            return CreatedAtAction(nameof(GetRequest), new { id = result.RequestId }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating LoadView request");
            return StatusCode(500, "Failed to create request");
        }
    }

    /// <summary>
    /// Get a specific LoadView request by ID
    /// </summary>
    [HttpGet("requests/{id:guid}")]
    public async Task<ActionResult<LoadViewRequestDto>> GetRequest(Guid id)
    {
        var userId = GetUserId();
        var result = await _loadViewService.GetRequestAsync(id, userId);
        
        if (result == null)
            return NotFound();

        return Ok(result);
    }

    /// <summary>
    /// Get current user's LoadView requests
    /// </summary>
    [HttpGet("requests")]
    public async Task<ActionResult<List<LoadViewRequestDto>>> GetUserRequests()
    {
        var userId = GetUserId();
        var result = await _loadViewService.GetUserRequestsAsync(userId);
        return Ok(result);
    }

    /// <summary>
    /// Cancel a LoadView request
    /// </summary>
    [HttpDelete("requests/{id:guid}")]
    public async Task<IActionResult> CancelRequest(Guid id)
    {
        var userId = GetUserId();
        var success = await _loadViewService.CancelRequestAsync(id, userId);
        
        if (!success)
            return NotFound();

        return NoContent();
    }

    /// <summary>
    /// Submit a video response to a LoadView request
    /// </summary>
    [HttpPost("requests/{id:guid}/respond")]
    public async Task<ActionResult<LoadViewResponseDto>> SubmitResponse(Guid id, [FromBody] SubmitLoadViewResponseDto dto)
    {
        try
        {
            var userId = GetUserId();
            var result = await _loadViewService.SubmitResponseAsync(id, userId, dto);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting LoadView response");
            return StatusCode(500, "Failed to submit response");
        }
    }

    /// <summary>
    /// Start a live stream response to a LoadView request
    /// </summary>
    [HttpPost("requests/{id:guid}/live")]
    public async Task<ActionResult<StartLiveResponseDto>> StartLiveResponse(Guid id)
    {
        try
        {
            var userId = GetUserId();
            var result = await _loadViewService.StartLiveResponseAsync(id, userId);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting live LoadView response");
            return StatusCode(500, "Failed to start live response");
        }
    }

    /// <summary>
    /// End a live stream response
    /// </summary>
    [HttpPost("responses/{id:guid}/end-live")]
    public async Task<IActionResult> EndLiveResponse(Guid id, [FromQuery] string? recordingUrl = null)
    {
        var userId = GetUserId();
        var success = await _loadViewService.EndLiveResponseAsync(id, userId, recordingUrl);
        
        if (!success)
            return NotFound();

        return NoContent();
    }

    /// <summary>
    /// Get all responses for a LoadView request
    /// </summary>
    [HttpGet("requests/{id:guid}/responses")]
    public async Task<ActionResult<List<LoadViewResponseDto>>> GetResponses(Guid id)
    {
        var result = await _loadViewService.GetResponsesAsync(id);
        return Ok(result);
    }

    /// <summary>
    /// Mark a response as helpful or not helpful
    /// </summary>
    [HttpPost("responses/{id:guid}/feedback")]
    public async Task<IActionResult> MarkResponseHelpful(Guid id, [FromQuery] bool wasHelpful)
    {
        var userId = GetUserId();
        var success = await _loadViewService.MarkResponseHelpfulAsync(id, userId, wasHelpful);
        
        if (!success)
            return NotFound();

        return NoContent();
    }

    /// <summary>
    /// Delete own response
    /// </summary>
    [HttpDelete("responses/{id:guid}")]
    public async Task<IActionResult> DeleteResponse(Guid id)
    {
        var userId = GetUserId();
        var success = await _loadViewService.DeleteResponseAsync(id, userId);
        
        if (!success)
            return NotFound();

        return NoContent();
    }
}
