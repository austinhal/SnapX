using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using ShareXMac.ViewModels;

namespace ShareXMac.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow(SettingsViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.CloseRequested += Close;

        // Each hotkey TextBox captures key combos on KeyDown
        this.FindControl<TextBox>("CaptureRegionBox")!.KeyDown    += (_, e) => OnHotkeyKeyDown(e, v => vm.CaptureRegionHotkey = v);
        this.FindControl<TextBox>("CaptureWindowBox")!.KeyDown    += (_, e) => OnHotkeyKeyDown(e, v => vm.CaptureWindowHotkey = v);
        this.FindControl<TextBox>("CaptureFullscreenBox")!.KeyDown += (_, e) => OnHotkeyKeyDown(e, v => vm.CaptureFullscreenHotkey = v);
        this.FindControl<TextBox>("RecordVideoBox")!.KeyDown      += (_, e) => OnHotkeyKeyDown(e, v => vm.RecordVideoHotkey = v);
        this.FindControl<TextBox>("RecordGifBox")!.KeyDown        += (_, e) => OnHotkeyKeyDown(e, v => vm.RecordGifHotkey = v);
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private static void OnHotkeyKeyDown(KeyEventArgs e, Action<string> setHotkey)
    {
        // Ignore bare modifier keypresses — wait for the real key
        if (e.Key is Key.LeftShift or Key.RightShift
                  or Key.LeftCtrl  or Key.RightCtrl
                  or Key.LeftAlt   or Key.RightAlt
                  or Key.LWin      or Key.RWin) return;

        string? keyStr = GetKeyString(e.Key);
        if (keyStr == null) return;

        var mods = new List<string>();
        if (e.KeyModifiers.HasFlag(KeyModifiers.Meta))    mods.Add("Cmd");
        if (e.KeyModifiers.HasFlag(KeyModifiers.Control)) mods.Add("Ctrl");
        if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))   mods.Add("Shift");
        if (e.KeyModifiers.HasFlag(KeyModifiers.Alt))     mods.Add("Alt");

        setHotkey(mods.Count > 0 ? $"{string.Join("+", mods)}+{keyStr}" : keyStr);
        e.Handled = true;
    }

    private static string? GetKeyString(Key key) => key switch
    {
        >= Key.A and <= Key.Z => key.ToString(),
        Key.D0 => "0", Key.D1 => "1", Key.D2 => "2", Key.D3 => "3", Key.D4 => "4",
        Key.D5 => "5", Key.D6 => "6", Key.D7 => "7", Key.D8 => "8", Key.D9 => "9",
        >= Key.F1 and <= Key.F12 => key.ToString(),
        _ => null
    };
}
