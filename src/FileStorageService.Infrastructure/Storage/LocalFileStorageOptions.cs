namespace FileStorageService.Infrastructure.Storage;

public sealed class LocalFileStorageOptions
{
    public LocalFileStorageOptions(string rootPath)
    {
        RootPath = rootPath;
    }

    public string RootPath { get; }
}
