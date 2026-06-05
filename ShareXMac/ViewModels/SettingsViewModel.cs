using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShareXMac.Services;

namespace ShareXMac.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly SettingsService _service;

    [ObservableProperty] private string _savePath = "";
    [ObservableProperty] private bool _autoCopyImage;
    [ObservableProperty] private bool _showPostCaptureToolbar;
    [ObservableProperty] private int _postCaptureTimeoutSeconds;

    public event Action? CloseRequested;

    public SettingsViewModel(SettingsService service)
    {
        _service = service;
        var s = service.Current;
        SavePath = s.SavePath;
        AutoCopyImage = s.AutoCopyImage;
        ShowPostCaptureToolbar = s.ShowPostCaptureToolbar;
        PostCaptureTimeoutSeconds = s.PostCaptureToolbarTimeoutSeconds;
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
        _service.Save();
        CloseRequested?.Invoke();
    }
}
