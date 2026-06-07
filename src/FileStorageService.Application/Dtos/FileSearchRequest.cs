namespace FileStorageService.Application.Dtos;

public sealed record FileSearchRequest(
    int PageNumber = 1,
    int PageSize = 20,
    string? Name = null,
    string? Tag = null,
    string? ContentType = null,
    DateTime? CreatedFromUtc = null,
    DateTime? CreatedToUtc = null);
