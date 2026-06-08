using ShareX.HelpersLib;
using ShareXMac.Models;
using ShareXMac.Platform;
using ShareXMac.Services;
using ShareXMac.ViewModels;
using Xunit;

namespace ShareXMac.Tests;

public class TrayViewModelHotkeyTests
{
    private static (TrayViewModel vm, TrackingHotkeyManager hotkeys, SettingsService svc) Make()
    {
        var svc = new SettingsService(Path.Combine(Path.GetTempPath(), $"s-{Guid.NewGuid():N}.json"));
        var hotkeys = new TrackingHotkeyManager();
        var vm = new TrayViewModel(
            new StubScreenCapture(), svc,
            new HistoryService(Path.Combine(Path.GetTempPath(), $"h-{Guid.NewGuid():N}.json")),
            new UploadService(), hotkeys,
            new OcrService(new StubScreenCapture()));
        return (vm, hotkeys, svc);
    }

    [Fact]
    public void TrayViewModel_DoesNotRegisterHotkeys_WhenAllNull()
    {
        var (_, hotkeys, _) = Make();
        Assert.Empty(hotkeys.RegisteredIds);
    }

    [Fact]
    public void TrayViewModel_RegistersHotkey_WhenCaptureRegionSet()
    {
        var (_, hotkeys, svc) = Make();
        svc.Current.Hotkeys.CaptureRegion = new KeyCombo("Cmd", "1");
        svc.Save();
        Assert.Contains("capture-region", hotkeys.RegisteredIds);
    }

    [Fact]
    public void TrayViewModel_ReregistersOnSave_ClearingPreviousHotkeys()
    {
        var (_, hotkeys, svc) = Make();
        svc.Current.Hotkeys.CaptureRegion = new KeyCombo("Cmd", "1");
        svc.Save();
        Assert.Single(hotkeys.RegisteredIds);

        svc.Current.Hotkeys.CaptureRegion = null;
        svc.Save();
        Assert.Empty(hotkeys.RegisteredIds);
    }
}

// Test double — tracks which hotkey IDs are currently registered
public class TrackingHotkeyManager : IHotkeyManager
{
    public bool IsAvailable => false;
    public List<string> RegisteredIds { get; } = new();
    public void Register(string id, KeyCombo combo, Action callback) => RegisteredIds.Add(id);
    public void Unregister(string id) => RegisteredIds.Remove(id);
    public void UnregisterAll() => RegisteredIds.Clear();
}
