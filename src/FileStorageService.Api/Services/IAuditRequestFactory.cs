using FileStorageService.Application.Dtos;
using FileStorageService.Domain.Enums;

namespace FileStorageService.Api.Services;

public interface IAuditRequestFactory
{
    CreateAuditLogRequest Create(
        Guid? fileId,
        AuditAction action,
        string details);
}
