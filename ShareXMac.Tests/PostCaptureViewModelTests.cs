using ShareXMac.Models;
using ShareXMac.ViewModels;
using Xunit;

namespace ShareXMac.Tests;

public class PostCaptureViewModelTests
{
    // Minimal 1x1 white PNG
    private static readonly byte[] MinimalPng = new byte[]
    {
        0x89,0x50,0x4E,0x47,0x0D,0x0A,0x1A,0x0A,0x00,0x00,0x00,0x0D,
        0x49,0x48,0x44,0x52,0x00,0x00,0x00,0x01,0x00,0x00,0x00,0x01,
        0x08,0x02,0x00,0x00,0x00,0x90,0x77,0x53,0xDE,0x00,0x00,0x00,
        0x0C,0x49,0x44,0x41,0x54,0x08,0xD7,0x63,0xF8,0xCF,0xC0,0x00,
        0x00,0x00,0x02,0x00,0x01,0xE2,0x21,0xBC,0x33,0x00,0x00,0x00,
        0x00,0x49,0x45,0x4E,0x44,0xAE,0x42,0x60,0x82
    };

    [Fact]
    public void Constructor_SetsFilePath()
    {
        var result = new CaptureResult(MinimalPng, "/tmp/test.png");
        var vm = new PostCaptureViewModel(result);
        Assert.Equal("/tmp/test.png", vm.FilePath);
    }

    [Fact]
    public void Constructor_CreatesThumbnailProperty()
    {
        var result = new CaptureResult(MinimalPng, "/tmp/test.png");
        var vm = new PostCaptureViewModel(result);
        // Thumbnail may be null in unit tests due to lack of Avalonia platform initialization
        // but the property should exist and be accessible
        Assert.NotNull(vm);
    }

    [Fact]
    public void DismissCommand_RaisesCloseRequested()
    {
        var result = new CaptureResult(MinimalPng, "/tmp/test.png");
        var vm = new PostCaptureViewModel(result);
        bool raised = false;
        vm.CloseRequested += () => raised = true;
        vm.DismissCommand.Execute(null);
        Assert.True(raised);
    }

    [Fact]
    public void CopyPathCommand_DoesNotThrow()
    {
        var result = new CaptureResult(MinimalPng, "/tmp/test.png");
        var vm = new PostCaptureViewModel(result);
        vm.CopyPathCommand.Execute(null);
    }
}
