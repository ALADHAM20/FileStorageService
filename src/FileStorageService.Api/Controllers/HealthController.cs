using FileStorageService.Infrastructure.DbContext;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FileStorageService.Api.Controllers;

/// <summary>
/// Operational health endpoints.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("health")]
public sealed class HealthController : ControllerBase
{
    private readonly FileStorageDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<HealthController> _logger;

    public HealthController(
        FileStorageDbContext dbContext,
        IConfiguration configuration,
        ILogger<HealthController> logger)
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Reports whether the API process is running.
    /// </summary>
    [HttpGet("live")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Live()
    {
        return Ok(new
        {
            status = "Healthy",
            service = "FileStorageService.Api",
            checkedAtUtc = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Reports whether required dependencies are reachable.
    /// </summary>
    [HttpGet("ready")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Ready(CancellationToken cancellationToken)
    {
        var databaseHealthy = await CanConnectToDatabaseAsync(cancellationToken);
        var storageHealthy = CanReadAndWriteStorage();
        var isHealthy = databaseHealthy && storageHealthy;

        var response = new
        {
            status = isHealthy ? "Healthy" : "Unhealthy",
            checkedAtUtc = DateTime.UtcNow,
            checks = new
            {
                database = databaseHealthy ? "Healthy" : "Unhealthy",
                storage = storageHealthy ? "Healthy" : "Unhealthy"
            }
        };

        return isHealthy
            ? Ok(response)
            : StatusCode(StatusCodes.Status503ServiceUnavailable, response);
    }

    private async Task<bool> CanConnectToDatabaseAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await _dbContext.Database.CanConnectAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Database health check failed.");
            return false;
        }
    }

    private bool CanReadAndWriteStorage()
    {
        var storageRootPath = _configuration["Storage:RootPath"] ?? "_storage";
        var rootPath = Path.GetFullPath(storageRootPath);
        var testFilePath = Path.Combine(rootPath, $".health-{Guid.NewGuid():N}.tmp");

        try
        {
            Directory.CreateDirectory(rootPath);
            System.IO.File.WriteAllText(testFilePath, "health-check");
            var content = System.IO.File.ReadAllText(testFilePath);

            return content == "health-check";
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Filesystem health check failed.");
            return false;
        }
        finally
        {
            if (System.IO.File.Exists(testFilePath))
            {
                System.IO.File.Delete(testFilePath);
            }
        }
    }
}
