namespace FileStorageService.Application.Options;

public sealed class ResumableUploadOptions
{
    public const string SectionName = "Files:ResumableUpload";

    public int SessionLifetimeHours { get; init; } = 24;

    public TimeSpan SessionLifetime => TimeSpan.FromHours(Math.Max(SessionLifetimeHours, 1));
}
