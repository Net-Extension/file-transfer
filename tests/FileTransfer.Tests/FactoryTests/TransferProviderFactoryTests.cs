using FileTransfer.Core.Abstractions;
using FileTransfer.Factory;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace FileTransfer.Tests.FactoryTests;

public class TransferProviderFactoryTests
{
    [Fact]
    public void GetProvider_WithRegisteredScheme_ReturnsProvider()
    {
        // Arrange
        var mockProvider = new Mock<ITransferProvider>();
        mockProvider.Setup(p => p.Scheme).Returns("http");

        var factory = new TransferProviderFactory(
            new[] { mockProvider.Object },
            NullLogger<TransferProviderFactory>.Instance);

        // Act
        var provider = factory.GetProvider("http");

        // Assert
        provider.Should().NotBeNull();
        provider.Scheme.Should().Be("http");
    }

    [Fact]
    public void GetProvider_WithUnregisteredScheme_ThrowsNotSupportedException()
    {
        // Arrange
        var factory = new TransferProviderFactory(
            Array.Empty<ITransferProvider>(),
            NullLogger<TransferProviderFactory>.Instance);

        // Act
        Action act = () => factory.GetProvider("unknown");

        // Assert
        act.Should().Throw<NotSupportedException>()
            .WithMessage("*unknown*");
    }

    [Fact]
    public void TryGetProvider_WithRegisteredScheme_ReturnsTrue()
    {
        // Arrange
        var mockProvider = new Mock<ITransferProvider>();
        mockProvider.Setup(p => p.Scheme).Returns("ftp");

        var factory = new TransferProviderFactory(
            new[] { mockProvider.Object },
            NullLogger<TransferProviderFactory>.Instance);

        // Act
        var success = factory.TryGetProvider("ftp", out var provider);

        // Assert
        success.Should().BeTrue();
        provider.Should().NotBeNull();
        provider!.Scheme.Should().Be("ftp");
    }

    [Fact]
    public void SupportsScheme_WithRegisteredScheme_ReturnsTrue()
    {
        // Arrange
        var mockProvider = new Mock<ITransferProvider>();
        mockProvider.Setup(p => p.Scheme).Returns("sftp");

        var factory = new TransferProviderFactory(
            new[] { mockProvider.Object },
            NullLogger<TransferProviderFactory>.Instance);

        // Act
        var supports = factory.SupportsScheme("sftp");

        // Assert
        supports.Should().BeTrue();
    }
}
