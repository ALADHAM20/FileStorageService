using System.Text.Json;
using FileStorageService.Domain.Entities;
using FileStorageService.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FileStorageService.Infrastructure.DbContext;

public sealed class UploadSessionConfiguration : IEntityTypeConfiguration<UploadSession>
{
    public void Configure(EntityTypeBuilder<UploadSession> builder)
    {
        builder.ToTable("UploadSessions");

        builder.HasKey(session => session.Id);

        builder.Property(session => session.OriginalName)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(session => session.ContentType)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(session => session.TotalSizeBytes)
            .IsRequired();

        builder.Property(session => session.UploadedBytes)
            .IsRequired();

        builder.Property(session => session.CreatedByUserId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(session => session.CreatedAtUtc)
            .IsRequired();

        builder.Property(session => session.ExpiresAtUtc)
            .IsRequired();

        builder.Property(session => session.CompletedAtUtc);

        builder.Property(session => session.Status)
            .HasConversion(
                status => status.ToString(),
                value => Enum.Parse<UploadSessionStatus>(value))
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(session => session.TempStoredKey)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(session => session.FinalFileId);

        builder.Ignore(session => session.Tags);

        builder.Property<List<string>>("_tags")
            .HasColumnName("Tags")
            .HasConversion(
                tags => JsonSerializer.Serialize(tags, (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<List<string>>(json, (JsonSerializerOptions?)null) ?? new List<string>())
            .Metadata.SetValueComparer(new ValueComparer<List<string>>(
                (left, right) => TagsAreEqual(left, right),
                tags => GetTagsHashCode(tags),
                tags => tags.ToList()));

        builder.HasIndex(session => session.Status);
        builder.HasIndex(session => session.CreatedByUserId);
        builder.HasIndex(session => session.ExpiresAtUtc);
        builder.HasIndex(session => session.FinalFileId);
    }

    private static bool TagsAreEqual(List<string>? left, List<string>? right)
    {
        if (left is null || right is null)
        {
            return left == right;
        }

        return left.SequenceEqual(right);
    }

    private static int GetTagsHashCode(List<string> tags)
    {
        var hash = new HashCode();

        foreach (var tag in tags)
        {
            hash.Add(tag);
        }

        return hash.ToHashCode();
    }
}
