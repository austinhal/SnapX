using ShareX.HelpersLib;
using ShareXMac.Models;
using ShareXMac.Services;
using ShareXMac.ViewModels;
using Xunit;

namespace ShareXMac.Tests;

public class SettingsViewModelTests
{
    [Fact]
    public void SettingsViewModel_LoadsSavePath()
    {
        string tempFile = Path.Combine(Path.GetTempPath(), $"sharexmac-vm-{Guid.NewGuid():N}.json");
        try
        {
            var svc = new SettingsService(tempFile);
            svc.Current.SavePath = "/tmp/pics";
            var vm = new SettingsViewModel(svc);
            Assert.Equal("/tmp/pics", vm.SavePath);
        }
        finally { if (File.Exists(tempFile)) File.Delete(tempFile); }
    }

    [Fact]
    public void SettingsViewModel_SaveCommand_PersistsSavePath()
    {
        string tempFile = Path.Combine(Path.GetTempPath(), $"sharexmac-vm2-{Guid.NewGuid():N}.json");
        try
        {
            var svc = new SettingsService(tempFile);
            var vm = new SettingsViewModel(svc);
            vm.SavePath = "/tmp/new-path";
            vm.SaveCommand.Execute(null);
            var svc2 = new SettingsService(tempFile);
            Assert.Equal("/tmp/new-path", svc2.Current.SavePath);
        }
        finally { if (File.Exists(tempFile)) File.Delete(tempFile); }
    }
}

public class SettingsViewModelUploadTests
{
    [Fact]
    public void SettingsViewModel_LoadsImgurClientId()
    {
        string tempFile = Path.Combine(Path.GetTempPath(), $"s-up-{Guid.NewGuid():N}.json");
        try
        {
            var svc = new SettingsService(tempFile);
            svc.Current.ImgurClientId = "myclientid";
            var vm = new SettingsViewModel(svc);
            Assert.Equal("myclientid", vm.ImgurClientId);
        }
        finally { if (File.Exists(tempFile)) File.Delete(tempFile); }
    }

