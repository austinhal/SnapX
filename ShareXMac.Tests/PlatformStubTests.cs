using ShareXMac.Platform;
using Xunit;

namespace ShareXMac.Tests;

public class PlatformStubTests
{
    [Fact]
    public async Task StubScreenCapture_CaptureRegion_ReturnsNull()
    {
        IScreenCapture capture = new StubScreenCapture();
        var result = await capture.CaptureRegionAsync();
        Assert.Null(result);
    }

    [Fact]
    public async Task StubScreenCapture_CaptureWindow_ReturnsNull()
    {
        IScreenCapture capture = new StubScreenCapture();
        var result = await capture.CaptureWindowAsync();
        Assert.Null(result);
    }

    [Fact]
    public async Task StubScreenCapture_CaptureFullscreen_ReturnsNull()
    {
        IScreenCapture capture = new StubScreenCapture();
        var result = await capture.CaptureFullscreenAsync();
        Assert.Null(result);
    }

    [Fact]
    public async Task StubScreenCapture_RecognizeText_ReturnsNull()
    {
        IScreenCapture capture = new StubScreenCapture();
        var result = await capture.RecognizeTextAsync(Array.Empty<byte>());
        Assert.Null(result);
    }

    [Fact]
    public void StubHotkeyManager_IsAvailable_ReturnsFalse()
    {
        IHotkeyManager hotkeys = new StubHotkeyManager();
        Assert.False(hotkeys.IsAvailable);
    }

    [Fact]
    public async Task StubNotificationService_Show_DoesNotThrow()
    {
        INotificationService notifications = new StubNotificationService();
        await notifications.ShowAsync("Test", "Body");
    }

    [Fact]
    public async Task StubScreenCapture_StartRecording_DoesNotThrow()
    {
        IScreenCapture capture = new StubScreenCapture();
        await capture.StartRecordingAsync("/tmp/test.mp4", RecordingFormat.MP4);
    }
}
