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
    private readonly IHotkeyManager _hotkeyManager;
    private readonly OcrService _ocr;

    [ObservableProperty] private bool _isRecording;

    public string RecordVideoHeader => IsRecording ? "Stop Recording (Video)" : "Record Video";
    public string RecordGifHeader   => IsRecording ? "Stop Recording (GIF)"   : "Record GIF";

    public TrayViewModel(
        IScreenCapture capture,
        SettingsService settings,
        HistoryService history,
        UploadService upload,
        IHotkeyManager hotkeyManager,
        OcrService ocr)
    {
        _capture      = capture;
        _settings     = settings;
        _history      = history;
        _upload       = upload;
        _hotkeyManager = hotkeyManager;
        _ocr = ocr;
        settings.Saved += RegisterHotkeys;
        RegisterHotkeys();
    }

    partial void OnIsRecordingChanged(bool value)
    {
        OnPropertyChanged(nameof(RecordVideoHeader));
        OnPropertyChanged(nameof(RecordGifHeader));
    }

    private void RegisterHotkeys()
    {
        _hotkeyManager.UnregisterAll();
        var h = _settings.Current.Hotkeys;
        RegisterHotkey("capture-region",     h.CaptureRegion,    CaptureRegion);
        RegisterHotkey("capture-window",     h.CaptureWindow,    CaptureWindow);
        RegisterHotkey("capture-fullscreen", h.CaptureFullscreen, CaptureFullscreen);
        RegisterHotkey("record-video",       h.RecordVideo,      RecordVideo);
        RegisterHotkey("record-gif",         h.RecordGif,        RecordGif);
        RegisterHotkey("ocr-text",           h.OcrText,          CaptureTextOcr);
    }

    private void RegisterHotkey(string id, KeyCombo? combo, Func<Task> action)
    {
        if (combo == null) return;
        _hotkeyManager.Register(id, combo,
            () => Dispatcher.UIThread.Post(() => _ = action()));
    }

    [RelayCommand]
    private async Task CaptureRegion()
    {
        byte[]? data = await _capture.CaptureRegionAsync();
        if (data != null) await OnCaptureComplete(data, CaptureType.Region);
    }

    [RelayCommand]
    private async Task CaptureWindow()
    {
        byte[]? data = await _capture.CaptureWindowAsync();
        if (data != null) await OnCaptureComplete(data, CaptureType.Window);
    }

    [RelayCommand]
    private async Task CaptureFullscreen()
    {
        byte[]? data = await _capture.CaptureFullscreenAsync();
        if (data != null) await OnCaptureComplete(data, CaptureType.Fullscreen);
    }

    [RelayCommand]
    private async Task RecordVideo()
    {
        if (IsRecording)
        {
            await _capture.StopRecordingAsync();
            IsRecording = false;
        }
        else
        {
            string dir = _settings.Current.SavePath;
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, $"ShareX-{DateTime.Now:yyyy-MM-dd-HHmmss}.mp4");
            await _capture.StartRecordingAsync(path, RecordingFormat.MP4);
            IsRecording = true;
        }
    }

    [RelayCommand]
    private async Task RecordGif()
    {
        if (IsRecording)
        {
            await _capture.StopRecordingAsync();
            IsRecording = false;
        }
        else
        {
            string dir = _settings.Current.SavePath;
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, $"ShareX-{DateTime.Now:yyyy-MM-dd-HHmmss}.gif");
            await _capture.StartRecordingAsync(path, RecordingFormat.GIF);
            IsRecording = true;
        }
    }

    [RelayCommand]
    private async Task CaptureTextOcr()
    {
        try
        {
            string? text = await _ocr.CaptureAndRecognizeAsync();
            if (string.IsNullOrWhiteSpace(text)) return;
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var vm = new OcrResultViewModel(text);
                new OcrResultWindow(vm).Show();
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OCR failed: {ex.Message}");
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

    private ColorPickerWindow? _colorPickerWindow;

    [RelayCommand]
    private void OpenColorPicker()
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (_colorPickerWindow != null)
            {
                _colorPickerWindow.Activate();
                return;
            }
            var vm = new ColorPickerViewModel();
            _colorPickerWindow = new ColorPickerWindow(vm);
            _colorPickerWindow.Closed += (_, _) => _colorPickerWindow = null;
            _colorPickerWindow.Show();
        });
    }

    [RelayCommand]
    private static void Quit()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime app)
            app.Shutdown();
    }

    private async Task OnCaptureComplete(byte[] data, CaptureType captureType)
    {
        var wf = _settings.Current.Workflow.Resolve(captureType);

        string dir = wf.SaveFolder ?? _settings.Current.SavePath;
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, $"ShareX-{DateTime.Now:yyyy-MM-dd-HHmmss}.png");
        await File.WriteAllBytesAsync(path, data);

        string? url = null;
        if (wf.AutoUpload)
        {
            url = await _upload.UploadImageAsync(data, Path.GetFileName(path), _settings.Current);
            if (url != null)
                MacClipboard.SetText(url);
        }

        _history.AddCapture(path, url);

        if (url == null)
        {
            if (wf.AutoCopyImage)
                MacClipboard.SetImage(data);
            else
                MacClipboard.SetText(path);
        }

        if (wf.ShowToolbar)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var result = new CaptureResult(data, path);
                var vm = new PostCaptureViewModel(result, _upload, _settings.Current)
                {
                    AutoDismissSeconds = _settings.Current.PostCaptureToolbarTimeoutSeconds,
                    UploadedUrl = url
                };
                new PostCaptureWindow(vm).Show();
            });
        }
    }
}
