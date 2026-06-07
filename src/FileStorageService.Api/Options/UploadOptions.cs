namespace FileStorageService.Api.Options;

public sealed class UploadOptions
{
    public const string SectionName = "Upload";

    public long MaxUploadBytes { get; init; } = 104_857_600;

    public string[] AllowedContentTypes { get; init; } = [];
}
