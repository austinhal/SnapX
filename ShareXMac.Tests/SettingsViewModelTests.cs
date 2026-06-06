using ShareX.HelpersLib;
using ShareXMac.Services;
using ShareXMac.ViewModels;
using Xunit;

namespace ShareXMac.Tests;

public class SettingsViewModelTests
{
    [Fact]
    public void SettingsViewModel_LoadsCurrentValues()
    {
        string tempFile = Path.Combine(Path.GetTempPath(), $"sharexmac-vm-{Guid.NewGuid():N}.json");
        try
        {
            var svc = new SettingsService(tempFile);
            svc.Current.SavePath = "/tmp/pics";
            svc.Current.AutoCopyImage = false;

            var vm = new SettingsViewModel(svc);
            Assert.Equal("/tmp/pics", vm.SavePath);
            Assert.False(vm.AutoCopyImage);
        }
        finally { if (File.Exists(tempFile)) File.Delete(tempFile); }
    }

    [Fact]
    public void SettingsViewModel_SaveCommand_PersistsValues()
    {
        string tempFile = Path.Combine(Path.GetTempPath(), $"sharexmac-vm2-{Guid.NewGuid():N}.json");
        try
        {
            var svc = new SettingsService(tempFile);
            var vm = new SettingsViewModel(svc);
            vm.SavePath = "/tmp/new-path";
            vm.AutoCopyImage = false;
            vm.SaveCommand.Execute(null);

            var svc2 = new SettingsService(tempFile);
            Assert.Equal("/tmp/new-path", svc2.Current.SavePath);
            Assert.False(svc2.Current.AutoCopyImage);
        }
        finally { if (File.Exists(tempFile)) File.Delete(tempFile); }
    }
}

public class SettingsViewModelUploadTests
{
    [Fact]
    public void SettingsViewModel_LoadsUploadValues()
    {
        string tempFile = Path.Combine(Path.GetTempPath(), $"s-up-{Guid.NewGuid():N}.json");
        try
        {
            var svc = new SettingsService(tempFile);
            svc.Current.ImgurClientId = "myclientid";
            svc.Current.AutoUploadAfterCapture = true;

            var vm = new SettingsViewModel(svc);
            Assert.Equal("myclientid", vm.ImgurClientId);
            Assert.True(vm.AutoUploadAfterCapture);
        }
        finally { if (File.Exists(tempFile)) File.Delete(tempFile); }
    }

    [Fact]
    public void SettingsViewModel_SaveCommand_PersistsUploadValues()
    {
        string tempFile = Path.Combine(Path.GetTempPath(), $"s-up2-{Guid.NewGuid():N}.json");
        try
        {
            var svc = new SettingsService(tempFile);
            var vm = new SettingsViewModel(svc);
            vm.ImgurClientId = "newid";
            vm.AutoUploadAfterCapture = true;
            vm.SaveCommand.Execute(null);

            var svc2 = new SettingsService(tempFile);
            Assert.Equal("newid", svc2.Current.ImgurClientId);
            Assert.True(svc2.Current.AutoUploadAfterCapture);
        }
        finally { if (File.Exists(tempFile)) File.Delete(tempFile); }
    }
}

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
