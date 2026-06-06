using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ShareXMac.ScreenCaptureLib;
using ShareXMac.Services;
using ShareXMac.ViewModels;

namespace ShareXMac;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        string appSupport = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ShareX-Mac");
        Directory.CreateDirectory(appSupport);

        var settings = new SettingsService(Path.Combine(appSupport, "settings.json"));
        var history  = new HistoryService(Path.Combine(appSupport, "history.json"));
        var capture  = new MacScreenCapture();
        var upload   = new UploadService();

        DataContext = new TrayViewModel(capture, settings, history, upload);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.ShutdownMode = Avalonia.Controls.ShutdownMode.OnExplicitShutdown;

        base.OnFrameworkInitializationCompleted();
    }
}
