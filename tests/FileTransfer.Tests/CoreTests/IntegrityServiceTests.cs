using System.Text;
using FileTransfer.Core.Models;
using FileTransfer.Core.Services;
using FluentAssertions;
using Xunit;

namespace FileTransfer.Tests.CoreTests;

public class IntegrityServiceTests
{
    private readonly IntegrityService _service = new();

    [Fact]
    public async Task ComputeChecksumAsync_WithSha256_ReturnsCorrectHash()
    {
        // Arrange
        var content = "Hello, World!";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        // Act
        var checksum = await _service.ComputeChecksumAsync(stream, ChecksumAlgorithm.Sha256);

        // Assert
        checksum.Should().NotBeNullOrEmpty();
        checksum.Length.Should().Be(64); // SHA-256 produces 32 bytes = 64 hex chars
    }

    [Fact]
    public async Task VerifyChecksumAsync_WithMatchingChecksum_ReturnsTrue()
    {
        // Arrange
        var content = "Hello, World!";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var expectedChecksum = await _service.ComputeChecksumAsync(stream, ChecksumAlgorithm.Sha256);

        // Reset stream
        stream.Position = 0;

        // Act
        var isValid = await _service.VerifyChecksumAsync(stream, expectedChecksum, ChecksumAlgorithm.Sha256);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public async Task CountRecordsAsync_WithMultipleLines_ReturnsCorrectCount()
    {
        // Arrange
        var content = "Line 1\nLine 2\nLine 3\n";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        // Act
        var count = await _service.CountRecordsAsync(stream);

        // Assert
        count.Should().Be(3);
    }
}
