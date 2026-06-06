using ShareX.HelpersLib;
using ShareXMac.Services;
using ShareXMac.ViewModels;
using Xunit;

namespace ShareXMac.Tests;

public class SettingsViewModelHotkeyTests
{
    private static SettingsService MakeSvc()
        => new SettingsService(Path.Combine(Path.GetTempPath(), $"s-{Guid.NewGuid():N}.json"));

    [Fact]
    public void SettingsViewModel_LoadsEmptyHotkey_WhenNotConfigured()
    {
        var vm = new SettingsViewModel(MakeSvc());
        Assert.Equal("", vm.CaptureRegionHotkey);
    }

    [Fact]
    public void SettingsViewModel_LoadsHotkey_FromSettings()
    {
        var svc = MakeSvc();
        svc.Current.Hotkeys.CaptureRegion = new KeyCombo("Cmd+Shift", "3");
        var vm = new SettingsViewModel(svc);
        Assert.Equal("Cmd+Shift+3", vm.CaptureRegionHotkey);
    }

    [Fact]
    public void SettingsViewModel_SavesHotkey_ToSettings()
    {
        var svc = MakeSvc();
        var vm = new SettingsViewModel(svc);
        vm.CaptureRegionHotkey = "Cmd+4";
        vm.SaveCommand.Execute(null);
        Assert.Equal(new KeyCombo("Cmd", "4"), svc.Current.Hotkeys.CaptureRegion);
    }

    [Fact]
    public void SettingsViewModel_SavesNullHotkey_WhenEmpty()
    {
        var svc = MakeSvc();
        svc.Current.Hotkeys.CaptureWindow = new KeyCombo("Cmd", "2");
        var vm = new SettingsViewModel(svc);
        vm.CaptureWindowHotkey = "";
        vm.SaveCommand.Execute(null);
        Assert.Null(svc.Current.Hotkeys.CaptureWindow);
    }

    [Fact]
    public void SettingsViewModel_ClearCaptureRegionHotkeyCommand_SetsEmpty()
    {
        var svc = MakeSvc();
        svc.Current.Hotkeys.CaptureRegion = new KeyCombo("Cmd", "1");
        var vm = new SettingsViewModel(svc);
        vm.ClearCaptureRegionHotkeyCommand.Execute(null);
        Assert.Equal("", vm.CaptureRegionHotkey);
    }

    [Fact]
    public void SettingsViewModel_LoadsAllFiveHotkeys()
    {
        var svc = MakeSvc();
        svc.Current.Hotkeys.CaptureRegion    = new KeyCombo("Cmd+Shift", "1");
        svc.Current.Hotkeys.CaptureWindow    = new KeyCombo("Cmd+Shift", "2");
        svc.Current.Hotkeys.CaptureFullscreen = new KeyCombo("Cmd+Shift", "3");
        svc.Current.Hotkeys.RecordVideo      = new KeyCombo("Cmd+Shift", "5");
        svc.Current.Hotkeys.RecordGif        = new KeyCombo("Cmd+Shift", "6");
        var vm = new SettingsViewModel(svc);
        Assert.Equal("Cmd+Shift+1", vm.CaptureRegionHotkey);
        Assert.Equal("Cmd+Shift+2", vm.CaptureWindowHotkey);
        Assert.Equal("Cmd+Shift+3", vm.CaptureFullscreenHotkey);
        Assert.Equal("Cmd+Shift+5", vm.RecordVideoHotkey);
        Assert.Equal("Cmd+Shift+6", vm.RecordGifHotkey);
    }
}
