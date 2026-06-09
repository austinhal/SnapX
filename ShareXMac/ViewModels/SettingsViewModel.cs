using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShareX.UploadersLib;
using ShareXMac.Models;
using ShareXMac.Services;

namespace ShareXMac.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly SettingsService _service;
    private readonly LoginItemService _loginItems;

    [ObservableProperty] private string _savePath = "";
    [ObservableProperty] private int    _postCaptureTimeoutSeconds;
    [ObservableProperty] private string _imgurClientId = "";
    [ObservableProperty] private ImageDestination _activeImageDestination;
    [ObservableProperty] private bool   _launchAtLogin;

    // Hotkeys — displayed and edited as "Modifier+Key" strings (e.g. "Cmd+Shift+3")
    [ObservableProperty] private string _captureRegionHotkey = "";
    [ObservableProperty] private string _captureWindowHotkey = "";
    [ObservableProperty] private string _captureFullscreenHotkey = "";
    [ObservableProperty] private string _recordVideoHotkey = "";
    [ObservableProperty] private string _recordGifHotkey = "";
    [ObservableProperty] private string _ocrTextHotkey = "";

    // Workflow — global defaults (non-nullable)
    [ObservableProperty] private bool   _wfShowToolbar;
    [ObservableProperty] private bool   _wfAutoCopyImage;
    [ObservableProperty] private bool   _wfAutoUpload;
    [ObservableProperty] private string _wfSaveFolder = "";  // empty = use AppSettings.SavePath

    // Workflow — Region overrides (null = inherit global)
    [ObservableProperty] private bool?   _wfRegionShowToolbar;
    [ObservableProperty] private bool?   _wfRegionAutoCopyImage;
    [ObservableProperty] private bool?   _wfRegionAutoUpload;
    [ObservableProperty] private string? _wfRegionSaveFolder;

    // Workflow — Window overrides
    [ObservableProperty] private bool?   _wfWindowShowToolbar;
    [ObservableProperty] private bool?   _wfWindowAutoCopyImage;
    [ObservableProperty] private bool?   _wfWindowAutoUpload;
    [ObservableProperty] private string? _wfWindowSaveFolder;

    // Workflow — Fullscreen overrides
    [ObservableProperty] private bool?   _wfFullscreenShowToolbar;
    [ObservableProperty] private bool?   _wfFullscreenAutoCopyImage;
    [ObservableProperty] private bool?   _wfFullscreenAutoUpload;
    [ObservableProperty] private string? _wfFullscreenSaveFolder;

    public event Action? CloseRequested;

    public SettingsViewModel(SettingsService service, LoginItemService? loginItems = null)
    {
        _service    = service;
        _loginItems = loginItems ?? new LoginItemService();
        var s = service.Current;

        SavePath                  = s.SavePath;
        PostCaptureTimeoutSeconds = s.PostCaptureToolbarTimeoutSeconds;
        ImgurClientId             = s.ImgurClientId;
        ActiveImageDestination    = s.ActiveImageDestination;
        LaunchAtLogin             = _loginItems.IsEnabled;

        var h = s.Hotkeys;
        CaptureRegionHotkey     = KeyComboHelper.ToString(h.CaptureRegion);
        CaptureWindowHotkey     = KeyComboHelper.ToString(h.CaptureWindow);
        CaptureFullscreenHotkey = KeyComboHelper.ToString(h.CaptureFullscreen);
        RecordVideoHotkey       = KeyComboHelper.ToString(h.RecordVideo);
        RecordGifHotkey         = KeyComboHelper.ToString(h.RecordGif);
        OcrTextHotkey           = KeyComboHelper.ToString(h.OcrText);

        var wf = s.Workflow;
        WfShowToolbar   = wf.ShowToolbar;
        WfAutoCopyImage = wf.AutoCopyImage;
        WfAutoUpload    = wf.AutoUpload;
        WfSaveFolder    = wf.SaveFolder ?? "";

        WfRegionShowToolbar   = wf.RegionOverride.ShowToolbar;
        WfRegionAutoCopyImage = wf.RegionOverride.AutoCopyImage;
        WfRegionAutoUpload    = wf.RegionOverride.AutoUpload;
        WfRegionSaveFolder    = wf.RegionOverride.SaveFolder;

        WfWindowShowToolbar   = wf.WindowOverride.ShowToolbar;
        WfWindowAutoCopyImage = wf.WindowOverride.AutoCopyImage;
        WfWindowAutoUpload    = wf.WindowOverride.AutoUpload;
        WfWindowSaveFolder    = wf.WindowOverride.SaveFolder;

        WfFullscreenShowToolbar   = wf.FullscreenOverride.ShowToolbar;
        WfFullscreenAutoCopyImage = wf.FullscreenOverride.AutoCopyImage;
        WfFullscreenAutoUpload    = wf.FullscreenOverride.AutoUpload;
        WfFullscreenSaveFolder    = wf.FullscreenOverride.SaveFolder;
    }

    [RelayCommand]
    private async Task Browse(Avalonia.Controls.Window? owner)
    {
        if (owner is null) return;
        var result = await owner.StorageProvider.OpenFolderPickerAsync(
            new Avalonia.Platform.Storage.FolderPickerOpenOptions
            {
                Title = "Select Save Location",
                AllowMultiple = false
            });
        if (result.Count > 0)
            SavePath = result[0].Path.LocalPath;
    }

    [RelayCommand]
    private async Task BrowseWfSaveFolder(Avalonia.Controls.Window? owner)
    {
        if (owner is null) return;
        var result = await owner.StorageProvider.OpenFolderPickerAsync(
            new Avalonia.Platform.Storage.FolderPickerOpenOptions { Title = "Select Default Save Folder", AllowMultiple = false });
        if (result.Count > 0)
            WfSaveFolder = result[0].Path.LocalPath;
    }

    [RelayCommand]
    private async Task BrowseWfRegionSaveFolder(Avalonia.Controls.Window? owner)
    {
        if (owner is null) return;
        var result = await owner.StorageProvider.OpenFolderPickerAsync(
            new Avalonia.Platform.Storage.FolderPickerOpenOptions { Title = "Select Region Save Folder", AllowMultiple = false });
        if (result.Count > 0)
            WfRegionSaveFolder = result[0].Path.LocalPath;
    }

    [RelayCommand]
    private async Task BrowseWfWindowSaveFolder(Avalonia.Controls.Window? owner)
    {
        if (owner is null) return;
        var result = await owner.StorageProvider.OpenFolderPickerAsync(
            new Avalonia.Platform.Storage.FolderPickerOpenOptions { Title = "Select Window Save Folder", AllowMultiple = false });
        if (result.Count > 0)
            WfWindowSaveFolder = result[0].Path.LocalPath;
    }

    [RelayCommand]
    private async Task BrowseWfFullscreenSaveFolder(Avalonia.Controls.Window? owner)
    {
        if (owner is null) return;
        var result = await owner.StorageProvider.OpenFolderPickerAsync(
            new Avalonia.Platform.Storage.FolderPickerOpenOptions { Title = "Select Fullscreen Save Folder", AllowMultiple = false });
        if (result.Count > 0)
            WfFullscreenSaveFolder = result[0].Path.LocalPath;
    }

    [RelayCommand] private void ClearCaptureRegionHotkey()     => CaptureRegionHotkey = "";
    [RelayCommand] private void ClearCaptureWindowHotkey()     => CaptureWindowHotkey = "";
    [RelayCommand] private void ClearCaptureFullscreenHotkey() => CaptureFullscreenHotkey = "";
    [RelayCommand] private void ClearRecordVideoHotkey()       => RecordVideoHotkey = "";
    [RelayCommand] private void ClearRecordGifHotkey()         => RecordGifHotkey = "";
    [RelayCommand] private void ClearOcrTextHotkey()           => OcrTextHotkey = "";

    [RelayCommand]
    private void Save()
    {
        _service.Current.SavePath                         = SavePath;
        _service.Current.PostCaptureToolbarTimeoutSeconds = PostCaptureTimeoutSeconds;
        _service.Current.ImgurClientId                    = ImgurClientId;
        _service.Current.ActiveImageDestination           = ActiveImageDestination;

        _service.Current.Hotkeys.CaptureRegion     = KeyComboHelper.Parse(CaptureRegionHotkey);
        _service.Current.Hotkeys.CaptureWindow     = KeyComboHelper.Parse(CaptureWindowHotkey);
        _service.Current.Hotkeys.CaptureFullscreen = KeyComboHelper.Parse(CaptureFullscreenHotkey);
        _service.Current.Hotkeys.RecordVideo       = KeyComboHelper.Parse(RecordVideoHotkey);
        _service.Current.Hotkeys.RecordGif         = KeyComboHelper.Parse(RecordGifHotkey);
        _service.Current.Hotkeys.OcrText           = KeyComboHelper.Parse(OcrTextHotkey);

        _service.Current.Workflow.ShowToolbar   = WfShowToolbar;
        _service.Current.Workflow.AutoCopyImage = WfAutoCopyImage;
        _service.Current.Workflow.AutoUpload    = WfAutoUpload;
        _service.Current.Workflow.SaveFolder    = string.IsNullOrEmpty(WfSaveFolder) ? null : WfSaveFolder;

        _service.Current.Workflow.RegionOverride.ShowToolbar   = WfRegionShowToolbar;
        _service.Current.Workflow.RegionOverride.AutoCopyImage = WfRegionAutoCopyImage;
        _service.Current.Workflow.RegionOverride.AutoUpload    = WfRegionAutoUpload;
        _service.Current.Workflow.RegionOverride.SaveFolder    = string.IsNullOrEmpty(WfRegionSaveFolder) ? null : WfRegionSaveFolder;

        _service.Current.Workflow.WindowOverride.ShowToolbar   = WfWindowShowToolbar;
        _service.Current.Workflow.WindowOverride.AutoCopyImage = WfWindowAutoCopyImage;
        _service.Current.Workflow.WindowOverride.AutoUpload    = WfWindowAutoUpload;
        _service.Current.Workflow.WindowOverride.SaveFolder    = string.IsNullOrEmpty(WfWindowSaveFolder) ? null : WfWindowSaveFolder;

        _service.Current.Workflow.FullscreenOverride.ShowToolbar   = WfFullscreenShowToolbar;
        _service.Current.Workflow.FullscreenOverride.AutoCopyImage = WfFullscreenAutoCopyImage;
        _service.Current.Workflow.FullscreenOverride.AutoUpload    = WfFullscreenAutoUpload;
        _service.Current.Workflow.FullscreenOverride.SaveFolder    = string.IsNullOrEmpty(WfFullscreenSaveFolder) ? null : WfFullscreenSaveFolder;

        _service.Save();

        if (LaunchAtLogin && !_loginItems.IsEnabled)
        {
            string exe = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName
                         ?? Path.Combine(AppContext.BaseDirectory, "SnapX");
            _loginItems.Enable(exe);
        }
        else if (!LaunchAtLogin && _loginItems.IsEnabled)
        {
            _loginItems.Disable();
        }

        CloseRequested?.Invoke();
    }
}
