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
        var storageHealthy = await CanReadAndWriteStorageAsync(cancellationToken);
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

    private async Task<bool> CanReadAndWriteStorageAsync(CancellationToken cancellationToken)
    {
        var storageRootPath = _configuration["Storage:RootPath"] ?? "_storage";
        var rootPath = Path.GetFullPath(storageRootPath);
        var testFilePath = Path.Combine(rootPath, $".health-{Guid.NewGuid():N}.tmp");

        try
        {
            Directory.CreateDirectory(rootPath);
            await System.IO.File.WriteAllTextAsync(testFilePath, "health-check", cancellationToken);
            var content = await System.IO.File.ReadAllTextAsync(testFilePath, cancellationToken);

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
