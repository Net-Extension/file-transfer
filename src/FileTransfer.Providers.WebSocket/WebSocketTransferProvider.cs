using System.Net.WebSockets;
using System.Text;
using FileTransfer.Core.Abstractions;
using FileTransfer.Core.Models;
using FileTransfer.Core.Providers;
using Microsoft.Extensions.Logging;

namespace FileTransfer.Providers.WebSocket;

/// <summary>
/// WebSocket transfer provider for streaming file transfers.
/// Uses a custom framing protocol: [HEADER][CHUNK_SIZE][DATA]
/// </summary>
public sealed class WebSocketTransferProvider : BaseTransferProvider
{
    private const int MaxChunkSize = 64 * 1024; // 64KB chunks for WebSocket

    public override string Scheme => "ws";

    public WebSocketTransferProvider(
        ILogger<WebSocketTransferProvider> logger,
        IIntegrityService integrityService,
        ITransferValidator validator)
        : base(logger, integrityService, validator)
    {
    }

    public override async Task<TransferResult> UploadAsync(TransferRequest request, CancellationToken cancellationToken = default)
    {
        var startTime = DateTimeOffset.UtcNow;
        using var activity = StartActivity("WebSocket.Upload", request.CorrelationId);

        try
        {
            var destinationUri = TransferUri.Parse(request.Destination);
            ValidateHost(destinationUri.Host, request.Options);

            var fileSize = await GetFileSizeAsync(request.Source, cancellationToken);

            using var ws = new ClientWebSocket();

            // Add authentication if provided
            if (request.Credentials?.Type == CredentialType.BearerToken)
            {
                ws.Options.SetRequestHeader("Authorization", $"Bearer {request.Credentials.Token}");
            }

            var wsUri = new Uri(destinationUri.FullUri.Replace("ws://", "ws://"));
            await ws.ConnectAsync(wsUri, cancellationToken);

            // Send metadata header
            var metadata = new
            {
                fileName = Path.GetFileName(request.Source),
                fileSize,
                correlationId = request.CorrelationId
            };
            var metadataJson = System.Text.Json.JsonSerializer.Serialize(metadata);
            var metadataBytes = Encoding.UTF8.GetBytes(metadataJson);
            await ws.SendAsync(new ArraySegment<byte>(metadataBytes), WebSocketMessageType.Text, endOfMessage: true, cancellationToken);

            // Send file in chunks
            long totalSent = 0;
            using var fileStream = new FileStream(request.Source, FileMode.Open, FileAccess.Read, FileShare.Read);
            var buffer = new byte[MaxChunkSize];

            while (true)
            {
                var bytesRead = await fileStream.ReadAsync(buffer, cancellationToken);
                if (bytesRead == 0) break;

                await ws.SendAsync(new ArraySegment<byte>(buffer, 0, bytesRead), WebSocketMessageType.Binary, endOfMessage: false, cancellationToken);

                totalSent += bytesRead;

                var progress = TransferProgress.CreateInProgress(fileSize, totalSent, totalSent / (DateTimeOffset.UtcNow - startTime).TotalSeconds, request.CorrelationId);
                ReportProgress(request, progress);
            }

            // Send completion message
            await ws.SendAsync(Array.Empty<byte>(), WebSocketMessageType.Binary, endOfMessage: true, cancellationToken);

            await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Upload complete", cancellationToken);

            return new TransferResult
            {
                Success = true,
                Source = request.Source,
                Destination = request.Destination,
                BytesTransferred = totalSent,
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
            Logger.LogError(ex, "WebSocket upload failed (CorrelationId: {CorrelationId})", request.CorrelationId);

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
        using var activity = StartActivity("WebSocket.Download", request.CorrelationId);

        try
        {
            var sourceUri = TransferUri.Parse(request.Source);
            ValidateHost(sourceUri.Host, request.Options);

            using var ws = new ClientWebSocket();

            if (request.Credentials?.Type == CredentialType.BearerToken)
            {
                ws.Options.SetRequestHeader("Authorization", $"Bearer {request.Credentials.Token}");
            }

            var wsUri = new Uri(sourceUri.FullUri.Replace("ws://", "ws://"));
            await ws.ConnectAsync(wsUri, cancellationToken);

            using var fileStream = new FileStream(request.Destination, FileMode.Create, FileAccess.Write, FileShare.None);
            var buffer = new byte[MaxChunkSize];
            long totalReceived = 0;
            long fileSize = 0;

            while (ws.State == WebSocketState.Open)
            {
                var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Text && totalReceived == 0)
                {
                    // First message is metadata
                    var metadataJson = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    var metadata = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(metadataJson);
                    if (metadata?.ContainsKey("fileSize") == true)
                    {
                        fileSize = Convert.ToInt64(metadata["fileSize"]);
                    }
                    continue;
                }

                await fileStream.WriteAsync(buffer.AsMemory(0, result.Count), cancellationToken);
                totalReceived += result.Count;

                if (fileSize > 0)
                {
                    var progress = TransferProgress.CreateInProgress(fileSize, totalReceived, totalReceived / (DateTimeOffset.UtcNow - startTime).TotalSeconds, request.CorrelationId);
                    ReportProgress(request, progress);
                }

                if (result.EndOfMessage)
                {
                    break;
                }
            }

            await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Download complete", cancellationToken);

            return new TransferResult
            {
                Success = true,
                Source = request.Source,
                Destination = request.Destination,
                BytesTransferred = totalReceived,
                TotalBytes = fileSize > 0 ? fileSize : totalReceived,
                Duration = DateTimeOffset.UtcNow - startTime,
                CorrelationId = request.CorrelationId,
                StartTime = startTime,
                EndTime = DateTimeOffset.UtcNow,
                Protocol = Scheme
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "WebSocket download failed (CorrelationId: {CorrelationId})", request.CorrelationId);

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
            SupportsChunking = true,
            SupportsResume = false,
            SupportsChecksum = false,
            SupportsCompression = false,
            SupportsEncryption = false, // WSS would support encryption
            SupportsParallelism = false,
            SupportedCredentialTypes = new HashSet<CredentialType>
            {
                CredentialType.BearerToken
            }
        };
    }
}

/// <summary>
/// WebSocket Secure (WSS) provider.
/// </summary>
public sealed class WebSocketSecureTransferProvider : WebSocketTransferProvider
{
    public override string Scheme => "wss";

    public WebSocketSecureTransferProvider(
        ILogger<WebSocketSecureTransferProvider> logger,
        IIntegrityService integrityService,
        ITransferValidator validator)
        : base(logger, integrityService, validator)
    {
    }
}