    [Fact]
    public void SettingsViewModel_SaveCommand_PersistsImgurClientId()
    {
        string tempFile = Path.Combine(Path.GetTempPath(), $"s-up2-{Guid.NewGuid():N}.json");
        try
        {
            var svc = new SettingsService(tempFile);
            var vm = new SettingsViewModel(svc);
            vm.ImgurClientId = "newid";
            vm.SaveCommand.Execute(null);
            var svc2 = new SettingsService(tempFile);
            Assert.Equal("newid", svc2.Current.ImgurClientId);
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

    [Fact]
    public void SettingsViewModel_LoadsEmptyOcrTextHotkey_WhenNotConfigured()
    {
        var vm = new SettingsViewModel(MakeSvc());
        Assert.Equal("", vm.OcrTextHotkey);
    }

    [Fact]
    public void SettingsViewModel_LoadsOcrTextHotkey_FromSettings()
    {
        var svc = MakeSvc();
        svc.Current.Hotkeys.OcrText = new KeyCombo("Cmd+Shift", "T");
        var vm = new SettingsViewModel(svc);
        Assert.Equal("Cmd+Shift+T", vm.OcrTextHotkey);
    }

    [Fact]
    public void SettingsViewModel_SavesOcrTextHotkey_ToSettings()
    {
        var svc = MakeSvc();
        var vm = new SettingsViewModel(svc);
        vm.OcrTextHotkey = "Cmd+T";
        vm.SaveCommand.Execute(null);
        Assert.Equal(new KeyCombo("Cmd", "T"), svc.Current.Hotkeys.OcrText);
    }

    [Fact]
    public void SettingsViewModel_ClearOcrTextHotkeyCommand_SetsEmpty()
    {
        var svc = MakeSvc();
        svc.Current.Hotkeys.OcrText = new KeyCombo("Cmd", "T");
        var vm = new SettingsViewModel(svc);
        vm.ClearOcrTextHotkeyCommand.Execute(null);
        Assert.Equal("", vm.OcrTextHotkey);
    }
}

public class SettingsViewModelWorkflowTests
{
    private static SettingsService MakeSvc()
        => new SettingsService(Path.Combine(Path.GetTempPath(), $"s-wf-{Guid.NewGuid():N}.json"));

    [Fact]
    public void SettingsViewModel_LoadsWorkflowGlobalDefaults()
    {
        var svc = MakeSvc();
        svc.Current.Workflow.ShowToolbar   = false;
        svc.Current.Workflow.AutoCopyImage = false;
        svc.Current.Workflow.AutoUpload    = true;

        var vm = new SettingsViewModel(svc);
        Assert.False(vm.WfShowToolbar);
        Assert.False(vm.WfAutoCopyImage);
        Assert.True(vm.WfAutoUpload);
    }

    [Fact]
    public void SettingsViewModel_LoadsWorkflowOverride_NullWhenNotSet()
    {
        var vm = new SettingsViewModel(MakeSvc());
        Assert.Null(vm.WfRegionShowToolbar);
        Assert.Null(vm.WfWindowAutoCopyImage);
        Assert.Null(vm.WfFullscreenAutoUpload);
    }

    [Fact]
    public void SettingsViewModel_LoadsWorkflowOverride_WhenSet()
    {
        var svc = MakeSvc();
        svc.Current.Workflow.FullscreenOverride.AutoUpload    = true;
        svc.Current.Workflow.FullscreenOverride.ShowToolbar   = false;

        var vm = new SettingsViewModel(svc);
        Assert.True(vm.WfFullscreenAutoUpload);
        Assert.False(vm.WfFullscreenShowToolbar);
    }

    [Fact]
    public void SettingsViewModel_Save_PersistsGlobalDefaults()
    {
        var svc = MakeSvc();
        var vm = new SettingsViewModel(svc);
        vm.WfShowToolbar   = false;
        vm.WfAutoUpload    = true;
        vm.WfSaveFolder    = "/custom";
        vm.SaveCommand.Execute(null);

        Assert.False(svc.Current.Workflow.ShowToolbar);
        Assert.True(svc.Current.Workflow.AutoUpload);
        Assert.Equal("/custom", svc.Current.Workflow.SaveFolder);
    }

    [Fact]
    public void SettingsViewModel_Save_PersistsPerTypeOverride()
    {
        var svc = MakeSvc();
        var vm = new SettingsViewModel(svc);
        vm.WfRegionAutoUpload   = true;
        vm.WfRegionSaveFolder   = "/region-folder";
        vm.SaveCommand.Execute(null);

        Assert.True(svc.Current.Workflow.RegionOverride.AutoUpload);
        Assert.Equal("/region-folder", svc.Current.Workflow.RegionOverride.SaveFolder);
    }

    [Fact]
    public void SettingsViewModel_Save_NullForEmptyOverride()
    {
        var svc = MakeSvc();
        svc.Current.Workflow.RegionOverride.ShowToolbar = true;
        var vm = new SettingsViewModel(svc);
        vm.WfRegionShowToolbar = null;
        vm.WfRegionSaveFolder  = "";
        vm.SaveCommand.Execute(null);

        Assert.Null(svc.Current.Workflow.RegionOverride.ShowToolbar);
        Assert.Null(svc.Current.Workflow.RegionOverride.SaveFolder);
    }

    [Fact]
    public void SettingsViewModel_Save_NullWfSaveFolder_WhenEmpty()
    {
        var svc = MakeSvc();
        svc.Current.Workflow.SaveFolder = "/old";
        var vm = new SettingsViewModel(svc);
        vm.WfSaveFolder = "";
        vm.SaveCommand.Execute(null);
        Assert.Null(svc.Current.Workflow.SaveFolder);
    }
}
