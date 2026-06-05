using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ShareXMac.ViewModels;

namespace ShareXMac.Views;

public partial class PostCaptureWindow : Window
{
    public PostCaptureWindow()
    {
        InitializeComponent();
    }

    public PostCaptureWindow(PostCaptureViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.CloseRequested += Close;
        this.Loaded += (_, _) => PositionBottomRight();

        var cts = new CancellationTokenSource();
        this.Closed += (_, _) => cts.Cancel();
        this.Closed += (_, _) => (DataContext as IDisposable)?.Dispose();
        _ = Task.Delay(TimeSpan.FromSeconds(vm.AutoDismissSeconds), cts.Token)
                .ContinueWith(t =>
                {
                    if (!t.IsCanceled)
                        Avalonia.Threading.Dispatcher.UIThread.Post(Close);
                }, TaskScheduler.Default);
    }

    private void PositionBottomRight()
    {
        var screen = Screens.Primary;
        if (screen is null) return;
        var wa = screen.WorkingArea;
        Position = new PixelPoint(
            wa.Right - (int)Width - 20,
            wa.Bottom - (int)Height - 20);
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
