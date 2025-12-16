using FileTransfer.Auditing;
using FileTransfer.Compression;
using FileTransfer.Core.Abstractions;
using FileTransfer.Security;
using Microsoft.Extensions.Logging;

namespace FileTransfer.Providers.Http;

/// <summary>
/// HTTPS transfer provider (inherits from HTTP provider).
/// </summary>
public sealed class HttpsTransferProvider : HttpTransferProvider
{
    public override string Scheme => "https";

    public HttpsTransferProvider(
        IHttpClientFactory httpClientFactory,
        ILogger<HttpsTransferProvider> logger,
        IIntegrityService integrityService,
        ITransferValidator validator,
        ICompressionService? compressionService = null,
        IEncryptionService? encryptionService = null,
        IAuditSink? auditSink = null)
        : base(httpClientFactory, logger, integrityService, validator, compressionService, encryptionService, auditSink)
    {
    }
}
