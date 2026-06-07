namespace FileStorageService.Application.Models;

public sealed record StoredFileSearchCriteria(
    int PageNumber,
    int PageSize,
    string? Name,
    string? Tag,
    string? ContentType,
    DateTime? CreatedFromUtc,
    DateTime? CreatedToUtc);
