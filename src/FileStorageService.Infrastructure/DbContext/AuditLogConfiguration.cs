using FileStorageService.Domain.Entities;
using FileStorageService.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FileStorageService.Infrastructure.DbContext;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        builder.HasKey(auditLog => auditLog.Id);

        builder.Property(auditLog => auditLog.FileId);

        builder.Property(auditLog => auditLog.Action)
            .HasConversion(
                action => action.ToString(),
                value => Enum.Parse<AuditAction>(value))
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(auditLog => auditLog.UserId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(auditLog => auditLog.OccurredAtUtc)
            .IsRequired();

        builder.Property(auditLog => auditLog.IpAddress)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(auditLog => auditLog.CorrelationId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(auditLog => auditLog.Details)
            .HasMaxLength(1000)
            .IsRequired();

        builder.HasIndex(auditLog => auditLog.FileId);
        builder.HasIndex(auditLog => auditLog.Action);
        builder.HasIndex(auditLog => auditLog.UserId);
        builder.HasIndex(auditLog => auditLog.OccurredAtUtc);
    }
}
