using FileStorageService.Domain.Entities;

namespace FileStorageService.Application.Models;

public sealed record AuditLogSearchResult(
    IReadOnlyCollection<AuditLog> Items,
    int TotalCount);
