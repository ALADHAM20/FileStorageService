using FileStorageService.Application.Dtos;
using FileStorageService.Application.Interfaces;
using FileStorageService.Application.Models;
using FileStorageService.Application.Options;
using FileStorageService.Domain.Entities;
using FileStorageService.Domain.Enums;
using Microsoft.Extensions.Options;

namespace FileStorageService.Application.Services;

public sealed class ResumableUploadService : IResumableUploadService
{
    private readonly IFileStorageService _fileStorageService;
    private readonly ResumableUploadOptions _options;
    private readonly IResumableUploadStorageService _resumableUploadStorageService;
    private readonly IStoredFileRepository _storedFileRepository;
    private readonly IUploadSessionRepository _uploadSessionRepository;

    public ResumableUploadService(
        IFileStorageService fileStorageService,
        IResumableUploadStorageService resumableUploadStorageService,
        IStoredFileRepository storedFileRepository,
        IUploadSessionRepository uploadSessionRepository,
        IOptions<ResumableUploadOptions> options)
    {
        _fileStorageService = fileStorageService;
        _options = options.Value;
        _resumableUploadStorageService = resumableUploadStorageService;
        _storedFileRepository = storedFileRepository;
        _uploadSessionRepository = uploadSessionRepository;
    }

    public async Task<UploadSessionResponse> CreateSessionAsync(
        CreateUploadSessionRequest request,
        CancellationToken cancellationToken)
    {
        ValidateCreateRequest(request);

        var now = DateTime.UtcNow;
        var sessionId = Guid.NewGuid();
        var tempStoredKey = CreateTempStoredKey(sessionId);
        var uploadSession = UploadSession.Create(
            sessionId,
            request.OriginalName,
            request.ContentType,
            request.TotalSizeBytes,
            request.Tags,
            request.CreatedByUserId,
            now,
            now.Add(_options.SessionLifetime),
            tempStoredKey);

        await _resumableUploadStorageService.CreateAsync(tempStoredKey, cancellationToken);
        await _uploadSessionRepository.AddAsync(uploadSession, cancellationToken);
        await _uploadSessionRepository.SaveChangesAsync(cancellationToken);

        return ToSessionResponse(uploadSession);
    }

    public async Task<UploadSessionResponse?> AppendChunkAsync(
        AppendUploadChunkRequest request,
        CancellationToken cancellationToken)
    {
        ValidateAppendRequest(request);

        var uploadSession = await _uploadSessionRepository.GetByIdAsync(
            request.SessionId,
            cancellationToken);

        if (uploadSession is null)
        {
            return null;
        }

        EnsureActive(uploadSession);

        var uploadedBytes = await _resumableUploadStorageService.AppendAsync(
            uploadSession.TempStoredKey,
            request.Content,
            request.UploadOffset,
            cancellationToken);

        uploadSession.UpdateProgress(uploadedBytes);
        await _uploadSessionRepository.SaveChangesAsync(cancellationToken);

        return ToSessionResponse(uploadSession);
    }

    public async Task<StoredFileResponse?> CompleteAsync(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        if (sessionId == Guid.Empty)
        {
            throw new ArgumentException("Upload session id cannot be empty.", nameof(sessionId));
        }

        var uploadSession = await _uploadSessionRepository.GetByIdAsync(sessionId, cancellationToken);

        if (uploadSession is null)
        {
            return null;
        }

        EnsureActive(uploadSession);

        if (uploadSession.UploadedBytes != uploadSession.TotalSizeBytes)
        {
            throw new InvalidOperationException("Upload session is not fully uploaded yet.");
        }

        StoredFileContentResult? storedContent = null;

        try
        {
            var completedAtUtc = DateTime.UtcNow;
            await using (var tempContent = await _resumableUploadStorageService.OpenReadAsync(
                uploadSession.TempStoredKey,
                cancellationToken))
            {
                storedContent = await _fileStorageService.SaveAsync(
                    tempContent,
                    completedAtUtc,
                    cancellationToken);
            }

            var storedFile = StoredFile.Create(
                Guid.NewGuid(),
                uploadSession.OriginalName,
                storedContent.StoredKey,
                storedContent.SizeBytes,
                uploadSession.ContentType,
                storedContent.Sha256Checksum,
                uploadSession.Tags,
                completedAtUtc,
                uploadSession.CreatedByUserId);

            await _storedFileRepository.AddAsync(storedFile, cancellationToken);
            uploadSession.Complete(storedFile.Id, completedAtUtc);
            await _uploadSessionRepository.SaveChangesAsync(cancellationToken);
            await _resumableUploadStorageService.DeleteAsync(
                uploadSession.TempStoredKey,
                CancellationToken.None);

            return ToStoredFileResponse(storedFile);
        }
        catch
        {
            if (storedContent is not null)
            {
                await _fileStorageService.DeleteAsync(storedContent.StoredKey, CancellationToken.None);
            }

            throw;
        }
    }

    private static void ValidateCreateRequest(CreateUploadSessionRequest request)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }
    }

    private static void ValidateAppendRequest(AppendUploadChunkRequest request)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (request.SessionId == Guid.Empty)
        {
            throw new ArgumentException("Upload session id cannot be empty.", nameof(request));
        }

        if (request.Content is null || !request.Content.CanRead)
        {
            throw new ArgumentException("Chunk content must be readable.", nameof(request));
        }

        if (request.UploadOffset < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "Upload offset cannot be negative.");
        }
    }

    private static void EnsureActive(UploadSession uploadSession)
    {
        if (uploadSession.Status != UploadSessionStatus.Active)
        {
            throw new InvalidOperationException("Upload session is not active.");
        }

        if (uploadSession.ExpiresAtUtc <= DateTime.UtcNow)
        {
            uploadSession.Expire();
            throw new InvalidOperationException("Upload session has expired.");
        }
    }

    private static string CreateTempStoredKey(Guid sessionId)
    {
        return $"{sessionId:N}.upload";
    }

    private static UploadSessionResponse ToSessionResponse(UploadSession uploadSession)
    {
        return new UploadSessionResponse(
            uploadSession.Id,
            uploadSession.OriginalName,
            uploadSession.ContentType,
            uploadSession.TotalSizeBytes,
            uploadSession.UploadedBytes,
            uploadSession.Tags,
            uploadSession.Status.ToString(),
            uploadSession.CreatedAtUtc,
            uploadSession.ExpiresAtUtc,
            uploadSession.FinalFileId);
    }

    private static StoredFileResponse ToStoredFileResponse(StoredFile storedFile)
    {
        return new StoredFileResponse(
            storedFile.Id,
            storedFile.OriginalName,
            storedFile.StoredKey,
            storedFile.SizeBytes,
            storedFile.ContentType,
            storedFile.Sha256Checksum,
            storedFile.Tags,
            storedFile.CreatedAtUtc,
            storedFile.DeletedAtUtc,
            storedFile.Version,
            storedFile.CreatedByUserId);
    }
}
