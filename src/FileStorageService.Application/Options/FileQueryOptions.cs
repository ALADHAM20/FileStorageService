namespace FileStorageService.Application.Options;

public sealed class FileQueryOptions
{
    public const string SectionName = "Files:Query";

    public int DefaultPageSize { get; init; } = 20;

    public int MaxPageSize { get; init; } = 100;
}
