using FileStorageService.Domain.Entities;

namespace FileStorageService.UnitTests;

public sealed class StoredFileTests
{
    [Fact]
    public void Create_WhenValuesAreValid_CreatesStoredFileMetadata()
    {
        var createdAtUtc = new DateTime(2026, 6, 6, 10, 0, 0, DateTimeKind.Utc);

        var storedFile = StoredFile.Create(
            Guid.NewGuid(),
            " report.pdf ",
            "2026/06/06/key/content.bin",
            1024,
            " application/pdf ",
            "ABC123",
            [" Finance ", "finance", "", "Quarterly"],
            createdAtUtc,
            " user-1 ",
            version: 1);

        Assert.Equal("report.pdf", storedFile.OriginalName);
        Assert.Equal("application/pdf", storedFile.ContentType);
        Assert.Equal("abc123", storedFile.Sha256Checksum);
        Assert.Equal(["finance", "quarterly"], storedFile.Tags);
        Assert.Equal("user-1", storedFile.CreatedByUserId);
        Assert.False(storedFile.IsDeleted);
    }

    [Fact]
    public void Create_WhenSizeIsZero_Throws()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            StoredFile.Create(
                Guid.NewGuid(),
                "report.pdf",
                "key",
                0,
                "application/pdf",
                "abc123",
                [],
                DateTime.UtcNow,
                "user-1"));

        Assert.Equal("sizeBytes", exception.ParamName);
    }

    [Fact]
    public void SoftDelete_WhenCalled_MarksFileAsDeleted()
    {
        var storedFile = CreateValidStoredFile();
        var deletedAtUtc = new DateTime(2026, 6, 6, 11, 0, 0, DateTimeKind.Utc);

        storedFile.SoftDelete(deletedAtUtc);

        Assert.True(storedFile.IsDeleted);
        Assert.Equal(deletedAtUtc, storedFile.DeletedAtUtc);
    }

    [Fact]
    public void SoftDelete_WhenCalledTwice_KeepsOriginalDeletedDate()
    {
        var storedFile = CreateValidStoredFile();
        var firstDeletedAtUtc = new DateTime(2026, 6, 6, 11, 0, 0, DateTimeKind.Utc);
        var secondDeletedAtUtc = new DateTime(2026, 6, 6, 12, 0, 0, DateTimeKind.Utc);

        storedFile.SoftDelete(firstDeletedAtUtc);
        storedFile.SoftDelete(secondDeletedAtUtc);

        Assert.Equal(firstDeletedAtUtc, storedFile.DeletedAtUtc);
    }

    private static StoredFile CreateValidStoredFile()
    {
        return StoredFile.Create(
            Guid.NewGuid(),
            "report.pdf",
            "2026/06/06/key/content.bin",
            1024,
            "application/pdf",
            "abc123",
            ["finance"],
            new DateTime(2026, 6, 6, 10, 0, 0, DateTimeKind.Utc),
            "user-1");
    }
}
