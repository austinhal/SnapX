using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShareX.UploadersLib;
using ShareXMac.Services;

namespace ShareXMac.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly SettingsService _service;
    private readonly LoginItemService _loginItems;

    [ObservableProperty] private string _savePath = "";
    [ObservableProperty] private bool _autoCopyImage;
    [ObservableProperty] private bool _showPostCaptureToolbar;
    [ObservableProperty] private int _postCaptureTimeoutSeconds;
    [ObservableProperty] private string _imgurClientId = "";
    [ObservableProperty] private ImageDestination _activeImageDestination;
    [ObservableProperty] private bool _autoUploadAfterCapture;
    [ObservableProperty] private bool _launchAtLogin;

    public event Action? CloseRequested;

    public SettingsViewModel(SettingsService service, LoginItemService? loginItems = null)
    {
        _service = service;
        _loginItems = loginItems ?? new LoginItemService();
        var s = service.Current;
        SavePath = s.SavePath;
        AutoCopyImage = s.AutoCopyImage;
        ShowPostCaptureToolbar = s.ShowPostCaptureToolbar;
        PostCaptureTimeoutSeconds = s.PostCaptureToolbarTimeoutSeconds;
        ImgurClientId = s.ImgurClientId;
        ActiveImageDestination = s.ActiveImageDestination;
        AutoUploadAfterCapture = s.AutoUploadAfterCapture;
        LaunchAtLogin = _loginItems.IsEnabled;
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
    private void Save()
    {
        _service.Current.SavePath = SavePath;
        _service.Current.AutoCopyImage = AutoCopyImage;
        _service.Current.ShowPostCaptureToolbar = ShowPostCaptureToolbar;
        _service.Current.PostCaptureToolbarTimeoutSeconds = PostCaptureTimeoutSeconds;
        _service.Current.ImgurClientId = ImgurClientId;
        _service.Current.ActiveImageDestination = ActiveImageDestination;
        _service.Current.AutoUploadAfterCapture = AutoUploadAfterCapture;
        _service.Save();

        if (LaunchAtLogin && !_loginItems.IsEnabled)
        {
            string exe = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName
                         ?? Path.Combine(AppContext.BaseDirectory, "SnapX.app", "Contents", "MacOS", "SnapX");
            _loginItems.Enable(exe);
        }
        else if (!LaunchAtLogin && _loginItems.IsEnabled)
        {
            _loginItems.Disable();
        }

        CloseRequested?.Invoke();
    }
}
