namespace FileStorageService.Api.Requests;

public sealed record MultipartFileUploadReadResult(
    MultipartFileUploadRequest? Request,
    string? ErrorTitle,
    string? ErrorDetail,
    int? StatusCode)
{
    public bool IsSuccess => Request is not null;

    public static MultipartFileUploadReadResult Success(MultipartFileUploadRequest request)
    {
        return new MultipartFileUploadReadResult(request, null, null, null);
    }

    public static MultipartFileUploadReadResult Failure(
        string title,
        string detail,
        int statusCode)
    {
        return new MultipartFileUploadReadResult(null, title, detail, statusCode);
    }
}
