using FileStorageService.Application.Dtos;
using FileStorageService.Application.Interfaces;
using FileStorageService.Api.Options;
using FileStorageService.Api.Requests;
using FileStorageService.Api.Services;
using FileStorageService.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FileStorageService.Api.Controllers;

/// <summary>
/// Resumable upload session operations.
/// </summary>
[ApiController]
[Authorize]
[Route("api/upload-sessions")]
public sealed class UploadSessionsController : ControllerBase
{
    private const string UploadOffsetHeaderName = "X-Upload-Offset";

    private readonly IAuditLogWriter _auditLogWriter;
    private readonly ICurrentUserService _currentUserService;
    private readonly IResumableUploadService _resumableUploadService;
    private readonly UploadOptions _uploadOptions;

    public UploadSessionsController(
        IAuditLogWriter auditLogWriter,
        ICurrentUserService currentUserService,
        IResumableUploadService resumableUploadService,
        IOptions<UploadOptions> uploadOptions)
    {
        _auditLogWriter = auditLogWriter;
        _currentUserService = currentUserService;
        _resumableUploadService = resumableUploadService;
        _uploadOptions = uploadOptions.Value;
    }

    /// <summary>
    /// Creates a resumable upload session before chunks are sent.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(UploadSessionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status415UnsupportedMediaType)]
    public async Task<ActionResult<UploadSessionResponse>> CreateAsync(
        CreateResumableUploadSessionRequest request,
        CancellationToken cancellationToken)
    {
        if (request.TotalSizeBytes > _uploadOptions.MaxUploadBytes)
        {
            return Problem(
                title: "Upload is too large.",
                detail: $"Maximum upload size is {_uploadOptions.MaxUploadBytes} bytes.",
                statusCode: StatusCodes.Status413PayloadTooLarge);
        }

        if (!IsAllowedContentType(request.ContentType))
        {
            return Problem(
                title: "File type is not allowed.",
                detail: $"Content type '{request.ContentType}' is not allowed.",
                statusCode: StatusCodes.Status415UnsupportedMediaType);
        }

        var response = await _resumableUploadService.CreateSessionAsync(
            new CreateUploadSessionRequest(
                request.OriginalName,
                request.ContentType,
                request.TotalSizeBytes,
                request.Tags ?? [],
                _currentUserService.UserId),
            cancellationToken);

        await _auditLogWriter.WriteAsync(
            null,
            AuditAction.Upload,
            $"Started resumable upload session '{response.Id}' for '{response.OriginalName}'.",
            cancellationToken);

        return Created($"/api/upload-sessions/{response.Id}", response);
    }

    /// <summary>
    /// Appends one chunk to a resumable upload session.
    /// </summary>
    [HttpPut("{id:guid}/chunks")]
    [Consumes("application/octet-stream")]
    [ProducesResponseType(typeof(UploadSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UploadSessionResponse>> AppendChunkAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var uploadOffset = GetUploadOffset();
        var response = await _resumableUploadService.AppendChunkAsync(
            new AppendUploadChunkRequest(
                id,
                Request.Body,
                uploadOffset),
            cancellationToken);

        return response is null ? NotFound() : Ok(response);
    }

    /// <summary>
    /// Completes a fully uploaded resumable session and creates the stored file.
    /// </summary>
    [HttpPost("{id:guid}/complete")]
    [ProducesResponseType(typeof(StoredFileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StoredFileResponse>> CompleteAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var response = await _resumableUploadService.CompleteAsync(id, cancellationToken);

        if (response is null)
        {
            return NotFound();
        }

        await _auditLogWriter.WriteAsync(
            response.Id,
            AuditAction.Upload,
            $"Completed resumable upload for '{response.OriginalName}'.",
            cancellationToken);

        return Ok(response);
    }

    private bool IsAllowedContentType(string contentType)
    {
        return _uploadOptions.AllowedContentTypes.Length == 0
            || _uploadOptions.AllowedContentTypes.Contains(
                contentType,
                StringComparer.OrdinalIgnoreCase);
    }

    private long GetUploadOffset()
    {
        if (!Request.Headers.TryGetValue(UploadOffsetHeaderName, out var value)
            || !long.TryParse(value.ToString(), out var uploadOffset))
        {
            throw new ArgumentException($"Header '{UploadOffsetHeaderName}' is required and must be a number.");
        }

        return uploadOffset;
    }

}
