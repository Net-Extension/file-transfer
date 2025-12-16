using FileTransfer.Core.Abstractions;
using FileTransfer.Core.Models;
using FileTransfer.Core.Providers;
using Microsoft.Extensions.Logging;
using Renci.SshNet;

namespace FileTransfer.Providers.Sftp;

/// <summary>
/// SFTP transfer provider using SSH.NET library.
/// </summary>
public sealed class SftpTransferProvider : BaseTransferProvider
{
    public override string Scheme => "sftp";

    public SftpTransferProvider(
        ILogger<SftpTransferProvider> logger,
        IIntegrityService integrityService,
        ITransferValidator validator)
        : base(logger, integrityService, validator)
    {
    }

    public override async Task<TransferResult> UploadAsync(TransferRequest request, CancellationToken cancellationToken = default)
    {
        var startTime = DateTimeOffset.UtcNow;
        using var activity = StartActivity("SFTP.Upload", request.CorrelationId);

        try
        {
            var destinationUri = TransferUri.Parse(request.Destination);
            ValidateHost(destinationUri.Host, request.Options);

            var fileSize = await GetFileSizeAsync(request.Source, cancellationToken);

            ConnectionInfo connectionInfo = CreateConnectionInfo(destinationUri, request.Credentials);

            using var client = new SftpClient(connectionInfo);
            client.Connect();

            using var fileStream = new FileStream(request.Source, FileMode.Open, FileAccess.Read, FileShare.Read);

            long uploadedBytes = 0;
            client.UploadFile(fileStream, destinationUri.Path, uploaded =>
            {
                uploadedBytes = (long)uploaded;
                var transferProgress = TransferProgress.CreateInProgress(fileSize, uploadedBytes, 0, request.CorrelationId);
                ReportProgress(request, transferProgress);
            });

            client.Disconnect();

            return new TransferResult
            {
                Success = true,
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
            Logger.LogError(ex, "SFTP upload failed (CorrelationId: {CorrelationId})", request.CorrelationId);

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
        using var activity = StartActivity("SFTP.Download", request.CorrelationId);

        try
        {
            var sourceUri = TransferUri.Parse(request.Source);
            ValidateHost(sourceUri.Host, request.Options);

            ConnectionInfo connectionInfo = CreateConnectionInfo(sourceUri, request.Credentials);

            using var client = new SftpClient(connectionInfo);
            client.Connect();

            var fileSize = client.GetAttributes(sourceUri.Path).Size;

            using var fileStream = new FileStream(request.Destination, FileMode.Create, FileAccess.Write, FileShare.None);

            long downloadedBytes = 0;
            client.DownloadFile(sourceUri.Path, fileStream, downloaded =>
            {
                downloadedBytes = (long)downloaded;
                var transferProgress = TransferProgress.CreateInProgress(fileSize, downloadedBytes, 0, request.CorrelationId);
                ReportProgress(request, transferProgress);
            });

            client.Disconnect();

            return new TransferResult
            {
                Success = true,
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
            Logger.LogError(ex, "SFTP download failed (CorrelationId: {CorrelationId})", request.CorrelationId);

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
            SupportsEncryption = true, // SFTP is encrypted by nature
            SupportsParallelism = false,
            SupportedCredentialTypes = new HashSet<CredentialType>
            {
                CredentialType.UsernamePassword,
                CredentialType.PrivateKey
            }
        };
    }

    private static ConnectionInfo CreateConnectionInfo(TransferUri uri, TransferCredentials? credentials)
    {
        var port = uri.Port ?? 22;

        if (credentials?.Type == CredentialType.PrivateKey)
        {
            var keyFile = new PrivateKeyFile(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(credentials.PrivateKey!)), credentials.Passphrase);
            return new ConnectionInfo(uri.Host, port, credentials.Username ?? "root", new PrivateKeyAuthenticationMethod(credentials.Username ?? "root", keyFile));
        }

        return new ConnectionInfo(uri.Host, port, credentials?.Username ?? "root", new PasswordAuthenticationMethod(credentials?.Username ?? "root", credentials?.Password ?? string.Empty));
    }
}
