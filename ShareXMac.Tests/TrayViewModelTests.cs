using ShareXMac.ViewModels;
using ShareXMac.Platform;
using ShareXMac.Services;
using ShareXMac.ScreenCaptureLib;
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
            new UploadService(),
            new StubHotkeyManager(),
            new OcrService(new StubScreenCapture()));
        Assert.NotNull(vm.CaptureRegionCommand);
    }

    [Fact]
    public void TrayViewModel_QuitCommand_IsNotNull()
    {
        var vm = new TrayViewModel(
            new StubScreenCapture(),
            new SettingsService(Path.GetTempFileName()),
            new HistoryService(Path.GetTempFileName()),
            new UploadService(),
            new StubHotkeyManager(),
            new OcrService(new StubScreenCapture()));
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
                new UploadService(),
                new StubHotkeyManager(),
                new OcrService(new StubScreenCapture()));
            Assert.NotNull(vm.CaptureRegionCommand);
        }
        finally
        {
            if (File.Exists(settingsFile)) File.Delete(settingsFile);
            if (File.Exists(historyFile)) File.Delete(historyFile);
        }
    }

    [Fact]
    public async Task TrayViewModel_CaptureTextOcrCommand_InvokesOcrService()
    {
        var stubOcr = new TrackingOcrService();
        var vm = new TrayViewModel(
            new StubScreenCapture(),
            new SettingsService(Path.GetTempFileName()),
            new HistoryService(Path.GetTempFileName()),
            new UploadService(),
            new StubHotkeyManager(),
            stubOcr);
        await vm.CaptureTextOcrCommand.ExecuteAsync(null);
        Assert.True(stubOcr.WasCalled);
    }
}

public class StubHotkeyManager : IHotkeyManager
{
    public bool IsAvailable => false;
    public void Register(string id, KeyCombo combo, Action callback) { }
    public void Unregister(string id) { }
    public void UnregisterAll() { }
}

internal class TrackingOcrService : OcrService
{
    public bool WasCalled { get; private set; }
    public TrackingOcrService() : base(new StubScreenCapture()) { }
    public override Task<string?> CaptureAndRecognizeAsync()
    {
        WasCalled = true;
        return Task.FromResult<string?>(null); // return null so no window opens
    }
}
