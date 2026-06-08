using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShareXMac.ScreenCaptureLib;

namespace ShareXMac.ViewModels;

public partial class OcrResultViewModel : ObservableObject
{
    [ObservableProperty] private string _recognizedText;
    public int AutoDismissSeconds { get; init; } = 30;
    public event Action? CloseRequested;

    public OcrResultViewModel(string text) => _recognizedText = text;

    [RelayCommand]
    private void Copy()
    {
        MacClipboard.SetText(RecognizedText);
        CloseRequested?.Invoke();
    }

    [RelayCommand]
    private void Dismiss() => CloseRequested?.Invoke();
}
