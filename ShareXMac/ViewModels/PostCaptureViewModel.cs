using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShareXMac.Models;
using ShareXMac.ScreenCaptureLib;
using System.Diagnostics;

namespace ShareXMac.ViewModels;

public partial class PostCaptureViewModel : ObservableObject
{
    public string FilePath { get; }
    public Bitmap? Thumbnail { get; private set; }
    public int AutoDismissSeconds { get; init; } = 8;
    private readonly byte[] _imageData;

    public event Action? CloseRequested;

    public PostCaptureViewModel(CaptureResult result)
    {
        FilePath = result.FilePath;
        _imageData = result.ImageData;
        TryLoadThumbnail(result.ImageData);
    }

    private void TryLoadThumbnail(byte[] imageData)
    {
        try
        {
            using var ms = new MemoryStream(imageData);
            Thumbnail = new Bitmap(ms);
        }
        catch
        {
            // Bitmap loading failed (e.g., in unit tests without Avalonia platform)
            Thumbnail = null;
        }
    }

    [RelayCommand]
    private void CopyImage() => MacClipboard.SetImage(_imageData);

    [RelayCommand]
    private void CopyPath() => MacClipboard.SetText(FilePath);

    [RelayCommand]
    private void OpenInFinder() =>
        Process.Start(new ProcessStartInfo("open", $"-R \"{FilePath}\"")
            { UseShellExecute = false });

    [RelayCommand]
    private void Dismiss() => CloseRequested?.Invoke();
}
