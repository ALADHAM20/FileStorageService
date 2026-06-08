namespace FileStorageService.Api.Requests;

public sealed record MultipartFileUploadRequest(
    Stream Content,
    string OriginalName,
    string ContentType,
    IReadOnlyCollection<string> Tags);
