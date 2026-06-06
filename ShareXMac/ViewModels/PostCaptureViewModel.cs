using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShareXMac.Models;
using ShareXMac.ScreenCaptureLib;
using ShareXMac.Services;
using System.Diagnostics;

namespace ShareXMac.ViewModels;

public partial class PostCaptureViewModel : ObservableObject, IDisposable
{
    public string FilePath { get; }
    public Bitmap Thumbnail { get; }
    public int AutoDismissSeconds { get; init; } = 8;

    [ObservableProperty] private bool _isUploading;
    [ObservableProperty] private string? _uploadedUrl;

    private readonly byte[] _imageData;
    private readonly UploadService _uploadService;
    private readonly AppSettings _settings;

    public event Action? CloseRequested;

    public PostCaptureViewModel(CaptureResult result, UploadService uploadService, AppSettings settings)
    {
        FilePath = result.FilePath;
        _imageData = result.ImageData;
        _uploadService = uploadService;
        _settings = settings;
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
    private async Task Upload()
    {
        if (IsUploading) return;
        IsUploading = true;
        try
        {
            string fileName = Path.GetFileName(FilePath);
            string? url = await _uploadService.UploadImageAsync(_imageData, fileName, _settings);
            if (url != null)
            {
                UploadedUrl = url;
                MacClipboard.SetText(url);
            }
        }
        finally
        {
            IsUploading = false;
        }
    }

    [RelayCommand]
    private void CopyUrl()
    {
        if (UploadedUrl != null)
            MacClipboard.SetText(UploadedUrl);
    }

    [RelayCommand]
    private void Dismiss() => CloseRequested?.Invoke();

    public void Dispose() => Thumbnail.Dispose();
}
