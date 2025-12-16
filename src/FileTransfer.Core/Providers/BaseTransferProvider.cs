using System.Diagnostics;
using FileTransfer.Core.Abstractions;
using FileTransfer.Core.Models;
using Microsoft.Extensions.Logging;

namespace FileTransfer.Core.Providers;

/// <summary>
/// Base class for transfer providers with common functionality.
/// </summary>
public abstract class BaseTransferProvider : ITransferProvider
{
    protected readonly ILogger Logger;
    protected readonly IIntegrityService IntegrityService;
    protected readonly ITransferValidator Validator;

    protected BaseTransferProvider(
        ILogger logger,
        IIntegrityService integrityService,
        ITransferValidator validator)
    {
        Logger = logger;
        IntegrityService = integrityService;
        Validator = validator;
    }

    public abstract string Scheme { get; }

    public abstract Task<TransferResult> UploadAsync(TransferRequest request, CancellationToken cancellationToken = default);

    public abstract Task<TransferResult> DownloadAsync(TransferRequest request, CancellationToken cancellationToken = default);

    public abstract TransferCapabilities GetCapabilities();

    public virtual async Task<ValidationResult> ValidateAsync(TransferRequest request, CancellationToken cancellationToken = default)
    {
        // Perform base validation
        var result = await Validator.ValidateAsync(request, cancellationToken);
        if (!result.IsValid)
        {
            return result;
        }

        // Provider-specific validation
        return await ValidateProviderSpecificAsync(request, cancellationToken);
    }

    protected virtual Task<ValidationResult> ValidateProviderSpecificAsync(TransferRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult(ValidationResult.Success());
    }

    protected void ReportProgress(TransferRequest request, TransferProgress progress)
    {
        try
        {
            request.Progress?.Report(progress);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Error reporting progress for correlation ID {CorrelationId}", request.CorrelationId);
        }
    }

    protected void InvokeCallback(Action? callback, string callbackName, string correlationId)
    {
        if (callback == null) return;

        try
        {
            callback();
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Error invoking {CallbackName} callback for correlation ID {CorrelationId}",
                callbackName, correlationId);
        }
    }

    protected void InvokeCallback<T>(Action<T>? callback, T argument, string callbackName, string correlationId)
    {
        if (callback == null) return;

        try
        {
            callback(argument);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Error invoking {CallbackName} callback for correlation ID {CorrelationId}",
                callbackName, correlationId);
        }
    }

    protected async Task<long> GetFileSizeAsync(string filePath, CancellationToken cancellationToken)
    {
        var fileInfo = new FileInfo(filePath);
        if (!fileInfo.Exists)
        {
            throw new FileNotFoundException("File not found.", filePath);
        }

        return await Task.FromResult(fileInfo.Length);
    }

    protected async Task ValidateFileSizeAsync(long fileSize, TransferOptions options, CancellationToken cancellationToken)
    {
        if (options.MaxFileSize.HasValue && fileSize > options.MaxFileSize.Value)
        {
            throw new InvalidOperationException(
                $"File size ({fileSize} bytes) exceeds maximum allowed size ({options.MaxFileSize.Value} bytes).");
        }

        await Task.CompletedTask;
    }

    protected void ValidateFileExtension(string filePath, TransferOptions options)
    {
        if (options.AllowedExtensions == null || options.AllowedExtensions.Count == 0)
        {
            return;
        }

        var extension = Path.GetExtension(filePath);
        if (!options.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"File extension '{extension}' is not allowed. Allowed extensions: {string.Join(", ", options.AllowedExtensions)}");
        }
    }

    protected void ValidateHost(string host, TransferOptions options)
    {
        // Check denied hosts
        if (options.DeniedHosts?.Contains(host, StringComparer.OrdinalIgnoreCase) == true)
        {
            throw new InvalidOperationException($"Host '{host}' is in the denied list.");
        }

        // Check allowed hosts
        if (options.AllowedHosts != null && options.AllowedHosts.Count > 0)
        {
            if (!options.AllowedHosts.Contains(host, StringComparer.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Host '{host}' is not in the allowed list. Allowed hosts: {string.Join(", ", options.AllowedHosts)}");
            }
        }
    }

    protected static Activity? StartActivity(string operationName, string correlationId)
    {
        var activity = FileTransferActivitySource.Source.StartActivity(operationName);
        activity?.SetTag("correlation_id", correlationId);
        return activity;
    }
}

/// <summary>
/// Activity source for distributed tracing.
/// </summary>
internal static class FileTransferActivitySource
{
    public static readonly ActivitySource Source = new("FileTransfer.Core", "1.0.0");
}
