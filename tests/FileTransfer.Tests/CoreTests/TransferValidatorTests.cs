using FileTransfer.Core.Models;
using FileTransfer.Core.Services;
using FluentAssertions;
using Xunit;

namespace FileTransfer.Tests.CoreTests;

public class TransferValidatorTests
{
    private readonly TransferValidator _validator = new();

    [Fact]
    public async Task ValidateAsync_WithValidRequest_ReturnsSuccess()
    {
        // Arrange
        var request = new TransferRequest
        {
            Source = "test.txt",
            Destination = "https://example.com/test.txt"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateAsync_WithEmptySource_ReturnsFailure()
    {
        // Arrange
        var request = new TransferRequest
        {
            Source = "",
            Destination = "https://example.com/test.txt"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.PropertyName.Should().Be("Source");
    }

    [Fact]
    public async Task ValidateAsync_WithEncryptionEnabledButNoKey_ReturnsFailure()
    {
        // Arrange
        var request = new TransferRequest
        {
            Source = "test.txt",
            Destination = "https://example.com/test.txt",
            Options = new TransferOptions
            {
                EnableEncryption = true,
                EncryptionKey = null
            }
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == TransferErrorCode.EncryptionKeyMissing);
    }

    [Fact]
    public async Task ValidateAsync_WithPathTraversal_ReturnsFailure()
    {
        // Arrange
        var request = new TransferRequest
        {
            Source = "../../../etc/passwd",
            Destination = "https://example.com/test.txt",
            Options = new TransferOptions
            {
                PreventPathTraversal = true
            }
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == TransferErrorCode.PathTraversalDetected);
    }
}
