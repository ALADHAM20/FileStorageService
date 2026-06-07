namespace FileStorageService.Application.Dtos;

public sealed record UploadFileRequest(
    Stream Content,
    string OriginalName,
    string ContentType,
    IReadOnlyCollection<string> Tags,
    string CreatedByUserId);
