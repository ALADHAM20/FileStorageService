using System.Text;
using FileStorageService.Api.Options;
using FileStorageService.Api.Requests;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;

namespace FileStorageService.Api.Services;

public sealed class MultipartUploadRequestReader
{
    public async Task<MultipartFileUploadReadResult> ReadAsync(
        HttpRequest request,
        UploadOptions uploadOptions,
        CancellationToken cancellationToken)
    {
        if (!IsMultipartRequest(request))
        {
            return MultipartFileUploadReadResult.Failure(
                "Unsupported content type.",
                "Use multipart/form-data with a file field.",
                StatusCodes.Status415UnsupportedMediaType);
        }

        if (request.ContentLength > uploadOptions.MaxUploadBytes)
        {
            return MultipartFileUploadReadResult.Failure(
                "Upload is too large.",
                $"Maximum upload size is {uploadOptions.MaxUploadBytes} bytes.",
                StatusCodes.Status413PayloadTooLarge);
        }

        var boundary = GetMultipartBoundary(request.ContentType!);
        var reader = new MultipartReader(boundary, request.Body)
        {
            BodyLengthLimit = uploadOptions.MaxUploadBytes
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

            var contentType = string.IsNullOrWhiteSpace(section.ContentType)
                ? "application/octet-stream"
                : section.ContentType;

            if (!IsAllowedContentType(contentType, uploadOptions))
            {
                return MultipartFileUploadReadResult.Failure(
                    "File type is not allowed.",
                    $"Content type '{contentType}' is not allowed.",
                    StatusCodes.Status415UnsupportedMediaType);
            }

            return MultipartFileUploadReadResult.Success(new MultipartFileUploadRequest(
                section.Body,
                GetSubmittedFileName(contentDisposition),
                contentType,
                tags));
        }

        return MultipartFileUploadReadResult.Failure(
            "Invalid multipart request.",
            "Multipart request must include a file field.",
            StatusCodes.Status400BadRequest);
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

    private static bool IsAllowedContentType(string contentType, UploadOptions uploadOptions)
    {
        return uploadOptions.AllowedContentTypes.Length == 0
            || uploadOptions.AllowedContentTypes.Contains(
                contentType,
                StringComparer.OrdinalIgnoreCase);
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
