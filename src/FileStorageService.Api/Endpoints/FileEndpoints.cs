using System.Text;
using FileStorageService.Application.Dtos;
using FileStorageService.Application.Interfaces;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;

namespace FileStorageService.Api.Endpoints;

public static class FileEndpoints
{
    public static IEndpointRouteBuilder MapFileEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/files", SearchAsync)
            .WithName("SearchFiles")
            .WithOpenApi()
            .Produces<PagedResponse<StoredFileResponse>>(StatusCodes.Status200OK);

        app.MapGet("/api/files/{id:guid}/download", DownloadAsync)
            .WithName("DownloadFile")
            .WithOpenApi()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status206PartialContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        app.MapGet("/api/files/{id:guid}/preview", PreviewAsync)
            .WithName("PreviewFile")
            .WithOpenApi()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status206PartialContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        app.MapDelete("/api/files/{id:guid}", SoftDeleteAsync)
            .WithName("SoftDeleteFile")
            .WithOpenApi()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        app.MapDelete("/api/files/{id:guid}/hard", HardDeleteAsync)
            .WithName("HardDeleteFile")
            .WithOpenApi()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        app.MapPost("/api/files", UploadAsync)
            .WithName("UploadFile")
            .WithOpenApi(operation =>
            {
                operation.RequestBody = CreateMultipartRequestBody();

                return operation;
            })
            .Produces<StoredFileResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status415UnsupportedMediaType);

        return app;
    }

    private static async Task<IResult> SoftDeleteAsync(
        Guid id,
        IFileDeleteService fileDeleteService,
        CancellationToken cancellationToken)
    {
        var deleted = await fileDeleteService.SoftDeleteAsync(id, cancellationToken);

        return deleted ? Results.NoContent() : Results.NotFound();
    }

    private static async Task<IResult> HardDeleteAsync(
        Guid id,
        IFileDeleteService fileDeleteService,
        CancellationToken cancellationToken)
    {
        var deleted = await fileDeleteService.HardDeleteAsync(id, cancellationToken);

        return deleted ? Results.NoContent() : Results.NotFound();
    }

    private static async Task<IResult> PreviewAsync(
        Guid id,
        IFilePreviewService filePreviewService,
        CancellationToken cancellationToken)
    {
        var response = await filePreviewService.GetPreviewAsync(id, cancellationToken);

        if (response is null)
        {
            return Results.NotFound();
        }

        return Results.File(
            response.Content,
            response.ContentType,
            enableRangeProcessing: true);
    }

    private static async Task<IResult> DownloadAsync(
        Guid id,
        IFileDownloadService fileDownloadService,
        CancellationToken cancellationToken)
    {
        var response = await fileDownloadService.GetDownloadAsync(id, cancellationToken);

        if (response is null)
        {
            return Results.NotFound();
        }

        return Results.File(
            response.Content,
            response.ContentType,
            response.OriginalName,
            enableRangeProcessing: true);
    }

    private static async Task<IResult> SearchAsync(
        IFileQueryService fileQueryService,
        int pageNumber,
        int pageSize,
        string? name,
        string? tag,
        string? contentType,
        DateTime? createdFromUtc,
        DateTime? createdToUtc,
        CancellationToken cancellationToken)
    {
        var request = new FileSearchRequest(
            pageNumber,
            pageSize,
            name,
            tag,
            contentType,
            createdFromUtc,
            createdToUtc);

        var response = await fileQueryService.SearchAsync(request, cancellationToken);

        return Results.Ok(response);
    }

    private static async Task<IResult> UploadAsync(
        HttpRequest request,
        IFileUploadService fileUploadService,
        CancellationToken cancellationToken)
    {
        if (!IsMultipartRequest(request))
        {
            return Results.Problem(
                title: "Unsupported content type.",
                detail: "Use multipart/form-data with a file field.",
                statusCode: StatusCodes.Status415UnsupportedMediaType);
        }

        var boundary = GetMultipartBoundary(request.ContentType!);
        var reader = new MultipartReader(boundary, request.Body);
        var tags = new List<string>();
        var createdByUserId = "development-user";

        while (await reader.ReadNextSectionAsync(cancellationToken) is { } section)
        {
            if (!ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var contentDisposition))
            {
                continue;
            }

            if (IsFormField(contentDisposition))
            {
                await ReadFormFieldAsync(section, contentDisposition, tags, value =>
                {
                    createdByUserId = value;
                }, cancellationToken);

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

            var uploadRequest = new UploadFileRequest(
                section.Body,
                originalName,
                contentType,
                tags,
                createdByUserId);

            var response = await fileUploadService.UploadAsync(uploadRequest, cancellationToken);

            return Results.Created($"/api/files/{response.Id}", response);
        }

        return Results.BadRequest("Multipart request must include a file field.");
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
        Action<string> setCreatedByUserId,
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
            return;
        }

        if (fieldName.Equals("createdByUserId", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(value))
        {
            setCreatedByUserId(value.Trim());
        }
    }

    private static IEnumerable<string> ParseTags(string value)
    {
        return value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(tag => !string.IsNullOrWhiteSpace(tag));
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

    private static OpenApiRequestBody CreateMultipartRequestBody()
    {
        return new OpenApiRequestBody
        {
            Required = true,
            Content =
            {
                ["multipart/form-data"] = new OpenApiMediaType
                {
                    Schema = new OpenApiSchema
                    {
                        Type = "object",
                        Required = new HashSet<string> { "file" },
                        Properties =
                        {
                            ["tags"] = new OpenApiSchema
                            {
                                Type = "string",
                                Description = "Comma-separated tags."
                            },
                            ["createdByUserId"] = new OpenApiSchema
                            {
                                Type = "string",
                                Description = "Temporary user id until JWT authentication is added."
                            },
                            ["file"] = new OpenApiSchema
                            {
                                Type = "string",
                                Format = "binary"
                            }
                        }
                    }
                }
            }
        };
    }
}
