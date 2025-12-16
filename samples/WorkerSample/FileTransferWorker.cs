using FileTransfer.Core.Abstractions;
using FileTransfer.Core.Models;

namespace WorkerSample;

/// <summary>
/// Example background worker that performs file transfers on a schedule.
/// </summary>
public class FileTransferWorker : BackgroundService
{
    private readonly IFileTransferClient _transferClient;
    private readonly ILogger<FileTransferWorker> _logger;

    public FileTransferWorker(IFileTransferClient transferClient, ILogger<FileTransferWorker> logger)
    {
        _transferClient = transferClient;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("FileTransferWorker started at: {time}", DateTimeOffset.Now);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Example: Process files from a watch folder
                await ProcessPendingTransfersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing transfers");
            }

            // Wait for 60 seconds before next check
            await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
        }
    }

    private async Task ProcessPendingTransfersAsync(CancellationToken cancellationToken)
    {
        // Example: Upload all files from a local folder to HTTP endpoint
        var watchFolder = @"C:\TransferQueue";
        if (!Directory.Exists(watchFolder))
        {
            _logger.LogWarning("Watch folder does not exist: {WatchFolder}", watchFolder);
            return;
        }

        var files = Directory.GetFiles(watchFolder, "*.txt");
        _logger.LogInformation("Found {FileCount} files to transfer", files.Length);

        foreach (var file in files)
        {
            try
            {
                var request = new TransferRequest
                {
                    Source = file,
                    Destination = $"https://api.example.com/files/{Path.GetFileName(file)}",
                    Options = new TransferOptions
                    {
                        VerifyChecksum = true,
                        Overwrite = true
                    },
                    Progress = new Progress<TransferProgress>(progress =>
                    {
                        _logger.LogInformation("{FileName}: {Percent}% complete",
                            Path.GetFileName(file), progress.PercentComplete);
                    })
                };

                var result = await _transferClient.UploadAsync(request, cancellationToken);

                if (result.Success)
                {
                    _logger.LogInformation("Successfully transferred {FileName} ({BytesTransferred} bytes in {Duration})",
                        Path.GetFileName(file), result.BytesTransferred, result.Duration);

                    // Move to processed folder
                    var processedFolder = Path.Combine(watchFolder, "Processed");
                    Directory.CreateDirectory(processedFolder);
                    File.Move(file, Path.Combine(processedFolder, Path.GetFileName(file)), overwrite: true);
                }
                else
                {
                    _logger.LogError("Failed to transfer {FileName}: {Error}",
                        Path.GetFileName(file), result.Error?.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error transferring file: {FileName}", Path.GetFileName(file));
            }
        }
    }
}
