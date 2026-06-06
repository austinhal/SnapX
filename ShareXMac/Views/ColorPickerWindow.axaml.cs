using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using ShareXMac.ViewModels;

namespace ShareXMac.Views;

public partial class ColorPickerWindow : Window
{
    private readonly DispatcherTimer _timer;

    public ColorPickerWindow(ColorPickerViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(80) };
        _timer.Tick += (_, _) => vm.Refresh();

        Opened  += (_, _) => { vm.Refresh(); _timer.Start(); };
        Closed  += (_, _) => _timer.Stop();

        KeyDown        += OnKeyDown;
        PointerPressed += (_, _) => PickAndClose(vm);
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape) { Close(); e.Handled = true; }
        else if (e.Key == Key.Enter) { PickAndClose((ColorPickerViewModel)DataContext!); e.Handled = true; }
    }

    private bool _closing;

    private void PickAndClose(ColorPickerViewModel vm)
    {
        if (_closing) return;
        _closing = true;
        vm.CopyHexCommand.Execute(null);
        Close();
    }
}
