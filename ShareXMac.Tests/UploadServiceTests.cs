using ShareX.UploadersLib;
using ShareXMac.Models;
using ShareXMac.Services;
using Xunit;

namespace ShareXMac.Tests;

public class UploadServiceTests
{
    [Fact]
    public void UploadService_Constructor_DoesNotThrow()
    {
        var svc = new UploadService();
        Assert.NotNull(svc);
    }

    [Fact]
    public async Task UploadImageAsync_EmptyClientId_ReturnsNull()
    {
        var svc = new UploadService();
        var settings = new AppSettings { ImgurClientId = "", ActiveImageDestination = ImageDestination.Imgur };
        byte[] data = new byte[] { 0x89, 0x50, 0x4E, 0x47 }; // PNG magic bytes only
        var result = await svc.UploadImageAsync(data, "test.png", settings);
        Assert.Null(result);
    }

    [Fact]
    public async Task UploadImageAsync_EmptyData_ReturnsNull()
    {
        var svc = new UploadService();
        var settings = new AppSettings { ImgurClientId = "any-id" };
        var result = await svc.UploadImageAsync(Array.Empty<byte>(), "test.png", settings);
        Assert.Null(result);
    }
}
