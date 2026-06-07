namespace FileStorageService.Application.Options;

public sealed class FilePreviewOptions
{
    public const string SectionName = "Files:Preview";

    public string[] AllowedContentTypes { get; init; } = [];

    public string[] AllowedContentTypePrefixes { get; init; } = [];
}
