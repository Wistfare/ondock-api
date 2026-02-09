using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ondock.api.Configuration;
using ondock.api.Exceptions;
using ondock.api.Services.Interfaces;

namespace ondock.api.Controllers;

[ApiController]
[Route("api/admin/chat-settings")]
[Produces("application/json")]
public class AdminChatSettingsController : ControllerBase
{
    private readonly IChatSettingsProvider _provider;
    private readonly AdminSettings _adminSettings;

    public AdminChatSettingsController(IChatSettingsProvider provider, IOptions<AdminSettings> adminSettings)
    {
        _provider = provider;
        _adminSettings = adminSettings.Value;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ChatSettings), StatusCodes.Status200OK)]
    public ActionResult<ChatSettings> Get()
    {
        RequireAdmin();
        return Ok(_provider.Get());
    }

    [HttpPut]
    [ProducesResponseType(typeof(ChatSettings), StatusCodes.Status200OK)]
    public ActionResult<ChatSettings> Update([FromBody] ChatSettings settings)
    {
        RequireAdmin();

        // Basic sanitization
        settings.RestrictedColors ??= new List<string>();
        settings.RandomColorPalette ??= new List<string>();

        _provider.Update(settings);
        return Ok(_provider.Get());
    }

    private void RequireAdmin()
    {
        var key = Request.Headers["X-Admin-Key"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(_adminSettings.ApiKey))
        {
            throw new NotImplementedException("Admin API key not configured");
        }

        if (string.IsNullOrWhiteSpace(key) || !string.Equals(key, _adminSettings.ApiKey, StringComparison.Ordinal))
        {
            throw new ForbiddenException("Invalid admin key");
        }
    }
}
