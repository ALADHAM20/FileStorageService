using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FileStorageService.Api.Controllers;

/// <summary>
/// Basic API status endpoint.
/// </summary>
[ApiController]
[AllowAnonymous]
public sealed class StatusController : ControllerBase
{
    /// <summary>
    /// Confirms the API is reachable.
    /// </summary>
    [HttpGet("/")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Get()
    {
        return Ok(new { Service = "FileStorageService.Api", Status = "Ready" });
    }
}
