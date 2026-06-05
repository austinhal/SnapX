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
        _ = Task.Delay(TimeSpan.FromSeconds(vm.AutoDismissSeconds))
                .ContinueWith(_ => Avalonia.Threading.Dispatcher.UIThread.Post(Close));
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
