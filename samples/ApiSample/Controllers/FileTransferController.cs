using FileTransfer.Core.Abstractions;
using FileTransfer.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace ApiSample.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FileTransferController : ControllerBase
{
    private readonly IFileTransferClient _transferClient;
    private readonly ILogger<FileTransferController> _logger;

    public FileTransferController(IFileTransferClient transferClient, ILogger<FileTransferController> logger)
    {
        _transferClient = transferClient;
        _logger = logger;
    }

    /// <summary>
    /// Upload a file to a remote destination.
    /// </summary>
    [HttpPost("upload")]
    [ProducesResponseType(typeof(TransferResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upload([FromBody] UploadRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var transferRequest = new TransferRequest
            {
                Source = request.SourcePath,
                Destination = request.DestinationUrl,
                Options = new TransferOptions
                {
                    VerifyChecksum = request.VerifyChecksum,
                    Overwrite = request.Overwrite
                },
                Credentials = request.ApiKey != null
                    ? TransferCredentials.CreateApiKey(request.ApiKey)
                    : null,
                Progress = new Progress<TransferProgress>(progress =>
                {
                    _logger.LogInformation("Upload progress: {Percent}% ({BytesTransferred}/{TotalBytes})",
                        progress.PercentComplete, progress.BytesTransferred, progress.TotalBytes);
                })
            };

            var result = await _transferClient.UploadAsync(transferRequest, cancellationToken);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Upload failed");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Download a file from a remote source.
    /// </summary>
    [HttpPost("download")]
    [ProducesResponseType(typeof(TransferResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Download([FromBody] DownloadRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var transferRequest = new TransferRequest
            {
                Source = request.SourceUrl,
                Destination = request.DestinationPath,
                Options = new TransferOptions
                {
                    VerifyChecksum = request.VerifyChecksum,
                    Overwrite = request.Overwrite
                },
                Credentials = request.ApiKey != null
                    ? TransferCredentials.CreateApiKey(request.ApiKey)
                    : null,
                Progress = new Progress<TransferProgress>(progress =>
                {
                    _logger.LogInformation("Download progress: {Percent}% ({BytesTransferred}/{TotalBytes})",
                        progress.PercentComplete, progress.BytesTransferred, progress.TotalBytes);
                })
            };

            var result = await _transferClient.DownloadAsync(transferRequest, cancellationToken);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Download failed");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get capabilities of the transfer service.
    /// </summary>
    [HttpGet("capabilities")]
    public IActionResult GetCapabilities()
    {
        var capabilities = _transferClient.GetCapabilities();
        return Ok(capabilities);
    }
}

public record UploadRequest(string SourcePath, string DestinationUrl, bool VerifyChecksum = true, bool Overwrite = true, string? ApiKey = null);
public record DownloadRequest(string SourceUrl, string DestinationPath, bool VerifyChecksum = true, bool Overwrite = true, string? ApiKey = null);
