using System.Security.Claims;
using FileStorageService.Application.Dtos;
using FileStorageService.Application.Interfaces;
using FileStorageService.Api.Filters;
using FileStorageService.Api.Options;
using FileStorageService.Api.Responses;
using FileStorageService.Api.Services;
using FileStorageService.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FileStorageService.Api.Controllers;

/// <summary>
/// File metadata and content operations.
/// </summary>
[ApiController]
[Authorize]
[Route("api/files")]
public sealed class FilesController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;
    private readonly IFileDeleteService _fileDeleteService;
    private readonly IFileDownloadService _fileDownloadService;
    private readonly IFilePreviewService _filePreviewService;
    private readonly IFileQueryService _fileQueryService;
    private readonly IFileUploadService _fileUploadService;
    private readonly MultipartUploadRequestReader _multipartUploadRequestReader;
    private readonly UploadOptions _uploadOptions;

    public FilesController(
        IAuditLogService auditLogService,
        IFileDeleteService fileDeleteService,
        IFileDownloadService fileDownloadService,
        IFilePreviewService filePreviewService,
        IFileQueryService fileQueryService,
        IFileUploadService fileUploadService,
        MultipartUploadRequestReader multipartUploadRequestReader,
        IOptions<UploadOptions> uploadOptions)
    {
        _auditLogService = auditLogService;
        _fileDeleteService = fileDeleteService;
        _fileDownloadService = fileDownloadService;
        _filePreviewService = filePreviewService;
        _fileQueryService = fileQueryService;
        _fileUploadService = fileUploadService;
        _multipartUploadRequestReader = multipartUploadRequestReader;
        _uploadOptions = uploadOptions.Value;
    }

    /// <summary>
    /// Lists stored file metadata with pagination and optional filters.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<StoredFileResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<StoredFileResponse>>> SearchAsync(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? name = null,
        [FromQuery] string? tag = null,
        [FromQuery] string? contentType = null,
        [FromQuery] DateTime? createdFromUtc = null,
        [FromQuery] DateTime? createdToUtc = null,
        CancellationToken cancellationToken = default)
    {
        var request = new FileSearchRequest(
            pageNumber,
            pageSize,
            name,
            tag,
            contentType,
            createdFromUtc,
            createdToUtc);

        var response = await _fileQueryService.SearchAsync(request, cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Uploads one file using streaming multipart/form-data.
    /// </summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [DisableFormValueModelBinding]
    [ProducesResponseType(typeof(StoredFileResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status415UnsupportedMediaType)]
    public async Task<ActionResult<StoredFileResponse>> UploadAsync(
        CancellationToken cancellationToken)
    {
        var readResult = await _multipartUploadRequestReader.ReadAsync(
            Request,
            _uploadOptions,
            cancellationToken);

        if (!readResult.IsSuccess)
        {
            return Problem(
                title: readResult.ErrorTitle,
                detail: readResult.ErrorDetail,
                statusCode: readResult.StatusCode);
        }

        var file = readResult.Request!;
        var uploadRequest = new UploadFileRequest(
            file.Content,
            file.OriginalName,
            file.ContentType,
            file.Tags,
            GetCurrentUserId());

        var response = await _fileUploadService.UploadAsync(uploadRequest, cancellationToken);
        await CreateAuditLogAsync(
            response.Id,
            AuditAction.Upload,
            $"Uploaded file '{response.OriginalName}'.",
            cancellationToken);

        return Created($"/api/files/{response.Id}", response);
    }

    /// <summary>
    /// Downloads a stored file as an attachment with HTTP Range support.
    /// </summary>
    [HttpGet("{id:guid}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status206PartialContent)]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var response = await _fileDownloadService.GetDownloadAsync(id, cancellationToken);

        if (response is null)
        {
            return NotFound();
        }

        if (RequestETagMatches(response.ETag))
        {
            response.Content.Dispose();
            Response.Headers.ETag = response.ETag;

            return StatusCode(StatusCodes.Status304NotModified);
        }

        await CreateAuditLogAsync(
            id,
            AuditAction.Download,
            $"Downloaded file '{response.OriginalName}'.",
            cancellationToken);

        Response.Headers.ETag = response.ETag;
        Response.Headers.CacheControl = "private, max-age=0";

        return new FileStreamResult(response.Content, response.ContentType)
        {
            FileDownloadName = response.OriginalName,
            EnableRangeProcessing = true
        };
    }

    /// <summary>
    /// Streams an inline preview for PDFs and images.
    /// </summary>
    [HttpGet("{id:guid}/preview")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status206PartialContent)]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PreviewAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var response = await _filePreviewService.GetPreviewAsync(id, cancellationToken);

        if (response is null)
        {
            return NotFound();
        }

        if (RequestETagMatches(response.ETag))
        {
            response.Content.Dispose();
            Response.Headers.ETag = response.ETag;

            return StatusCode(StatusCodes.Status304NotModified);
        }

        await CreateAuditLogAsync(
            id,
            AuditAction.Preview,
            "Previewed file content.",
            cancellationToken);

        Response.Headers.ETag = response.ETag;
        Response.Headers.CacheControl = "private, max-age=0";

        return new FileStreamResult(response.Content, response.ContentType)
        {
            EnableRangeProcessing = true
        };
    }

    /// <summary>
    /// Soft deletes file metadata while preserving physical content.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SoftDeleteAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var deleted = await _fileDeleteService.SoftDeleteAsync(id, cancellationToken);

        if (deleted)
        {
            await CreateAuditLogAsync(
                id,
                AuditAction.SoftDelete,
                "Soft deleted file.",
                cancellationToken);
        }

        return deleted
            ? Ok(new ApiResponse(
                StatusCodes.Status200OK,
                "File was soft deleted successfully."))
            : NotFound(new ApiResponse(
                StatusCodes.Status404NotFound,
                "File was not found."));
    }

    /// <summary>
    /// Permanently removes file metadata and physical content. Admin role required.
    /// </summary>
    [HttpDelete("{id:guid}/hard")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> HardDeleteAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var deleted = await _fileDeleteService.HardDeleteAsync(id, cancellationToken);

        if (deleted)
        {
            await CreateAuditLogAsync(
                id,
                AuditAction.HardDelete,
                "Permanently deleted file.",
                cancellationToken);
        }

        return deleted
            ? Ok(new ApiResponse(
                StatusCodes.Status200OK,
                "File was permanently deleted successfully."))
            : NotFound(new ApiResponse(
                StatusCodes.Status404NotFound,
                "File was not found."));
    }

    private bool RequestETagMatches(string eTag)
    {
        var ifNoneMatch = Request.Headers.IfNoneMatch.ToString();

        if (string.IsNullOrWhiteSpace(ifNoneMatch))
        {
            return false;
        }

        return ifNoneMatch
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(value => value == "*" || value.Equals(eTag, StringComparison.Ordinal));
    }

    private string GetCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new InvalidOperationException("User id claim is missing.");
    }

    private async Task CreateAuditLogAsync(
        Guid? fileId,
        AuditAction action,
        string details,
        CancellationToken cancellationToken)
    {
        var request = new CreateAuditLogRequest(
            fileId,
            action,
            GetCurrentUserId(),
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            HttpContext.TraceIdentifier,
            details);

        await _auditLogService.CreateAsync(request, cancellationToken);
    }
}
