namespace FileStorageService.Domain.Entities;

using FileStorageService.Domain.Helpers;

public sealed class StoredFile
{
    private readonly List<string> _tags = [];

    private StoredFile()
    {
        OriginalName = string.Empty;
        StoredKey = string.Empty;
        ContentType = string.Empty;
        Sha256Checksum = string.Empty;
        CreatedByUserId = string.Empty;
    }

    private StoredFile(
        Guid id,
        string originalName,
        string storedKey,
        long sizeBytes,
        string contentType,
        string sha256Checksum,
        IEnumerable<string> tags,
        DateTime createdAtUtc,
        int? version,
        string createdByUserId)
    {
        Id = id;
        OriginalName = originalName;
        StoredKey = storedKey;
        SizeBytes = sizeBytes;
        ContentType = contentType;
        Sha256Checksum = sha256Checksum;
        CreatedAtUtc = createdAtUtc;
        Version = version;
        CreatedByUserId = createdByUserId;
        _tags.AddRange(StoredFileHelper.NormalizeTags(tags));
    }

    public Guid Id { get; private set; }

    public string OriginalName { get; private set; }

    public string StoredKey { get; private set; }

    public long SizeBytes { get; private set; }

    public string ContentType { get; private set; }

    public string Sha256Checksum { get; private set; }

    public IReadOnlyCollection<string> Tags => _tags.AsReadOnly();

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? DeletedAtUtc { get; private set; }

    public int? Version { get; private set; }

    public string CreatedByUserId { get; private set; }

    public bool IsDeleted => DeletedAtUtc.HasValue;

    public static StoredFile Create(
        Guid id,
        string originalName,
        string storedKey,
        long sizeBytes,
        string contentType,
        string sha256Checksum,
        IEnumerable<string>? tags,
        DateTime createdAtUtc,
        string createdByUserId,
        int? version = null)
    {
        StoredFileHelper.ValidateCreateRequest(
            id,
            originalName,
            storedKey,
            sizeBytes,
            contentType,
            sha256Checksum,
            createdAtUtc,
            createdByUserId,
            version);

        return new StoredFile(
            id,
            StoredFileHelper.CleanText(originalName),
            StoredFileHelper.CleanText(storedKey),
            sizeBytes,
            StoredFileHelper.CleanText(contentType),
            StoredFileHelper.CleanChecksum(sha256Checksum),
            tags ?? [],
            createdAtUtc,
            version,
            StoredFileHelper.CleanText(createdByUserId));
    }

    public void SoftDelete(DateTime deletedAtUtc)
    {
        if (deletedAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Deleted date must be in UTC.", nameof(deletedAtUtc));
        }

        DeletedAtUtc ??= deletedAtUtc;
    }
}
