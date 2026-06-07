using System.Security.Claims;
using System.Text;
using FileStorageService.Application.Dtos;
using FileStorageService.Application.Interfaces;
using FileStorageService.Api.Filters;
using FileStorageService.Api.Options;
using FileStorageService.Api.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace FileStorageService.Api.Controllers;

/// <summary>
/// File metadata and content operations.
/// </summary>
[ApiController]
[Authorize]
[Route("api/files")]
public sealed class FilesController : ControllerBase
{
    private readonly IFileDeleteService _fileDeleteService;
    private readonly IFileDownloadService _fileDownloadService;
    private readonly IFilePreviewService _filePreviewService;
    private readonly IFileQueryService _fileQueryService;
    private readonly IFileUploadService _fileUploadService;
    private readonly UploadOptions _uploadOptions;

    public FilesController(
        IFileDeleteService fileDeleteService,
        IFileDownloadService fileDownloadService,
        IFilePreviewService filePreviewService,
        IFileQueryService fileQueryService,
        IFileUploadService fileUploadService,
        IOptions<UploadOptions> uploadOptions)
    {
        _fileDeleteService = fileDeleteService;
        _fileDownloadService = fileDownloadService;
        _filePreviewService = filePreviewService;
        _fileQueryService = fileQueryService;
        _fileUploadService = fileUploadService;
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
        if (!IsMultipartRequest(Request))
        {
            return Problem(
                title: "Unsupported content type.",
                detail: "Use multipart/form-data with a file field.",
                statusCode: StatusCodes.Status415UnsupportedMediaType);
        }

        if (Request.ContentLength > _uploadOptions.MaxUploadBytes)
        {
            return Problem(
                title: "Upload is too large.",
                detail: $"Maximum upload size is {_uploadOptions.MaxUploadBytes} bytes.",
                statusCode: StatusCodes.Status413PayloadTooLarge);
        }

        var boundary = GetMultipartBoundary(Request.ContentType!);
        var reader = new MultipartReader(boundary, Request.Body)
        {
            BodyLengthLimit = _uploadOptions.MaxUploadBytes
        };
        var tags = new List<string>();

        while (await reader.ReadNextSectionAsync(cancellationToken) is { } section)
        {
            if (!ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var contentDisposition))
            {
                continue;
            }

            if (IsFormField(contentDisposition))
            {
                await ReadFormFieldAsync(section, contentDisposition, tags, cancellationToken);

                continue;
            }

            if (!IsFileField(contentDisposition))
            {
                continue;
            }

            var originalName = GetSubmittedFileName(contentDisposition);
            var contentType = string.IsNullOrWhiteSpace(section.ContentType)
                ? "application/octet-stream"
                : section.ContentType;

            if (!IsAllowedContentType(contentType))
            {
                return Problem(
                    title: "File type is not allowed.",
                    detail: $"Content type '{contentType}' is not allowed.",
                    statusCode: StatusCodes.Status415UnsupportedMediaType);
            }

            var uploadRequest = new UploadFileRequest(
                section.Body,
                originalName,
                contentType,
                tags,
                GetCurrentUserId());

            var response = await _fileUploadService.UploadAsync(uploadRequest, cancellationToken);

            return Created($"/api/files/{response.Id}", response);
        }

        return BadRequest("Multipart request must include a file field.");
    }

    private bool IsAllowedContentType(string contentType)
    {
        return _uploadOptions.AllowedContentTypes.Length == 0
            || _uploadOptions.AllowedContentTypes.Contains(
                contentType,
                StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Downloads a stored file as an attachment with HTTP Range support.
    /// </summary>
    [HttpGet("{id:guid}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status206PartialContent)]
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

        return deleted
            ? Ok(new ApiResponse(
                StatusCodes.Status200OK,
                "File was permanently deleted successfully."))
            : NotFound(new ApiResponse(
                StatusCodes.Status404NotFound,
                "File was not found."));
    }

    private static bool IsMultipartRequest(HttpRequest request)
    {
        return request.HasFormContentType
            && request.ContentType?.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static string GetMultipartBoundary(string contentType)
    {
        var mediaType = MediaTypeHeaderValue.Parse(contentType);
        var boundary = HeaderUtilities.RemoveQuotes(mediaType.Boundary).Value;

        if (string.IsNullOrWhiteSpace(boundary))
        {
            throw new InvalidOperationException("Multipart boundary is missing.");
        }

        return boundary;
    }

    private static bool IsFormField(ContentDispositionHeaderValue contentDisposition)
    {
        return contentDisposition.IsFormDisposition()
            && string.IsNullOrEmpty(contentDisposition.FileName.Value)
            && string.IsNullOrEmpty(contentDisposition.FileNameStar.Value);
    }

    private static bool IsFileField(ContentDispositionHeaderValue contentDisposition)
    {
        return contentDisposition.IsFileDisposition()
            && (!string.IsNullOrEmpty(contentDisposition.FileName.Value)
                || !string.IsNullOrEmpty(contentDisposition.FileNameStar.Value));
    }

    private static async Task ReadFormFieldAsync(
        MultipartSection section,
        ContentDispositionHeaderValue contentDisposition,
        List<string> tags,
        CancellationToken cancellationToken)
    {
        var fieldName = HeaderUtilities.RemoveQuotes(contentDisposition.Name).Value;

        if (string.IsNullOrWhiteSpace(fieldName))
        {
            return;
        }

        using var reader = new StreamReader(
            section.Body,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true,
            bufferSize: 1024,
            leaveOpen: true);

        var value = await reader.ReadToEndAsync(cancellationToken);

        if (fieldName.Equals("tags", StringComparison.OrdinalIgnoreCase)
            || fieldName.Equals("tag", StringComparison.OrdinalIgnoreCase))
        {
            tags.AddRange(ParseTags(value));
        }
    }

    private string GetCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new InvalidOperationException("User id claim is missing.");
    }

    private static IReadOnlyCollection<string> ParseTags(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        return value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .ToArray();
    }

    private static string GetSubmittedFileName(ContentDispositionHeaderValue contentDisposition)
    {
        var fileName = HeaderUtilities.RemoveQuotes(contentDisposition.FileNameStar).Value;

        if (string.IsNullOrWhiteSpace(fileName))
        {
            fileName = HeaderUtilities.RemoveQuotes(contentDisposition.FileName).Value;
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new InvalidOperationException("Uploaded file name is missing.");
        }

        return Path.GetFileName(fileName.Replace('\\', '/'));
    }
}
