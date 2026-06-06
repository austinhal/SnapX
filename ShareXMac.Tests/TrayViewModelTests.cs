using ShareXMac.ViewModels;
using ShareXMac.Platform;
using ShareXMac.Services;
using ShareX.HelpersLib;
using Xunit;

namespace ShareXMac.Tests;

public class TrayViewModelTests
{
    [Fact]
    public void TrayViewModel_CaptureRegionCommand_IsNotNull()
    {
        var vm = new TrayViewModel(
            new StubScreenCapture(),
            new SettingsService(Path.GetTempFileName()),
            new HistoryService(Path.GetTempFileName()),
            new UploadService());
        Assert.NotNull(vm.CaptureRegionCommand);
    }

    [Fact]
    public void TrayViewModel_QuitCommand_IsNotNull()
    {
        var vm = new TrayViewModel(
            new StubScreenCapture(),
            new SettingsService(Path.GetTempFileName()),
            new HistoryService(Path.GetTempFileName()),
            new UploadService());
        Assert.NotNull(vm.QuitCommand);
    }

    [Fact]
    public void TrayViewModel_WithServices_DoesNotThrow()
    {
        string settingsFile = Path.Combine(Path.GetTempPath(), $"s-{Guid.NewGuid():N}.json");
        string historyFile = Path.Combine(Path.GetTempPath(), $"h-{Guid.NewGuid():N}.json");
        try
        {
            var vm = new TrayViewModel(
                new StubScreenCapture(),
                new SettingsService(settingsFile),
                new HistoryService(historyFile),
                new UploadService());
            Assert.NotNull(vm.CaptureRegionCommand);
        }
        finally
        {
            if (File.Exists(settingsFile)) File.Delete(settingsFile);
            if (File.Exists(historyFile)) File.Delete(historyFile);
        }
    }
}
