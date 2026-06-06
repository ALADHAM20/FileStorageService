using System.Text.Json;
using FileStorageService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FileStorageService.Infrastructure.DbContext;

public sealed class StoredFileConfiguration : IEntityTypeConfiguration<StoredFile>
{
    public void Configure(EntityTypeBuilder<StoredFile> builder)
    {
        builder.ToTable("StoredFiles");

        builder.HasKey(file => file.Id);

        builder.Property(file => file.OriginalName)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(file => file.StoredKey)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(file => file.SizeBytes)
            .IsRequired();

        builder.Property(file => file.ContentType)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(file => file.Sha256Checksum)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(file => file.CreatedAtUtc)
            .IsRequired();

        builder.Property(file => file.DeletedAtUtc);

        builder.Property(file => file.Version);

        builder.Property(file => file.CreatedByUserId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Ignore(file => file.Tags);
        builder.Ignore(file => file.IsDeleted);

        builder.Property<List<string>>("_tags")
            .HasColumnName("Tags")
            .HasConversion(
                tags => JsonSerializer.Serialize(tags, (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<List<string>>(json, (JsonSerializerOptions?)null) ?? new List<string>())
            .Metadata.SetValueComparer(new ValueComparer<List<string>>(
                (left, right) => TagsAreEqual(left, right),
                tags => GetTagsHashCode(tags),
                tags => tags.ToList()));

        builder.HasIndex(file => file.CreatedAtUtc);
        builder.HasIndex(file => file.CreatedByUserId);
        builder.HasIndex(file => file.ContentType);
        builder.HasIndex(file => file.DeletedAtUtc);
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
