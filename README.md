# FileTransfer Library - Production-Ready .NET 8 File Transfer Ecosystem

A modular, enterprise-grade file transfer library for .NET 8 with Clean Architecture principles, supporting multiple protocols (HTTP/HTTPS, FTP, SFTP, SCP, WebSocket) with comprehensive features including encryption, compression, progress tracking, resume capability, and audit trails.

## Features

- **Multiple Protocol Support**: HTTP/HTTPS, FTP, SFTP, SCP, WebSocket, Axway MFT, and extensible custom providers
- **Production-Ready**: Comprehensive error handling, retry policies, validation, and resilience
- **Security First**: Built-in encryption (AES-GCM), TLS support, credential management, and secret redaction
- **Progress Tracking**: Real-time progress reporting with callbacks and `IProgress<T>` support
- **Resumable Transfers**: Support for interrupted transfer recovery
- **Compression**: Built-in gzip, deflate, and brotli compression
- **Integrity Verification**: SHA-256, SHA-512, MD5 checksums with per-chunk validation
- **Audit Trail**: Structured logging with correlation IDs and pluggable audit sinks
- **Chunked Transfers**: Configurable chunk sizes with parallel transfer support
- **Factory Pattern**: Clean provider selection based on URI scheme
- **Dependency Injection**: Full DI support with fluent configuration API
- **OpenTelemetry**: Built-in ActivitySource for distributed tracing
- **Health Checks**: ASP.NET Core health check integration
- **Cross-Platform**: Runs on Windows, Linux, and macOS

## Quick Start

### Installation

```bash
# Core library and factory
dotnet add package FileTransfer.Factory

# Add providers as needed
dotnet add package FileTransfer.Providers.Http
dotnet add package FileTransfer.Providers.Ftp
dotnet add package FileTransfer.Providers.Sftp
dotnet add package FileTransfer.Providers.Scp
dotnet add package FileTransfer.Providers.WebSocket
dotnet add package FileTransfer.Providers.Axway
```

### Basic Usage

#### Console Application

```csharp
using FileTransfer.Core.Abstractions;
using FileTransfer.Core.Models;
using FileTransfer.Factory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Setup DI
var services = new ServiceCollection();
services.AddLogging(builder => builder.AddConsole());
services.AddFileTransfer()
    .AddHttpProvider()
    .AddFtpProvider()
    .AddSftpProvider()
    .AddAxwayProvider(config =>
    {
        config.ApiBaseUrl = "https://your-instance.axway.com/api/v2";
        config.AuthType = AxwayAuthType.ApiKey;
        config.ApiKey = "your-api-key";
    });

var serviceProvider = services.BuildServiceProvider();
var transferClient = serviceProvider.GetRequiredService<IFileTransferClient>();

// Upload a file
var uploadRequest = new TransferRequest
{
    Source = @"C:\local\file.txt",
    Destination = "https://api.example.com/files/file.txt",
    Options = new TransferOptions
    {
        VerifyChecksum = true,
        EnableCompression = false,
        Overwrite = true
    },
    Credentials = TransferCredentials.CreateBearerToken("your-api-token"),
    Progress = new Progress<TransferProgress>(progress =>
    {
        Console.WriteLine($"Progress: {progress.PercentComplete:F1}% " +
                         $"({progress.BytesTransferred:N0}/{progress.TotalBytes:N0} bytes) " +
                         $"Rate: {progress.RateBytesPerSecond / 1024 / 1024:F2} MB/s");
    })
};

var result = await transferClient.UploadAsync(uploadRequest);

if (result.Success)
{
    Console.WriteLine($"Upload completed! {result.BytesTransferred:N0} bytes in {result.Duration}");
    Console.WriteLine($"Checksum: {result.Checksum}");
}
else
{
    Console.WriteLine($"Upload failed: {result.Error?.Message}");
}
```

#### ASP.NET Core Web API

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddFileTransfer()
    .AddAllProviders(); // Or add specific providers

builder.Services.AddHealthChecks()
    .AddFileTransferHealthChecks();

builder.Services.AddControllers();

var app = builder.Build();
app.MapControllers();
app.MapHealthChecks("/health");
app.Run();
```

```csharp
// FileTransferController.cs
[ApiController]
[Route("api/files")]
public class FileTransferController : ControllerBase
{
    private readonly IFileTransferClient _transferClient;

