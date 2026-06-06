using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShareX.HelpersLib;
using ShareXMac.Models;
using ShareXMac.ScreenCaptureLib;
using ShareXMac.Services;
using ShareXMac.Views;
using System.Diagnostics;

namespace ShareXMac.ViewModels;

public partial class TrayViewModel : ObservableObject
{
    private readonly IScreenCapture _capture;
    private readonly SettingsService _settings;
    private readonly HistoryService _history;
    private readonly UploadService _upload;
    private bool _isRecording;

    public TrayViewModel(
        IScreenCapture capture,
        SettingsService settings,
        HistoryService history,
        UploadService upload)
    {
        _capture = capture;
        _settings = settings;
        _history = history;
        _upload = upload;
    }

    [RelayCommand]
    private async Task CaptureRegion()
    {
        byte[]? data = await _capture.CaptureRegionAsync();
        if (data != null)
            await OnCaptureComplete(data);
    }

    [RelayCommand]
    private async Task CaptureWindow()
    {
        byte[]? data = await _capture.CaptureWindowAsync();
        if (data != null)
            await OnCaptureComplete(data);
    }

    [RelayCommand]
    private async Task CaptureFullscreen()
    {
        byte[]? data = await _capture.CaptureFullscreenAsync();
        if (data != null)
            await OnCaptureComplete(data);
    }

    [RelayCommand]
    private async Task RecordVideo()
    {
        if (_isRecording)
        {
            await _capture.StopRecordingAsync();
            _isRecording = false;
        }
        else
        {
            string dir = _settings.Current.SavePath;
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, $"ShareX-{DateTime.Now:yyyy-MM-dd-HHmmss}.mp4");
            await _capture.StartRecordingAsync(path, RecordingFormat.MP4);
            _isRecording = true;
        }
    }

    [RelayCommand]
    private async Task RecordGif()
    {
        if (_isRecording)
        {
            await _capture.StopRecordingAsync();
            _isRecording = false;
        }
        else
        {
            string dir = _settings.Current.SavePath;
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, $"ShareX-{DateTime.Now:yyyy-MM-dd-HHmmss}.gif");
            await _capture.StartRecordingAsync(path, RecordingFormat.GIF);
            _isRecording = true;
        }
    }

    [RelayCommand]
    private void OpenSettings()
    {
        Dispatcher.UIThread.Post(() =>
        {
            var vm = new SettingsViewModel(_settings);
            new SettingsWindow(vm).Show();
        });
    }

    [RelayCommand]
    private void OpenHistory()
    {
        Dispatcher.UIThread.Post(() =>
        {
            var vm = new HistoryViewModel(_history);
            new HistoryWindow(vm).Show();
        });
    }

    [RelayCommand]
    private static void Quit()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime app)
            app.Shutdown();
    }

    private async Task OnCaptureComplete(byte[] data)
    {
        string dir = _settings.Current.SavePath;
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, $"ShareX-{DateTime.Now:yyyy-MM-dd-HHmmss}.png");
        await File.WriteAllBytesAsync(path, data);

        // Auto-upload before recording history so URL is captured
        string? url = null;
        if (_settings.Current.AutoUploadAfterCapture)
        {
            url = await _upload.UploadImageAsync(data, Path.GetFileName(path), _settings.Current);
            if (url != null)
                MacClipboard.SetText(url);
        }

        _history.AddCapture(path, url);

        // Clipboard (image or path) — only if we didn't already set a URL
        if (url == null)
        {
            if (_settings.Current.AutoCopyImage)
                MacClipboard.SetImage(data);
            else
                MacClipboard.SetText(path);
        }

        if (_settings.Current.ShowPostCaptureToolbar)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var result = new CaptureResult(data, path);
                var vm = new PostCaptureViewModel(result, _upload, _settings.Current)
                {
                    AutoDismissSeconds = _settings.Current.PostCaptureToolbarTimeoutSeconds
                };
                new PostCaptureWindow(vm).Show();
            });
        }
    }
}
