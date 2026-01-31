using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FileTransfer.Auditing;
using FileTransfer.Compression;
using FileTransfer.Core.Abstractions;
using FileTransfer.Core.Models;
using FileTransfer.Core.Providers;
using FileTransfer.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FileTransfer.Providers.Axway;

/// <summary>
/// Axway Managed File Transfer (MFT) provider using REST API.
/// Supports Axway SecureTransport and similar MFT solutions.
/// </summary>
public sealed class AxwayTransferProvider : BaseTransferProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AxwayConfiguration _configuration;
    private readonly ICompressionService? _compressionService;
    private readonly IEncryptionService? _encryptionService;
    private readonly IAuditSink? _auditSink;

    public override string Scheme => "axway";

    public AxwayTransferProvider(
        IHttpClientFactory httpClientFactory,
        IOptions<AxwayConfiguration> configuration,
        ILogger<AxwayTransferProvider> logger,
        IIntegrityService integrityService,
        ITransferValidator validator,
        ICompressionService? compressionService = null,
        IEncryptionService? encryptionService = null,
        IAuditSink? auditSink = null)
        : base(logger, integrityService, validator)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration.Value;
        _compressionService = compressionService;
        _encryptionService = encryptionService;
        _auditSink = auditSink;
    }

    public override async Task<TransferResult> UploadAsync(TransferRequest request, CancellationToken cancellationToken = default)
    {
        var startTime = DateTimeOffset.UtcNow;
        using var activity = StartActivity("Axway.Upload", request.CorrelationId);

        try
        {
            // Validate request
            var validationResult = await ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                throw new InvalidOperationException($"Validation failed: {string.Join(", ", validationResult.Errors.Select(e => e.Message))}");
            }

            // Get file size
            var fileSize = await GetFileSizeAsync(request.Source, cancellationToken);
            await ValidateFileSizeAsync(fileSize, request.Options, cancellationToken);
            ValidateFileExtension(request.Source, request.Options);

            // Parse destination (format: axway://folder/path/filename)
            var destinationUri = TransferUri.Parse(request.Destination);
            var folderPath = Path.GetDirectoryName(destinationUri.Path)?.Replace("\\", "/") ?? "/";
            var fileName = Path.GetFileName(destinationUri.Path);

            // Extract Axway metadata from request metadata
            var metadata = ExtractAxwayMetadata(request, fileName, fileSize, folderPath);

            // Audit: Transfer started
            if (_auditSink != null)
            {
                await _auditSink.WriteAsync(AuditEvent.CreateStarted(request, Scheme, fileSize), cancellationToken);
            }

            // Report start
            var startProgress = TransferProgress.CreateStarted(fileSize, request.CorrelationId);
            ReportProgress(request, startProgress);
            InvokeCallback(request.OnStart, startProgress, "OnStart", request.CorrelationId);

            var stopwatch = Stopwatch.StartNew();

            // Step 1: Initialize upload session
            var sessionId = await InitializeUploadSessionAsync(metadata, cancellationToken);

            // Step 2: Upload file content
            using var fileStream = new FileStream(request.Source, FileMode.Open, FileAccess.Read, FileShare.Read, request.Options.BufferSize, useAsync: true);

            long bytesTransferred;
            if (fileSize > request.Options.ChunkSize)
            {
                // Multipart upload for large files
                bytesTransferred = await UploadMultipartAsync(sessionId, fileStream, fileSize, request, cancellationToken);
            }
            else
            {
                // Single upload for small files
                bytesTransferred = await UploadSingleAsync(sessionId, fileStream, fileSize, request, cancellationToken);
            }

            // Step 3: Finalize upload and trigger workflows
            var fileId = await FinalizeUploadAsync(sessionId, metadata, cancellationToken);

            stopwatch.Stop();

            // Compute checksum if enabled
            string? checksum = null;
            if (request.Options.VerifyChecksum)
            {
                fileStream.Position = 0;
                checksum = await IntegrityService.ComputeChecksumAsync(fileStream, request.Options.ChecksumAlgorithm, cancellationToken);
            }

            var result = new TransferResult
            {
                Success = true,
                Source = request.Source,
                Destination = request.Destination,
                BytesTransferred = bytesTransferred,
                TotalBytes = fileSize,
                Duration = stopwatch.Elapsed,
                Checksum = checksum,
                ChecksumAlgorithm = request.Options.VerifyChecksum ? request.Options.ChecksumAlgorithm : null,
                CorrelationId = request.CorrelationId,
                StartTime = startTime,
                EndTime = DateTimeOffset.UtcNow,
                Protocol = Scheme,
                Metadata = new Dictionary<string, string>
                {
                    ["AxwayFileId"] = fileId,
                    ["AxwaySessionId"] = sessionId
                }
            };

            // Report completion
            var completedProgress = TransferProgress.CreateCompleted(fileSize, request.CorrelationId);
            ReportProgress(request, completedProgress);
            InvokeCallback(request.OnCompleted, result, "OnCompleted", request.CorrelationId);

            // Audit: Transfer completed
            if (_auditSink != null)
            {
                await _auditSink.WriteAsync(AuditEvent.CreateCompleted(result), cancellationToken);
            }

            Logger.LogInformation("Axway upload completed: {BytesTransferred} bytes in {Duration}ms (FileId: {FileId}, CorrelationId: {CorrelationId})",
                bytesTransferred, stopwatch.ElapsedMilliseconds, fileId, request.CorrelationId);

            return result;
        }
        catch (Exception ex)
        {
            var error = TransferError.FromException(ex, MapExceptionToErrorCode(ex), IsTransientError(ex));

            // Audit: Transfer failed
            if (_auditSink != null)
            {
                await _auditSink.WriteAsync(AuditEvent.CreateFailed(request, Scheme, error, DateTimeOffset.UtcNow - startTime), cancellationToken);
            }

            InvokeCallback(request.OnFailed, error, "OnFailed", request.CorrelationId);

            Logger.LogError(ex, "Axway upload failed (CorrelationId: {CorrelationId})", request.CorrelationId);

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
                Error = error
            };
        }
    }

    public override async Task<TransferResult> DownloadAsync(TransferRequest request, CancellationToken cancellationToken = default)
    {
        var startTime = DateTimeOffset.UtcNow;
        using var activity = StartActivity("Axway.Download", request.CorrelationId);

        try
        {
            // Validate request
            var validationResult = await ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                throw new InvalidOperationException($"Validation failed: {string.Join(", ", validationResult.Errors.Select(e => e.Message))}");
            }

            // Parse source (format: axway://folder/path/filename or axway://fileid/abc123)
            var sourceUri = TransferUri.Parse(request.Source);
            var fileId = ExtractFileId(sourceUri);

            var httpClient = CreateHttpClient();

            // Get file metadata
            var metadata = await GetFileMetadataAsync(httpClient, fileId, cancellationToken);
            var fileSize = metadata.FileSize;

            // Audit: Transfer started
            if (_auditSink != null)
            {
                await _auditSink.WriteAsync(AuditEvent.CreateStarted(request, Scheme, fileSize), cancellationToken);
            }

            // Report start
            var startProgress = TransferProgress.CreateStarted(fileSize, request.CorrelationId);
            ReportProgress(request, startProgress);
            InvokeCallback(request.OnStart, startProgress, "OnStart", request.CorrelationId);

            var stopwatch = Stopwatch.StartNew();

            // Download file content
            var downloadUrl = $"{_configuration.ApiBaseUrl}/files/{fileId}/content";
            var response = await httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            using var downloadStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var fileStream = new FileStream(request.Destination, FileMode.Create, FileAccess.Write, FileShare.None, request.Options.BufferSize, useAsync: true);

            // Stream with progress tracking
            long bytesRead = 0;
            var buffer = new byte[request.Options.BufferSize];
            int read;

            while ((read = await downloadStream.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                bytesRead += read;

                var progress = TransferProgress.CreateInProgress(fileSize, bytesRead, bytesRead / stopwatch.Elapsed.TotalSeconds, request.CorrelationId);
                ReportProgress(request, progress);
                InvokeCallback(request.OnChunkReceived, progress, "OnChunkReceived", request.CorrelationId);
            }

            stopwatch.Stop();

            // Compute checksum if enabled
            string? checksum = null;
            if (request.Options.VerifyChecksum)
            {
                fileStream.Position = 0;
                checksum = await IntegrityService.ComputeChecksumAsync(fileStream, request.Options.ChecksumAlgorithm, cancellationToken);
            }

            var result = new TransferResult
            {
                Success = true,
                Source = request.Source,
                Destination = request.Destination,
                BytesTransferred = bytesRead,
                TotalBytes = fileSize,
                Duration = stopwatch.Elapsed,
                Checksum = checksum,
                ChecksumAlgorithm = request.Options.VerifyChecksum ? request.Options.ChecksumAlgorithm : null,
                CorrelationId = request.CorrelationId,
                StartTime = startTime,
                EndTime = DateTimeOffset.UtcNow,
                Protocol = Scheme,
                Metadata = new Dictionary<string, string>
                {
                    ["AxwayFileId"] = fileId
                }
            };

            // Report completion
            var completedProgress = TransferProgress.CreateCompleted(fileSize, request.CorrelationId);
            ReportProgress(request, completedProgress);
            InvokeCallback(request.OnCompleted, result, "OnCompleted", request.CorrelationId);

            // Audit: Transfer completed
            if (_auditSink != null)
            {
                await _auditSink.WriteAsync(AuditEvent.CreateCompleted(result), cancellationToken);
            }

            Logger.LogInformation("Axway download completed: {BytesTransferred} bytes in {Duration}ms (FileId: {FileId}, CorrelationId: {CorrelationId})",
                bytesRead, stopwatch.ElapsedMilliseconds, fileId, request.CorrelationId);

            return result;
        }
        catch (Exception ex)
        {
            var error = TransferError.FromException(ex, MapExceptionToErrorCode(ex), IsTransientError(ex));

            // Audit: Transfer failed
            if (_auditSink != null)
            {
                await _auditSink.WriteAsync(AuditEvent.CreateFailed(request, Scheme, error, DateTimeOffset.UtcNow - startTime), cancellationToken);
            }

            InvokeCallback(request.OnFailed, error, "OnFailed", request.CorrelationId);

            Logger.LogError(ex, "Axway download failed (CorrelationId: {CorrelationId})", request.CorrelationId);

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
                Error = error
            };
        }
    }

    public override TransferCapabilities GetCapabilities()
    {
        return new TransferCapabilities
        {
            SupportsChunking = true,
            SupportsResume = true,
            SupportsChecksum = true,
            SupportsCompression = _compressionService != null,
            SupportsEncryption = _encryptionService != null,
            SupportsParallelism = true,
            SupportedChecksumAlgorithms = new HashSet<ChecksumAlgorithm>
            {
                ChecksumAlgorithm.Md5,
                ChecksumAlgorithm.Sha1,
                ChecksumAlgorithm.Sha256,
                ChecksumAlgorithm.Sha512
            },
            SupportedCompressionAlgorithms = new HashSet<CompressionAlgorithm>
            {
                CompressionAlgorithm.Gzip,
                CompressionAlgorithm.Deflate
            },
            SupportedEncryptionAlgorithms = new HashSet<EncryptionAlgorithm>
            {
                EncryptionAlgorithm.AesGcm
            },
            SupportedCredentialTypes = new HashSet<CredentialType>
            {
                CredentialType.ApiKey,
                CredentialType.BearerToken,
                CredentialType.Certificate
            }
        };
    }

    private HttpClient CreateHttpClient()
    {
        var httpClient = _httpClientFactory.CreateClient("FileTransfer.Axway");
        httpClient.BaseAddress = new Uri(_configuration.ApiBaseUrl);
        httpClient.Timeout = _configuration.ApiTimeout;

        // Add authentication headers
        switch (_configuration.AuthType)
        {
            case AxwayAuthType.ApiKey:
                httpClient.DefaultRequestHeaders.Add("X-API-Key", _configuration.ApiKey);
                break;
            case AxwayAuthType.OAuth:
                // In production, implement OAuth token retrieval
                var token = GetOAuthToken(); // Implement OAuth flow
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                break;
        }

        if (!string.IsNullOrEmpty(_configuration.TenantId))
        {
            httpClient.DefaultRequestHeaders.Add("X-Tenant-Id", _configuration.TenantId);
        }

        return httpClient;
    }

    private async Task<string> InitializeUploadSessionAsync(AxwayFileMetadata metadata, CancellationToken cancellationToken)
    {
        var httpClient = CreateHttpClient();
        var initRequest = new
        {
            fileName = metadata.FileName,
            fileSize = metadata.FileSize,
            folderPath = metadata.FolderPath,
            contentType = metadata.ContentType ?? "application/octet-stream",
            attributes = metadata.Attributes,
            workflowId = metadata.WorkflowId
        };

        var response = await httpClient.PostAsJsonAsync("/uploads/init", initRequest, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<AxwayApiResponse>(cancellationToken);
        return result?.Data?["sessionId"]?.ToString() ?? throw new InvalidOperationException("Failed to initialize upload session");
    }

    private async Task<long> UploadSingleAsync(string sessionId, Stream fileStream, long fileSize, TransferRequest request, CancellationToken cancellationToken)
    {
        var httpClient = CreateHttpClient();

        using var content = new StreamContent(fileStream, request.Options.BufferSize);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        content.Headers.Add("X-Session-Id", sessionId);

        var response = await httpClient.PostAsync($"/uploads/{sessionId}/content", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        return fileSize;
    }

    private async Task<long> UploadMultipartAsync(string sessionId, Stream fileStream, long fileSize, TransferRequest request, CancellationToken cancellationToken)
    {
        var httpClient = CreateHttpClient();
        long totalUploaded = 0;
        var chunkSize = request.Options.ChunkSize;
        var buffer = new byte[chunkSize];
        int partNumber = 1;

        while (totalUploaded < fileSize)
        {
            var bytesRead = await fileStream.ReadAsync(buffer, 0, chunkSize, cancellationToken);
            if (bytesRead == 0) break;

            using var content = new ByteArrayContent(buffer, 0, bytesRead);
            content.Headers.Add("X-Session-Id", sessionId);
            content.Headers.Add("X-Part-Number", partNumber.ToString());
            content.Headers.ContentRange = new ContentRangeHeaderValue(totalUploaded, totalUploaded + bytesRead - 1, fileSize);

            var response = await httpClient.PostAsync($"/uploads/{sessionId}/parts", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            totalUploaded += bytesRead;
            partNumber++;

            var progress = TransferProgress.CreateInProgress(fileSize, totalUploaded, totalUploaded / partNumber, request.CorrelationId)
                with { CurrentChunk = partNumber, TotalChunks = (int)Math.Ceiling((double)fileSize / chunkSize) };
            ReportProgress(request, progress);
            InvokeCallback(request.OnChunkSent, progress, "OnChunkSent", request.CorrelationId);
        }

        return totalUploaded;
    }

    private async Task<string> FinalizeUploadAsync(string sessionId, AxwayFileMetadata metadata, CancellationToken cancellationToken)
    {
        var httpClient = CreateHttpClient();
        var finalizeRequest = new
        {
            sessionId,
            triggerWorkflow = _configuration.EnableWorkflowTriggers && !string.IsNullOrEmpty(metadata.WorkflowId)
        };

        var response = await httpClient.PostAsJsonAsync($"/uploads/{sessionId}/finalize", finalizeRequest, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<AxwayApiResponse>(cancellationToken);
        return result?.FileId ?? throw new InvalidOperationException("Failed to finalize upload");
    }

    private async Task<AxwayFileMetadata> GetFileMetadataAsync(HttpClient httpClient, string fileId, CancellationToken cancellationToken)
    {
        var response = await httpClient.GetAsync($"/files/{fileId}/metadata", cancellationToken);
        response.EnsureSuccessStatusCode();

        var metadata = await response.Content.ReadFromJsonAsync<AxwayFileMetadata>(cancellationToken);
        return metadata ?? throw new InvalidOperationException($"File not found: {fileId}");
    }

    private AxwayFileMetadata ExtractAxwayMetadata(TransferRequest request, string fileName, long fileSize, string folderPath)
    {
        return new AxwayFileMetadata
        {
            FileName = fileName,
            FileSize = fileSize,
            FolderPath = folderPath,
            ContentType = request.Metadata.TryGetValue("ContentType", out var contentType) ? contentType : null,
            WorkflowId = request.Metadata.TryGetValue("WorkflowId", out var workflowId) ? workflowId : null,
            Attributes = request.Metadata
                .Where(kvp => kvp.Key.StartsWith("Axway."))
                .ToDictionary(kvp => kvp.Key.Substring(6), kvp => kvp.Value)
        };
    }

    private string ExtractFileId(TransferUri uri)
    {
        // Support both: axway://folder/path/file.txt and axway://fileid/abc123
        var path = uri.Path.TrimStart('/');

        if (path.StartsWith("fileid/", StringComparison.OrdinalIgnoreCase))
        {
            return path.Substring(7);
        }

        // For path-based, would need to query API to get file ID
        return path;
    }

    private string GetOAuthToken()
    {
        // TODO: Implement OAuth 2.0 client credentials flow
        // This is a placeholder - in production, implement proper OAuth token acquisition
        throw new NotImplementedException("OAuth authentication not yet implemented. Use API key authentication.");
    }

    private static TransferErrorCode MapExceptionToErrorCode(Exception ex)
    {
        return ex switch
        {
            HttpRequestException => TransferErrorCode.NetworkError,
            TimeoutException => TransferErrorCode.TimeoutExpired,
            UnauthorizedAccessException => TransferErrorCode.AuthenticationFailed,
            _ => TransferErrorCode.Unknown
        };
    }

    private static bool IsTransientError(Exception ex)
    {
        return ex is HttpRequestException or TimeoutException or IOException;
    }
}