    public FileTransferController(IFileTransferClient transferClient)
    {
        _transferClient = transferClient;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> Upload([FromBody] UploadRequest request)
    {
        var transferRequest = new TransferRequest
        {
            Source = request.LocalPath,
            Destination = request.RemoteUrl,
            Options = new TransferOptions { VerifyChecksum = true },
            Credentials = TransferCredentials.CreateApiKey(request.ApiKey)
        };

        var result = await _transferClient.UploadAsync(transferRequest);
        return Ok(result);
    }
}
```

#### .NET Worker Service

```csharp
// Program.cs
var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddFileTransfer()
    .AddAllProviders();

builder.Services.AddHostedService<FileTransferWorker>();

var host = builder.Build();
host.Run();
```

```csharp
// FileTransferWorker.cs
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
        while (!stoppingToken.IsCancellationRequested)
        {
            // Process pending transfers
            var files = Directory.GetFiles(@"C:\TransferQueue");

            foreach (var file in files)
            {
                var request = new TransferRequest
                {
                    Source = file,
                    Destination = $"sftp://server.example.com/uploads/{Path.GetFileName(file)}",
                    Credentials = TransferCredentials.CreateUsernamePassword("user", "pass")
                };

                var result = await _transferClient.UploadAsync(request, stoppingToken);

                if (result.Success)
                {
                    _logger.LogInformation("Transferred {File}: {Bytes} bytes", file, result.BytesTransferred);
                    File.Move(file, Path.Combine(@"C:\Processed", Path.GetFileName(file)));
                }
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
```

#### WinForms Application

```csharp
public partial class MainForm : Form
{
    private readonly IFileTransferClient _transferClient;

    public MainForm()
    {
        // Setup DI
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFileTransfer().AddHttpProvider();

        var serviceProvider = services.BuildServiceProvider();
        _transferClient = serviceProvider.GetRequiredService<IFileTransferClient>();

        InitializeComponent();
    }

    private async void btnUpload_Click(object sender, EventArgs e)
    {
        var request = new TransferRequest
        {
            Source = txtLocalFile.Text,
            Destination = txtRemoteUrl.Text,
            Progress = new Progress<TransferProgress>(progress =>
            {
                // Update UI on UI thread
                Invoke(() =>
                {
                    progressBar.Value = (int)progress.PercentComplete;
                    lblStatus.Text = $"{progress.PercentComplete:F1}% - {progress.RateBytesPerSecond / 1024:F0} KB/s";
                });
            })
        };

        var result = await _transferClient.UploadAsync(request);
        MessageBox.Show(result.Success ? "Upload successful!" : $"Upload failed: {result.Error?.Message}");
    }
}
```

## Advanced Features

### Axway Managed File Transfer (MFT)

The Axway provider supports enterprise MFT solutions with REST API integration:

```csharp
// Configure Axway provider
services.AddFileTransfer()
    .AddAxwayProvider(config =>
    {
        config.ApiBaseUrl = "https://your-instance.axway.com/api/v2";
        config.AuthType = AxwayAuthType.ApiKey;
        config.ApiKey = "your-api-key";
        config.TenantId = "your-tenant-id"; // For multi-tenant instances
        config.EnableWorkflowTriggers = true;
        config.ApplicationName = "MyApp";
    });

// Upload to Axway with metadata
var request = new TransferRequest
{
    Source = "local-file.txt",
    Destination = "axway://documents/uploads/file.txt",
    Metadata = new Dictionary<string, string>
    {
        ["ContentType"] = "text/plain",
        ["WorkflowId"] = "auto-process-workflow",
        ["Axway.Department"] = "Finance",
        ["Axway.Priority"] = "High",
        ["Axway.ExpirationDays"] = "30"
    }
};

var result = await transferClient.UploadAsync(request);

// Get Axway file ID from result
var axwayFileId = result.Metadata["AxwayFileId"];

// Download from Axway using file ID
var downloadRequest = new TransferRequest
{
    Source = $"axway://fileid/{axwayFileId}",
    Destination = "downloaded-file.txt"
};

await transferClient.DownloadAsync(downloadRequest);
```

**Axway Features:**
- REST API integration with SecureTransport and MFT solutions
- Multipart upload for large files
- Workflow trigger support on upload
- Custom metadata attributes
- File expiration management
- OAuth and API key authentication
- Multi-tenant support

### Encryption

```csharp
var encryptionService = serviceProvider.GetRequiredService<IEncryptionService>();
var encryptionKey = encryptionService.GenerateKey(EncryptionAlgorithm.AesGcm);

var request = new TransferRequest
{
    Source = "sensitive-data.txt",
    Destination = "https://api.example.com/upload",
    Options = new TransferOptions
    {
        EnableEncryption = true,
        EncryptionAlgorithm = EncryptionAlgorithm.AesGcm,
        EncryptionKey = encryptionKey
    }
};
```

### Compression

```csharp
var request = new TransferRequest
{
    Source = "large-file.log",
    Destination = "https://api.example.com/upload",
    Options = new TransferOptions
    {
        EnableCompression = true,
        CompressionAlgorithm = CompressionAlgorithm.Gzip
    }
};
```

### Retry Policy

```csharp
var retryPolicy = new RetryPolicy
{
    MaxAttempts = 5,
    InitialDelay = TimeSpan.FromSeconds(2),
    MaxDelay = TimeSpan.FromSeconds(60),
    BackoffMultiplier = 2.0,
    UseExponentialBackoff = true,
    UseJitter = true
};

// Register custom policy
services.AddSingleton<ITransferPolicy>(new DefaultTransferPolicy(retryPolicy));
```

### Event Callbacks

```csharp
var request = new TransferRequest
{
    Source = "file.txt",
    Destination = "https://api.example.com/upload",
    OnStart = (progress) => Console.WriteLine("Transfer started"),
    OnChunkSent = (progress) => Console.WriteLine($"Chunk sent: {progress.CurrentChunk}/{progress.TotalChunks}"),
    OnCompleted = (result) => Console.WriteLine($"Completed in {result.Duration}"),
    OnFailed = (error) => Console.WriteLine($"Failed: {error.Message}")
};
```

### Audit Trail

```csharp
// Configure file audit sink
services.Configure<FileAuditSinkOptions>(options =>
{
    options.FilePath = "logs/audit.log";
});

services.AddSingleton<IAuditSink, FileAuditSink>();

// Or implement custom audit sink
public class DatabaseAuditSink : IAuditSink
{
    public async Task WriteAsync(AuditEvent auditEvent, CancellationToken cancellationToken)
    {
        // Save to database
        await _dbContext.AuditEvents.AddAsync(auditEvent, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
```

### Custom Provider

```csharp
public class AzureBlobProvider : BaseTransferProvider
{
    public override string Scheme => "azblob";

    public override async Task<TransferResult> UploadAsync(TransferRequest request, CancellationToken cancellationToken)
    {
        // Implement Azure Blob Storage upload
        var blobClient = new BlobClient(request.Destination);
        using var fileStream = File.OpenRead(request.Source);
        await blobClient.UploadAsync(fileStream, cancellationToken);
        // ... return result
    }

    // Implement DownloadAsync and GetCapabilities
}

// Register custom provider
services.AddFileTransfer()
    .AddCustomProvider<AzureBlobProvider>();
```

## Configuration

### appsettings.json

```json
{
  "FileTransfer": {
    "DefaultTransferOptions": {
      "ChunkSize": 4194304,
      "MaxRetries": 3,
      "VerifyChecksum": true,
      "ChecksumAlgorithm": "Sha256",
      "MinimumTlsVersion": "Tls12"
    },
    "EnableTelemetry": true,
    "EnableDetailedLogging": false
  },
  "FileAuditSink": {
    "FilePath": "logs/audit.log"
  }
}
```

### Programmatic Configuration

```csharp
services.AddFileTransfer(options =>
{
    options.EnableTelemetry = true;
    options.EnableDetailedLogging = true;
    options.DefaultTransferOptions = new TransferOptions
    {
        ChunkSize = 8 * 1024 * 1024, // 8MB chunks
        MaxRetries = 5,
        VerifyChecksum = true,
        ChecksumAlgorithm = ChecksumAlgorithm.Sha256
    };
});
```

## Architecture

### Project Structure

```
src/
├── FileTransfer.Core            - Core abstractions and models
├── FileTransfer.Security        - Encryption, credentials, secret redaction
├── FileTransfer.Compression     - Compression services
├── FileTransfer.Auditing        - Audit events and sinks
├── FileTransfer.Providers.Http  - HTTP/HTTPS provider
├── FileTransfer.Providers.Ftp   - FTP provider
├── FileTransfer.Providers.Sftp  - SFTP provider
├── FileTransfer.Providers.Scp   - SCP provider
├── FileTransfer.Providers.WebSocket - WebSocket provider
├── FileTransfer.Providers.Custom    - Custom provider template
├── FileTransfer.Factory         - Factory and DI registration
└── FileTransfer.Hosting         - ASP.NET Core integrations
```

### Key Interfaces

- **`IFileTransferClient`**: Main client interface for uploads/downloads
- **`ITransferProvider`**: Protocol-specific provider interface
- **`ITransferProviderFactory`**: Factory for selecting providers by scheme
- **`ICredentialProvider`**: Credential management
- **`IEncryptionService`**: Encryption/decryption operations
- **`ICompressionService`**: Compression/decompression operations
- **`IIntegrityService`**: Checksum calculation and verification
- **`IAuditSink`**: Audit event persistence

## Security Considerations

### Secret Redaction

The library automatically redacts secrets in logs:

```csharp
using FileTransfer.Security;

var safeLog = SecretRedactor.RedactSecrets("password=mysecret123");
// Output: "password=***REDACTED***"
```

### TLS Configuration

```csharp
var options = new TransferOptions
{
    MinimumTlsVersion = TlsVersion.Tls13,
    AllowedHosts = new HashSet<string> { "api.example.com", "secure.example.com" },
    DeniedHosts = new HashSet<string> { "untrusted.com" },
    PreventPathTraversal = true
};
```

### File Validation

```csharp
var options = new TransferOptions
{
    MaxFileSize = 100 * 1024 * 1024, // 100MB limit
    AllowedExtensions = new HashSet<string> { ".txt", ".pdf", ".docx" },
    PreventPathTraversal = true
};
```

## Testing

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true /p:CoverageReporter=lcov
```

### Provider Contract Tests

```csharp
public class HttpProviderTests : ProviderTestBase
{
    protected override ITransferProvider CreateProvider()
    {
        return new HttpTransferProvider(/* dependencies */);
    }

    [Fact]
    public async Task Upload_SmallFile_Succeeds()
    {
        var provider = CreateProvider();
        var result = await provider.UploadAsync(CreateTestRequest());
        Assert.True(result.Success);
    }
}
```

## Performance

- **Streaming**: Zero-copy streaming for large files
- **Chunking**: Configurable chunk sizes (default 4MB)
- **Parallelism**: Concurrent chunk transfers when supported
- **Buffer Pooling**: Efficient memory usage with `ArrayPool<byte>`
- **Async/Await**: Fully asynchronous with proper cancellation support

## Telemetry and Monitoring

### OpenTelemetry Integration

```csharp
services.AddOpenTelemetry()
    .WithTracing(builder =>
    {
        builder.AddSource("FileTransfer.Core");
    });
```

### Metrics

- Transfer duration
- Bytes transferred
- Transfer rate
- Success/failure counts
- Retry attempts

## Dependencies

### Core Dependencies
- `Microsoft.Extensions.Logging.Abstractions` (8.0.0)
- `Microsoft.Extensions.DependencyInjection.Abstractions` (8.0.0)
- `System.Diagnostics.DiagnosticSource` (8.0.0)

### Provider-Specific Dependencies
- **FTP**: `FluentFTP` (49.0.2) - Robust FTP/FTPS library
- **SFTP/SCP**: `SSH.NET` (2024.1.0) - Industry-standard SSH library
- **Axway**: Uses built-in `HttpClient` for REST API integration

All dependencies are carefully selected and justified:
- FluentFTP: Robust, actively maintained FTP/FTPS library
- SSH.NET: Industry-standard SSH library for .NET

## Contributing

1. Fork the repository
2. Create a feature branch
3. Add tests for new functionality
4. Ensure all tests pass
5. Submit a pull request

## License

MIT License - see LICENSE file for details

## Support

- Documentation: https://github.com/your-org/file-transfer/wiki
- Issues: https://github.com/your-org/file-transfer/issues
- Discussions: https://github.com/your-org/file-transfer/discussions

## Roadmap

- [ ] Azure Blob Storage provider
- [ ] AWS S3 provider
- [ ] Google Cloud Storage provider
- [ ] BitTorrent provider
- [ ] Rate limiting
- [ ] Circuit breaker pattern
- [ ] Background queue for batch transfers
- [ ] Differential sync
- [ ] Multi-part upload for large files

## Credits

Built with modern .NET 8 and Clean Architecture principles by the FileTransfer team.
