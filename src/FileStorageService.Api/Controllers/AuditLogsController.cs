using FileStorageService.Application.Dtos;
using FileStorageService.Application.Interfaces;
using FileStorageService.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FileStorageService.Api.Controllers;

/// <summary>
/// Admin-only audit log search.
/// </summary>
[ApiController]
[Authorize(Policy = "AdminOnly")]
[Route("api/audit-logs")]
public sealed class AuditLogsController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;

    public AuditLogsController(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    /// <summary>
    /// Lists audit events with pagination and optional filters.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<AuditLogResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResponse<AuditLogResponse>>> SearchAsync(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? fileId = null,
        [FromQuery] AuditAction? action = null,
        [FromQuery] string? userId = null,
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null,
        CancellationToken cancellationToken = default)
    {
        var request = new AuditLogSearchRequest(
            pageNumber,
            pageSize,
            fileId,
            action,
            userId,
            fromUtc,
            toUtc);

        var response = await _auditLogService.SearchAsync(request, cancellationToken);

        return Ok(response);
    }
}
