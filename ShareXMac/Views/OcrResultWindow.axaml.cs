using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ShareXMac.ViewModels;

namespace ShareXMac.Views;

public partial class OcrResultWindow : Window
{
    public OcrResultWindow(OcrResultViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.CloseRequested += Close;

        var cts = new CancellationTokenSource();
        this.Closed += (_, _) => cts.Cancel();
        _ = Task.Delay(TimeSpan.FromSeconds(vm.AutoDismissSeconds), cts.Token)
                .ContinueWith(t =>
                {
                    if (!t.IsCanceled)
                        Avalonia.Threading.Dispatcher.UIThread.Post(Close);
                }, TaskScheduler.Default);

        this.Opened += (_, _) =>
        {
            var tb = this.FindControl<TextBox>("TextResult");
            tb?.SelectAll();
            tb?.Focus();
        };
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
