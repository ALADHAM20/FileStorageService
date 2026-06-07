using FileStorageService.Domain.Entities;

namespace FileStorageService.Application.Models;

public sealed record StoredFileSearchResult(
    IReadOnlyCollection<StoredFile> Items,
    int TotalCount);
