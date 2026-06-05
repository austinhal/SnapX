using CommunityToolkit.Mvvm.ComponentModel;

namespace ShareXMac.ViewModels;

public partial class AppViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = "ShareX Mac";
}
