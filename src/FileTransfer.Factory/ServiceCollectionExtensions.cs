using FileTransfer.Auditing;
using FileTransfer.Compression;
using FileTransfer.Core.Abstractions;
using FileTransfer.Core.Services;
using FileTransfer.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FileTransfer.Factory;

/// <summary>
/// Extension methods for registering FileTransfer services with DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the FileTransfer library with all core services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Optional configuration callback.</param>
    /// <returns>A builder for configuring providers.</returns>
    public static IFileTransferBuilder AddFileTransfer(
        this IServiceCollection services,
        Action<FileTransferOptions>? configureOptions = null)
    {
        // Register core services
        services.TryAddSingleton<IIntegrityService, IntegrityService>();
        services.TryAddSingleton<ITransferValidator, TransferValidator>();
        services.TryAddSingleton<ITransferPolicy, DefaultTransferPolicy>();

        // Register security services
        services.TryAddSingleton<IEncryptionService, AesGcmEncryptionService>();
        services.TryAddSingleton<ICredentialProvider, DefaultCredentialProvider>();

        // Register compression service
        services.TryAddSingleton<ICompressionService, GzipCompressionService>();

        // Register auditing (default to file sink)
        services.TryAddSingleton<IAuditSink, FileAuditSink>();

        // Register factory and client
        services.TryAddSingleton<ITransferProviderFactory, TransferProviderFactory>();
        services.TryAddSingleton<IFileTransferClient, FileTransferClient>();

        // Register HttpClientFactory
        services.AddHttpClient("FileTransfer");

        // Configure options
        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }

        return new FileTransferBuilder(services);
    }

    /// <summary>
    /// Adds HTTP/HTTPS transfer support.
    /// </summary>
    public static IFileTransferBuilder AddHttpProvider(this IFileTransferBuilder builder)
    {
        builder.Services.AddTransient<ITransferProvider, Providers.Http.HttpTransferProvider>();
        builder.Services.AddTransient<ITransferProvider, Providers.Http.HttpsTransferProvider>();
        return builder;
    }

    /// <summary>
    /// Adds FTP transfer support.
    /// </summary>
    public static IFileTransferBuilder AddFtpProvider(this IFileTransferBuilder builder)
    {
        builder.Services.AddTransient<ITransferProvider, Providers.Ftp.FtpTransferProvider>();
        return builder;
    }

    /// <summary>
    /// Adds SFTP transfer support.
    /// </summary>
    public static IFileTransferBuilder AddSftpProvider(this IFileTransferBuilder builder)
    {
        builder.Services.AddTransient<ITransferProvider, Providers.Sftp.SftpTransferProvider>();
        return builder;
    }

    /// <summary>
    /// Adds SCP transfer support.
    /// </summary>
    public static IFileTransferBuilder AddScpProvider(this IFileTransferBuilder builder)
    {
        builder.Services.AddTransient<ITransferProvider, Providers.Scp.ScpTransferProvider>();
        return builder;
    }

    /// <summary>
    /// Adds WebSocket transfer support.
    /// </summary>
    public static IFileTransferBuilder AddWebSocketProvider(this IFileTransferBuilder builder)
    {
        builder.Services.AddTransient<ITransferProvider, Providers.WebSocket.WebSocketTransferProvider>();
        builder.Services.AddTransient<ITransferProvider, Providers.WebSocket.WebSocketSecureTransferProvider>();
        return builder;
    }

    /// <summary>
    /// Adds Axway Managed File Transfer (MFT) support.
    /// </summary>
    /// <param name="builder">The file transfer builder.</param>
    /// <param name="configureOptions">Optional configuration callback for Axway settings.</param>
    public static IFileTransferBuilder AddAxwayProvider(this IFileTransferBuilder builder, Action<Providers.Axway.AxwayConfiguration>? configureOptions = null)
    {
        builder.Services.AddHttpClient("FileTransfer.Axway");
        builder.Services.AddTransient<ITransferProvider, Providers.Axway.AxwayTransferProvider>();

        if (configureOptions != null)
        {
            builder.Services.Configure(configureOptions);
        }

        return builder;
    }

    /// <summary>
    /// Adds all available providers.
    /// </summary>
    public static IFileTransferBuilder AddAllProviders(this IFileTransferBuilder builder)
    {
        return builder
            .AddHttpProvider()
            .AddFtpProvider()
            .AddSftpProvider()
            .AddScpProvider()
            .AddWebSocketProvider();
    }

    /// <summary>
    /// Adds a custom transfer provider.
    /// </summary>
    public static IFileTransferBuilder AddCustomProvider<TProvider>(this IFileTransferBuilder builder)
        where TProvider : class, ITransferProvider
    {
        builder.Services.AddTransient<ITransferProvider, TProvider>();
        return builder;
    }
}

/// <summary>
/// Builder for configuring FileTransfer services.
/// </summary>
public interface IFileTransferBuilder
{
    IServiceCollection Services { get; }
}

internal sealed class FileTransferBuilder : IFileTransferBuilder
{
    public FileTransferBuilder(IServiceCollection services)
    {
        Services = services;
    }

    public IServiceCollection Services { get; }
}

/// <summary>
/// Configuration options for FileTransfer library.
/// </summary>
public sealed class FileTransferOptions
{
    /// <summary>
    /// Default transfer options applied to all requests.
    /// </summary>
    public TransferOptions? DefaultTransferOptions { get; set; }

    /// <summary>
    /// Whether to enable telemetry/tracing.
    /// </summary>
    public bool EnableTelemetry { get; set; } = true;

    /// <summary>
    /// Whether to enable detailed logging.
    /// </summary>
    public bool EnableDetailedLogging { get; set; }
}
