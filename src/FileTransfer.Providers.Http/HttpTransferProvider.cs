using System.Diagnostics;
using System.Net.Http.Headers;
using FileTransfer.Auditing;
using FileTransfer.Compression;
using FileTransfer.Core.Abstractions;
using FileTransfer.Core.Models;
using FileTransfer.Core.Providers;
using FileTransfer.Security;
using Microsoft.Extensions.Logging;

namespace FileTransfer.Providers.Http;

/// <summary>
/// HTTP/HTTPS transfer provider with full support for chunking, resume, and streaming.
/// </summary>
public sealed class HttpTransferProvider : BaseTransferProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ICompressionService? _compressionService;
    private readonly IEncryptionService? _encryptionService;
    private readonly IAuditSink? _auditSink;

    public override string Scheme => "http";

    public HttpTransferProvider(
        IHttpClientFactory httpClientFactory,
        ILogger<HttpTransferProvider> logger,
        IIntegrityService integrityService,
        ITransferValidator validator,
        ICompressionService? compressionService = null,
        IEncryptionService? encryptionService = null,
        IAuditSink? auditSink = null)
        : base(logger, integrityService, validator)
    {
        _httpClientFactory = httpClientFactory;
        _compressionService = compressionService;
        _encryptionService = encryptionService;
        _auditSink = auditSink;
    }

    public override async Task<TransferResult> UploadAsync(TransferRequest request, CancellationToken cancellationToken = default)
    {
        var startTime = DateTimeOffset.UtcNow;
        using var activity = StartActivity("HTTP.Upload", request.CorrelationId);

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

            var destinationUri = TransferUri.Parse(request.Destination);
            ValidateHost(destinationUri.Host, request.Options);
            ValidateFileExtension(request.Source, request.Options);

            // Audit: Transfer started
            if (_auditSink != null)
            {
                await _auditSink.WriteAsync(AuditEvent.CreateStarted(request, Scheme, fileSize), cancellationToken);
            }

            // Report start
            var startProgress = TransferProgress.CreateStarted(fileSize, request.CorrelationId);
            ReportProgress(request, startProgress);
            InvokeCallback(request.OnStart, startProgress, "OnStart", request.CorrelationId);

            long bytesTransferred = 0;
            string? checksum = null;
            var stopwatch = Stopwatch.StartNew();

            using var fileStream = new FileStream(request.Source, FileMode.Open, FileAccess.Read, FileShare.Read, request.Options.BufferSize, useAsync: true);

            Stream uploadStream = fileStream;

            // Apply compression if enabled
            if (request.Options.EnableCompression && _compressionService != null)
            {
                var compressedStream = new MemoryStream();
                await _compressionService.CompressAsync(fileStream, compressedStream, request.Options.CompressionAlgorithm, cancellationToken);
                compressedStream.Position = 0;
                uploadStream = compressedStream;
            }

            // Apply encryption if enabled
            if (request.Options.EnableEncryption && _encryptionService != null)
            {
                var encryptedStream = new MemoryStream();
                await _encryptionService.EncryptAsync(uploadStream, encryptedStream, request.Options.EncryptionAlgorithm, request.Options.EncryptionKey!, cancellationToken);
                encryptedStream.Position = 0;
                uploadStream = encryptedStream;
            }

            // Perform upload
            var httpClient = _httpClientFactory.CreateClient("FileTransfer");
            ConfigureHttpClient(httpClient, request);

            if (request.Options.ChunkSize < fileSize && request.Options.DegreeOfParallelism == 1)
            {
                // Chunked upload
                bytesTransferred = await UploadChunkedAsync(httpClient, uploadStream, request, destinationUri, fileSize, cancellationToken);
            }
            else
            {
                // Single upload
                using var content = new StreamContent(uploadStream, request.Options.BufferSize);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                var progressStream = new ProgressStream(uploadStream, fileSize, progress =>
                {
                    var transferProgress = TransferProgress.CreateInProgress(fileSize, progress.BytesTransferred, progress.RateBytesPerSecond, request.CorrelationId);
                    ReportProgress(request, transferProgress);
                });

                using var progressContent = new StreamContent(progressStream, request.Options.BufferSize);
                var response = await httpClient.PutAsync(destinationUri.FullUri, progressContent, cancellationToken);
                response.EnsureSuccessStatusCode();

                bytesTransferred = fileSize;
            }

            // Compute checksum if enabled
            if (request.Options.VerifyChecksum)
            {
                fileStream.Position = 0;
                checksum = await IntegrityService.ComputeChecksumAsync(fileStream, request.Options.ChecksumAlgorithm, cancellationToken);
            }

            stopwatch.Stop();

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
                Protocol = Scheme
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

            Logger.LogInformation("HTTP upload completed: {BytesTransferred} bytes in {Duration}ms (CorrelationId: {CorrelationId})",
                bytesTransferred, stopwatch.ElapsedMilliseconds, request.CorrelationId);

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

            Logger.LogError(ex, "HTTP upload failed (CorrelationId: {CorrelationId})", request.CorrelationId);

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
        using var activity = StartActivity("HTTP.Download", request.CorrelationId);

        try
        {
            // Validate request
            var validationResult = await ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                throw new InvalidOperationException($"Validation failed: {string.Join(", ", validationResult.Errors.Select(e => e.Message))}");
            }

            var sourceUri = TransferUri.Parse(request.Source);
            ValidateHost(sourceUri.Host, request.Options);

            var httpClient = _httpClientFactory.CreateClient("FileTransfer");
            ConfigureHttpClient(httpClient, request);

            // Get content length
            var headRequest = new HttpRequestMessage(HttpMethod.Head, sourceUri.FullUri);
            var headResponse = await httpClient.SendAsync(headRequest, cancellationToken);
            headResponse.EnsureSuccessStatusCode();

            var contentLength = headResponse.Content.Headers.ContentLength ?? 0;
            await ValidateFileSizeAsync(contentLength, request.Options, cancellationToken);

            // Audit: Transfer started
            if (_auditSink != null)
            {
                await _auditSink.WriteAsync(AuditEvent.CreateStarted(request, Scheme, contentLength), cancellationToken);
            }

            // Report start
            var startProgress = TransferProgress.CreateStarted(contentLength, request.CorrelationId);
            ReportProgress(request, startProgress);
            InvokeCallback(request.OnStart, startProgress, "OnStart", request.CorrelationId);

            var stopwatch = Stopwatch.StartNew();

            // Download
            var response = await httpClient.GetAsync(sourceUri.FullUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            using var downloadStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var fileStream = new FileStream(request.Destination, FileMode.Create, FileAccess.Write, FileShare.None, request.Options.BufferSize, useAsync: true);

            // Progress tracking
            long bytesRead = 0;
            var buffer = new byte[request.Options.BufferSize];
            int read;

            while ((read = await downloadStream.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                bytesRead += read;

                var progress = TransferProgress.CreateInProgress(contentLength, bytesRead, bytesRead / stopwatch.Elapsed.TotalSeconds, request.CorrelationId);
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
                TotalBytes = contentLength,
                Duration = stopwatch.Elapsed,
                Checksum = checksum,
                ChecksumAlgorithm = request.Options.VerifyChecksum ? request.Options.ChecksumAlgorithm : null,
                CorrelationId = request.CorrelationId,
                StartTime = startTime,
                EndTime = DateTimeOffset.UtcNow,
                Protocol = Scheme
            };

            // Report completion
            var completedProgress = TransferProgress.CreateCompleted(contentLength, request.CorrelationId);
            ReportProgress(request, completedProgress);
            InvokeCallback(request.OnCompleted, result, "OnCompleted", request.CorrelationId);

            // Audit: Transfer completed
            if (_auditSink != null)
            {
                await _auditSink.WriteAsync(AuditEvent.CreateCompleted(result), cancellationToken);
            }

            Logger.LogInformation("HTTP download completed: {BytesTransferred} bytes in {Duration}ms (CorrelationId: {CorrelationId})",
                bytesRead, stopwatch.ElapsedMilliseconds, request.CorrelationId);

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

            Logger.LogError(ex, "HTTP download failed (CorrelationId: {CorrelationId})", request.CorrelationId);

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
                CompressionAlgorithm.Deflate,
                CompressionAlgorithm.Brotli
            },
            SupportedEncryptionAlgorithms = new HashSet<EncryptionAlgorithm>
            {
                EncryptionAlgorithm.AesGcm
            },
            SupportedCredentialTypes = new HashSet<CredentialType>
            {
                CredentialType.BearerToken,
                CredentialType.ApiKey,
                CredentialType.UsernamePassword
            }
        };
    }

    private async Task<long> UploadChunkedAsync(HttpClient client, Stream stream, TransferRequest request, TransferUri destinationUri, long totalSize, CancellationToken cancellationToken)
    {
        long totalBytesTransferred = 0;
        var chunkSize = request.Options.ChunkSize;
        var totalChunks = (int)Math.Ceiling((double)totalSize / chunkSize);
        var buffer = new byte[chunkSize];

        for (int chunkIndex = 0; chunkIndex < totalChunks; chunkIndex++)
        {
            var bytesRead = await stream.ReadAsync(buffer, 0, chunkSize, cancellationToken);
            if (bytesRead == 0) break;

            using var content = new ByteArrayContent(buffer, 0, bytesRead);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            content.Headers.ContentRange = new ContentRangeHeaderValue(totalBytesTransferred, totalBytesTransferred + bytesRead - 1, totalSize);

            var response = await client.PutAsync($"{destinationUri.FullUri}?chunk={chunkIndex}", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            totalBytesTransferred += bytesRead;

            var progress = TransferProgress.CreateInProgress(totalSize, totalBytesTransferred, totalBytesTransferred / chunkIndex + 1, request.CorrelationId)
                with
            {
                CurrentChunk = chunkIndex + 1,
                TotalChunks = totalChunks
            };

            ReportProgress(request, progress);
            InvokeCallback(request.OnChunkSent, progress, "OnChunkSent", request.CorrelationId);
        }

        return totalBytesTransferred;
    }

    private void ConfigureHttpClient(HttpClient httpClient, TransferRequest request)
    {
        if (request.Options.Timeout.HasValue)
        {
            httpClient.Timeout = request.Options.Timeout.Value;
        }

        if (request.Credentials != null)
        {
            switch (request.Credentials.Type)
            {
                case CredentialType.BearerToken:
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", request.Credentials.Token);
                    break;
                case CredentialType.ApiKey:
                    httpClient.DefaultRequestHeaders.Add("X-API-Key", request.Credentials.ApiKey);
                    break;
                case CredentialType.UsernamePassword:
                    var authBytes = System.Text.Encoding.UTF8.GetBytes($"{request.Credentials.Username}:{request.Credentials.Password}");
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));
                    break;
            }
        }
    }

    private static TransferErrorCode MapExceptionToErrorCode(Exception ex)
    {
        return ex switch
        {
            HttpRequestException => TransferErrorCode.NetworkError,
            TimeoutException => TransferErrorCode.TimeoutExpired,
            OperationCanceledException => TransferErrorCode.OperationCancelled,
            UnauthorizedAccessException => TransferErrorCode.AuthorizationFailed,
            FileNotFoundException => TransferErrorCode.FileNotFound,
            _ => TransferErrorCode.Unknown
        };
    }

    private static bool IsTransientError(Exception ex)
    {
        return ex is HttpRequestException or TimeoutException or IOException;
    }
}
