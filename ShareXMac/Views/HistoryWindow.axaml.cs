using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ShareXMac.ViewModels;

namespace ShareXMac.Views;

public partial class HistoryWindow : Window
{
    public HistoryWindow(HistoryViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.CloseRequested += Close;
        this.Opened += async (_, _) => await vm.LoadItemsAsync();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
