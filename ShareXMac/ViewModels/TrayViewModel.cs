using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia;

namespace ShareXMac.ViewModels;

public partial class TrayViewModel : ObservableObject
{
    [RelayCommand]
    private void CaptureRegion() { /* wired to IScreenCapture in Plan 2 */ }

    [RelayCommand]
    private void CaptureWindow() { /* wired to IScreenCapture in Plan 2 */ }

    [RelayCommand]
    private void CaptureFullscreen() { /* wired to IScreenCapture in Plan 2 */ }

    [RelayCommand]
    private void RecordVideo() { /* wired to IScreenCapture in Plan 2 */ }

    [RelayCommand]
    private void RecordGif() { /* wired to IScreenCapture in Plan 2 */ }

    [RelayCommand]
    private void OpenSettings() { /* opens SettingsWindow in Plan 3 */ }

    [RelayCommand]
    private void OpenHistory() { /* opens HistoryWindow in Plan 3 */ }

    [RelayCommand]
    private static void Quit()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime app)
            app.Shutdown();
    }
}
