using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShareXMac.Models;
using ShareXMac.ScreenCaptureLib;
using System.Diagnostics;

namespace ShareXMac.ViewModels;

public partial class PostCaptureViewModel : ObservableObject, IDisposable
{
    public string FilePath { get; }
    public Bitmap Thumbnail { get; }
    public int AutoDismissSeconds { get; init; } = 8;
    private readonly byte[] _imageData;

    public event Action? CloseRequested;

    public PostCaptureViewModel(CaptureResult result)
    {
        FilePath = result.FilePath;
        _imageData = result.ImageData;
        using var ms = new MemoryStream(result.ImageData);
        Thumbnail = Bitmap.DecodeToWidth(ms, 360);
    }

    [RelayCommand]
    private void CopyImage() => MacClipboard.SetImage(_imageData);

    [RelayCommand]
    private void CopyPath() => MacClipboard.SetText(FilePath);

    [RelayCommand]
    private void OpenInFinder() =>
        Process.Start(new ProcessStartInfo("open")
            { UseShellExecute = false, ArgumentList = { "-R", FilePath } });

    [RelayCommand]
    private void Dismiss() => CloseRequested?.Invoke();

    public void Dispose() => Thumbnail.Dispose();
}
