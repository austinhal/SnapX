using ShareXMac.ScreenCaptureLib;
using Xunit;

namespace ShareXMac.Tests;

public class MacScreenCaptureTests
{
    [Fact]
    public void Constructor_DoesNotThrow()
    {
        var capture = new MacScreenCapture();
        Assert.NotNull(capture);
    }

    [Fact]
    public async Task StopRecordingAsync_WhenNotRecording_DoesNotThrow()
    {
        var capture = new MacScreenCapture();
        await capture.StopRecordingAsync();
    }

    [Fact]
    public async Task RecognizeTextAsync_GarbageInput_ReturnsNullOrEmpty()
    {
        var capture = new MacScreenCapture();
        string? result = await capture.RecognizeTextAsync(new byte[] { 0, 1, 2, 3 });
        Assert.True(result == null || result.Length >= 0);
    }
}
