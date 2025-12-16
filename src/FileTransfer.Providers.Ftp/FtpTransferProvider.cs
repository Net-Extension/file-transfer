using FileTransfer.Core.Abstractions;
using FileTransfer.Core.Models;
using FileTransfer.Core.Providers;
using FluentFTP;
using Microsoft.Extensions.Logging;

namespace FileTransfer.Providers.Ftp;

/// <summary>
/// FTP transfer provider using FluentFTP library.
/// Note: FluentFTP provides robust FTP/FTPS support with progress tracking.
/// </summary>
public sealed class FtpTransferProvider : BaseTransferProvider
{
    public override string Scheme => "ftp";

    public FtpTransferProvider(
        ILogger<FtpTransferProvider> logger,
        IIntegrityService integrityService,
        ITransferValidator validator)
        : base(logger, integrityService, validator)
    {
    }

    public override async Task<TransferResult> UploadAsync(TransferRequest request, CancellationToken cancellationToken = default)
    {
        var startTime = DateTimeOffset.UtcNow;
        using var activity = StartActivity("FTP.Upload", request.CorrelationId);

        try
        {
            var destinationUri = TransferUri.Parse(request.Destination);
            ValidateHost(destinationUri.Host, request.Options);

            var fileSize = await GetFileSizeAsync(request.Source, cancellationToken);

            using var client = new AsyncFtpClient(destinationUri.Host, request.Credentials?.Username, request.Credentials?.Password, destinationUri.Port ?? 21);
            await client.Connect(cancellationToken);

            // Upload with progress tracking
            var progress = new Progress<FtpProgress>(ftpProgress =>
            {
                var transferProgress = TransferProgress.CreateInProgress(
                    fileSize,
                    (long)ftpProgress.TransferredBytes,
                    ftpProgress.TransferSpeed,
                    request.CorrelationId);
                ReportProgress(request, transferProgress);
            });

            var result = await client.UploadFile(request.Source, destinationUri.Path, FtpRemoteExists.Overwrite, createRemoteDir: false, progress: progress, token: cancellationToken);

            await client.Disconnect(cancellationToken);

            return new TransferResult
            {
                Success = result == FtpStatus.Success,
                Source = request.Source,
                Destination = request.Destination,
                BytesTransferred = fileSize,
                TotalBytes = fileSize,
                Duration = DateTimeOffset.UtcNow - startTime,
                CorrelationId = request.CorrelationId,
                StartTime = startTime,
                EndTime = DateTimeOffset.UtcNow,
                Protocol = Scheme
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "FTP upload failed (CorrelationId: {CorrelationId})", request.CorrelationId);

            return new TransferResult
            {
                Success = false,
                Source = request.Source,
                Destination = request.Destination,
                BytesTransferred = 0,
                TotalBytes = 0,
                Duration = DateTimeOffset.UtcNow - startTime,
                CorrelationId = request.CorrelationId,
                StartTime = startTime,
                EndTime = DateTimeOffset.UtcNow,
                Protocol = Scheme,
                Error = TransferError.FromException(ex)
            };
        }
    }

    public override async Task<TransferResult> DownloadAsync(TransferRequest request, CancellationToken cancellationToken = default)
    {
        var startTime = DateTimeOffset.UtcNow;
        using var activity = StartActivity("FTP.Download", request.CorrelationId);

        try
        {
            var sourceUri = TransferUri.Parse(request.Source);
            ValidateHost(sourceUri.Host, request.Options);

            using var client = new AsyncFtpClient(sourceUri.Host, request.Credentials?.Username, request.Credentials?.Password, sourceUri.Port ?? 21);
            await client.Connect(cancellationToken);

            var fileSize = await client.GetFileSize(sourceUri.Path, -1, cancellationToken);

            var progress = new Progress<FtpProgress>(ftpProgress =>
            {
                var transferProgress = TransferProgress.CreateInProgress(
                    fileSize,
                    (long)ftpProgress.TransferredBytes,
                    ftpProgress.TransferSpeed,
                    request.CorrelationId);
                ReportProgress(request, transferProgress);
            });

            var result = await client.DownloadFile(request.Destination, sourceUri.Path, FtpLocalExists.Overwrite, progress: progress, token: cancellationToken);

            await client.Disconnect(cancellationToken);

            return new TransferResult
            {
                Success = result == FtpStatus.Success,
                Source = request.Source,
                Destination = request.Destination,
                BytesTransferred = fileSize,
                TotalBytes = fileSize,
                Duration = DateTimeOffset.UtcNow - startTime,
                CorrelationId = request.CorrelationId,
                StartTime = startTime,
                EndTime = DateTimeOffset.UtcNow,
                Protocol = Scheme
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "FTP download failed (CorrelationId: {CorrelationId})", request.CorrelationId);

            return new TransferResult
            {
                Success = false,
                Source = request.Source,
                Destination = request.Destination,
                BytesTransferred = 0,
                TotalBytes = 0,
                Duration = DateTimeOffset.UtcNow - startTime,
                CorrelationId = request.CorrelationId,
                StartTime = startTime,
                EndTime = DateTimeOffset.UtcNow,
                Protocol = Scheme,
                Error = TransferError.FromException(ex)
            };
        }
    }

    public override TransferCapabilities GetCapabilities()
    {
        return new TransferCapabilities
        {
            SupportsChunking = false,
            SupportsResume = true,
            SupportsChecksum = false,
            SupportsCompression = false,
            SupportsEncryption = false,
            SupportsParallelism = false,
            SupportedCredentialTypes = new HashSet<CredentialType>
            {
                CredentialType.UsernamePassword
            }
        };
    }
}
