using Avalonia;
using ShareXMac.Models;
using ShareXMac.Services;
using ShareXMac.ViewModels;
using Xunit;

namespace ShareXMac.Tests;

public class PostCaptureViewModelTests
{
    private static readonly Lazy<bool> AvaloniaInitialized = new Lazy<bool>(() =>
    {
        try
        {
            AppBuilder.Configure<Application>()
                .UsePlatformDetect()
                .SetupWithoutStarting();
            return true;
        }
        catch
        {
            return false;
        }
    });

    // Minimal 1x1 white PNG
    private static readonly byte[] MinimalPng = new byte[]
    {
        0x89,0x50,0x4E,0x47,0x0D,0x0A,0x1A,0x0A,0x00,0x00,0x00,0x0D,
        0x49,0x48,0x44,0x52,0x00,0x00,0x00,0x01,0x00,0x00,0x00,0x01,
        0x08,0x02,0x00,0x00,0x00,0x90,0x77,0x53,0xDE,0x00,0x00,0x00,
        0x0C,0x49,0x44,0x41,0x54,0x78,0x9C,0x63,0xF8,0xFF,0xFF,0x3F,
        0x00,0x05,0xFE,0x02,0xFE,0x0D,0xEF,0x46,0xB8,0x00,0x00,0x00,
        0x00,0x49,0x45,0x4E,0x44,0xAE,0x42,0x60,0x82
    };

    public PostCaptureViewModelTests()
    {
        _ = AvaloniaInitialized.Value;
    }

    [Fact]
    public void Constructor_SetsFilePath()
    {
        var result = new CaptureResult(MinimalPng, "/tmp/test.png");
        var vm = new PostCaptureViewModel(result, new UploadService(), new AppSettings());
        Assert.Equal("/tmp/test.png", vm.FilePath);
    }

    [Fact]
    public void Constructor_CreatesThumbnailProperty()
    {
        var result = new CaptureResult(MinimalPng, "/tmp/test.png");
        var vm = new PostCaptureViewModel(result, new UploadService(), new AppSettings());
        Assert.NotNull(vm.Thumbnail);
    }

    [Fact]
    public void DismissCommand_RaisesCloseRequested()
    {
        var result = new CaptureResult(MinimalPng, "/tmp/test.png");
        var vm = new PostCaptureViewModel(result, new UploadService(), new AppSettings());
        bool raised = false;
        vm.CloseRequested += () => raised = true;
        vm.DismissCommand.Execute(null);
        Assert.True(raised);
    }

    [Fact]
    public void CopyPathCommand_DoesNotThrow()
    {
        var result = new CaptureResult(MinimalPng, "/tmp/test.png");
        var vm = new PostCaptureViewModel(result, new UploadService(), new AppSettings());
        vm.CopyPathCommand.Execute(null);
    }

    [Fact]
    public void PostCaptureViewModel_WithUploadService_HasUploadCommand()
    {
        var result = new CaptureResult(MinimalPng, "/tmp/test.png");
        var vm = new PostCaptureViewModel(result, new UploadService(), new AppSettings());
        Assert.NotNull(vm.UploadCommand);
    }

    [Fact]
    public void PostCaptureViewModel_InitialState_IsNotUploading()
    {
        var result = new CaptureResult(MinimalPng, "/tmp/test.png");
        var vm = new PostCaptureViewModel(result, new UploadService(), new AppSettings());
        Assert.False(vm.IsUploading);
        Assert.Null(vm.UploadedUrl);
    }

    [Fact]
    public void PostCaptureViewModel_CopyUrlCommand_NotNull()
    {
        var result = new CaptureResult(MinimalPng, "/tmp/test.png");
        var vm = new PostCaptureViewModel(result, new UploadService(), new AppSettings());
        Assert.NotNull(vm.CopyUrlCommand);
    }
}
