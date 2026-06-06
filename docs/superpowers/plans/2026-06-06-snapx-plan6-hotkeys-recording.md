# SnapX Plan 6: Hotkeys & Recording Polish

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Wire global hotkeys end-to-end (configurable per-action, persisted in settings, registered via MacHotkeyManager with correct modifier matching), and surface recording state in the tray menu with dynamic labels.

**Architecture:** Five layers of change — (1) model: `HotkeySettings` data class + `KeyComboHelper` string converter; (2) service: `SettingsService.Saved` event so hotkeys re-register whenever settings change; (3) hotkey engine: fix `MacHotkeyManager` to match modifier flags, wire into `TrayViewModel` with a `RegisterHotkeys()` method; (4) recording state: expose `IsRecording` as an observable property with computed menu-header strings bound in `App.axaml`; (5) settings UI: Hotkeys section in `SettingsWindow` with TextBoxes that capture key-down combos.

**Tech Stack:** .NET 10, Avalonia 11.2.1, CommunityToolkit.Mvvm 8.4.0, CoreGraphics P/Invoke (`CGEventGetFlags`), Newtonsoft.Json, xUnit.

---

## File Structure

```
ShareXMac/
  Models/
    AppSettings.cs             MODIFY — add Hotkeys property
    HotkeySettings.cs          CREATE — 5 nullable KeyCombo? fields
    KeyComboHelper.cs          CREATE — ToString / Parse static helpers
  Services/
    SettingsService.cs         MODIFY — add Saved event, fire in Save()
  ViewModels/
    TrayViewModel.cs           MODIFY — add IHotkeyManager param, IsRecording observable,
                                         RecordVideoHeader / RecordGifHeader, RegisterHotkeys()
    SettingsViewModel.cs       MODIFY — add 5 hotkey string properties + 5 Clear commands
  Views/
    SettingsWindow.axaml       MODIFY — add Hotkeys section (5 rows)
    SettingsWindow.axaml.cs    MODIFY — add KeyDown handlers for hotkey capture
  App.axaml                    MODIFY — bind Record menu item headers to new properties
  App.axaml.cs                 MODIFY — instantiate MacHotkeyManager, pass to TrayViewModel

ShareXMac.ScreenCaptureLib/
  MacHotkeyManager.cs          MODIFY — add CGEventGetFlags P/Invoke, check modifier flags

ShareXMac.Tests/
  KeyComboHelperTests.cs       CREATE — 7 tests for ToString / Parse round-trip
  SettingsServiceTests.cs      MODIFY — add Saved event test
  TrayViewModelTests.cs        MODIFY — update 3 existing tests to pass StubHotkeyManager;
                                         add 3 hotkey-registration tests
  SettingsViewModelTests.cs    CREATE — load/save hotkey properties + Clear commands
```

---

### Task 1: HotkeySettings model + KeyComboHelper

Create the data model and string-conversion helpers. No UI yet.

**Files:**
- Create: `ShareXMac/Models/HotkeySettings.cs`
- Create: `ShareXMac/Models/KeyComboHelper.cs`
- Modify: `ShareXMac/Models/AppSettings.cs`
- Create: `ShareXMac.Tests/KeyComboHelperTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `ShareXMac.Tests/KeyComboHelperTests.cs`:

```csharp
using ShareX.HelpersLib;
using ShareXMac.Models;
using Xunit;

namespace ShareXMac.Tests;

public class KeyComboHelperTests
{
    [Fact]
    public void ToString_ReturnsEmpty_ForNull()
        => Assert.Equal("", KeyComboHelper.ToString(null));

    [Fact]
    public void ToString_FormatsWithModifiers()
        => Assert.Equal("Cmd+Shift+3", KeyComboHelper.ToString(new KeyCombo("Cmd+Shift", "3")));

    [Fact]
    public void ToString_FormatsWithoutModifiers()
        => Assert.Equal("F5", KeyComboHelper.ToString(new KeyCombo("", "F5")));

    [Fact]
    public void Parse_ReturnsNull_ForEmpty()
    {
        Assert.Null(KeyComboHelper.Parse(""));
        Assert.Null(KeyComboHelper.Parse(null));
    }

    [Fact]
    public void Parse_ExtractsModifiersAndKey()
    {
        var combo = KeyComboHelper.Parse("Cmd+Shift+3");
        Assert.Equal("Cmd+Shift", combo!.Modifiers);
        Assert.Equal("3", combo.Key);
    }

    [Fact]
    public void Parse_HandlesKeyWithNoModifiers()
    {
        var combo = KeyComboHelper.Parse("F5");
        Assert.Equal("", combo!.Modifiers);
        Assert.Equal("F5", combo.Key);
    }

    [Fact]
    public void RoundTrip_PreservesCombo()
    {
        var original = new KeyCombo("Cmd+Shift", "4");
        Assert.Equal(original, KeyComboHelper.Parse(KeyComboHelper.ToString(original)));
    }
}
```

- [ ] **Step 2: Run to confirm compile failure**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet test ShareXMac.Tests/ShareXMac.Tests.csproj --filter "KeyComboHelperTests" 2>&1 | tail -5
```

Expected: compile error (`KeyComboHelper` not found)

- [ ] **Step 3: Create HotkeySettings.cs**

Create `ShareXMac/Models/HotkeySettings.cs`:

```csharp
using ShareX.HelpersLib;

namespace ShareXMac.Models;

public class HotkeySettings
{
    public KeyCombo? CaptureRegion { get; set; }
    public KeyCombo? CaptureWindow { get; set; }
    public KeyCombo? CaptureFullscreen { get; set; }
    public KeyCombo? RecordVideo { get; set; }
    public KeyCombo? RecordGif { get; set; }
}
```

All are `null` by default — no hotkey configured. Users set them explicitly in Settings.

- [ ] **Step 4: Create KeyComboHelper.cs**

Create `ShareXMac/Models/KeyComboHelper.cs`:

```csharp
using ShareX.HelpersLib;

namespace ShareXMac.Models;

public static class KeyComboHelper
{
    public static string ToString(KeyCombo? combo)
    {
        if (combo == null) return "";
        return string.IsNullOrEmpty(combo.Modifiers)
            ? combo.Key
            : $"{combo.Modifiers}+{combo.Key}";
    }

    public static KeyCombo? Parse(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        int lastPlus = s.LastIndexOf('+');
        if (lastPlus < 0) return new KeyCombo("", s.Trim());
        return new KeyCombo(s[..lastPlus].Trim(), s[(lastPlus + 1)..].Trim());
    }
}
```

- [ ] **Step 5: Add Hotkeys property to AppSettings**

In `ShareXMac/Models/AppSettings.cs`, add the new property after `AutoUploadAfterCapture`:

```csharp
using ShareX.UploadersLib;

namespace ShareXMac.Models;

public class AppSettings
{
    public string SavePath { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
        "ShareX Mac");
    public bool AutoCopyImage { get; set; } = true;
    public bool ShowPostCaptureToolbar { get; set; } = true;
    public int PostCaptureToolbarTimeoutSeconds { get; set; } = 8;

    // Upload settings
    public string ImgurClientId { get; set; } = "";
    public ImageDestination ActiveImageDestination { get; set; } = ImageDestination.Imgur;
    public bool AutoUploadAfterCapture { get; set; } = false;

    // Hotkeys
    public HotkeySettings Hotkeys { get; set; } = new HotkeySettings();
}
```

- [ ] **Step 6: Run tests**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet test ShareXMac.Tests/ShareXMac.Tests.csproj --filter "KeyComboHelperTests" -v normal 2>&1 | tail -10
```

Expected: 7 passed

- [ ] **Step 7: Run all tests**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet test ShareXMac.Tests/ShareXMac.Tests.csproj 2>&1 | tail -3
```

Expected: all 55 still pass

- [ ] **Step 8: Commit**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && git add ShareXMac/Models/HotkeySettings.cs ShareXMac/Models/KeyComboHelper.cs ShareXMac/Models/AppSettings.cs ShareXMac.Tests/KeyComboHelperTests.cs && git commit -m "feat: add HotkeySettings model and KeyComboHelper"
```

---

### Task 2: SettingsService.Saved event + MacHotkeyManager modifier fix + TrayViewModel hotkey registration

Wire the hotkey engine end-to-end: fix modifier matching in the event tap, add a `Saved` event to re-register on settings change, and register hotkeys in `TrayViewModel`.

**Files:**
- Modify: `ShareXMac/Services/SettingsService.cs`
- Modify: `ShareXMac.ScreenCaptureLib/MacHotkeyManager.cs`
- Modify: `ShareXMac/ViewModels/TrayViewModel.cs`
- Modify: `ShareXMac/App.axaml.cs`
- Modify: `ShareXMac.Tests/TrayViewModelTests.cs`
- Modify: `ShareXMac.Tests/SettingsServiceTests.cs`

- [ ] **Step 1: Write tests for SettingsService.Saved and TrayViewModel hotkey registration**

Add to `ShareXMac.Tests/SettingsServiceTests.cs` (read the file first to find the right place to append):

```csharp
[Fact]
public void SettingsService_FiresSavedEvent_WhenSaveCalled()
{
    bool fired = false;
    var svc = new SettingsService(Path.GetTempFileName());
    svc.Saved += () => fired = true;
    svc.Save();
    Assert.True(fired);
}
```

Add a new file `ShareXMac.Tests/TrayViewModelHotkeyTests.cs`:

```csharp
using ShareX.HelpersLib;
using ShareXMac.Models;
using ShareXMac.Platform;
using ShareXMac.Services;
using ShareXMac.ViewModels;
using Xunit;

namespace ShareXMac.Tests;

public class TrayViewModelHotkeyTests
{
    private static (TrayViewModel vm, TrackingHotkeyManager hotkeys, SettingsService svc) Make()
    {
        var svc = new SettingsService(Path.Combine(Path.GetTempPath(), $"s-{Guid.NewGuid():N}.json"));
        var hotkeys = new TrackingHotkeyManager();
        var vm = new TrayViewModel(
            new StubScreenCapture(), svc,
            new HistoryService(Path.Combine(Path.GetTempPath(), $"h-{Guid.NewGuid():N}.json")),
            new UploadService(), hotkeys);
        return (vm, hotkeys, svc);
    }

    [Fact]
    public void TrayViewModel_DoesNotRegisterHotkeys_WhenAllNull()
    {
        var (_, hotkeys, _) = Make();
        Assert.Empty(hotkeys.RegisteredIds);
    }

    [Fact]
    public void TrayViewModel_RegistersHotkey_WhenCaptureRegionSet()
    {
        var (_, hotkeys, svc) = Make();
        svc.Current.Hotkeys.CaptureRegion = new KeyCombo("Cmd", "1");
        svc.Save();
        Assert.Contains("capture-region", hotkeys.RegisteredIds);
    }

    [Fact]
    public void TrayViewModel_ReregistersOnSave_ClearingPreviousHotkeys()
    {
        var (_, hotkeys, svc) = Make();
        svc.Current.Hotkeys.CaptureRegion = new KeyCombo("Cmd", "1");
        svc.Save();
        Assert.Single(hotkeys.RegisteredIds);

        svc.Current.Hotkeys.CaptureRegion = null;
        svc.Save();
        Assert.Empty(hotkeys.RegisteredIds);
    }
}

// Test double — tracks which hotkey IDs are currently registered
public class TrackingHotkeyManager : IHotkeyManager
{
    public bool IsAvailable => false;
    public List<string> RegisteredIds { get; } = new();
    public void Register(string id, KeyCombo combo, Action callback) => RegisteredIds.Add(id);
    public void Unregister(string id) => RegisteredIds.Remove(id);
    public void UnregisterAll() => RegisteredIds.Clear();
}
```

- [ ] **Step 2: Run to confirm compile failure**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet test ShareXMac.Tests/ShareXMac.Tests.csproj --filter "TrayViewModelHotkeyTests" 2>&1 | tail -5
```

Expected: compile error (TrackingHotkeyManager can't implement IHotkeyManager with the current TrayViewModel 4-arg constructor)

- [ ] **Step 3: Add Saved event to SettingsService**

Replace `ShareXMac/Services/SettingsService.cs`:

```csharp
using Newtonsoft.Json;
using ShareXMac.Models;

namespace ShareXMac.Services;

public class SettingsService
{
    private readonly string _filePath;

    public AppSettings Current { get; private set; }

    public event Action? Saved;

    public SettingsService(string filePath)
    {
        _filePath = filePath;
        Current = Load();
    }

    private AppSettings Load()
    {
        if (!File.Exists(_filePath)) return new AppSettings();
        try
        {
            string json = File.ReadAllText(_filePath);
            return JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        File.WriteAllText(_filePath, JsonConvert.SerializeObject(Current, Formatting.Indented));
        Saved?.Invoke();
    }
}
```

- [ ] **Step 4: Fix MacHotkeyManager to check modifier flags**

Replace `ShareXMac.ScreenCaptureLib/MacHotkeyManager.cs`. Add the `CGEventGetFlags` P/Invoke and update `HandleEvent` to match modifier state:

```csharp
using System.Runtime.InteropServices;
using ShareX.HelpersLib;

namespace ShareXMac.ScreenCaptureLib;

public class MacHotkeyManager : IHotkeyManager, IDisposable
{
    private const int kCGHIDEventTap = 0;
    private const int kCGHeadInsertEventTap = 0;
    private const int kCGEventTapOptionListenOnly = 1;
    private const ulong kCGEventKeyDownMask = 1ul << 10;
    private const int kCGKeyboardEventKeycode = 9;

    // CGEventFlags bitmasks
    private const ulong FlagShift   = 0x00020000UL;
    private const ulong FlagControl = 0x00040000UL;
    private const ulong FlagAlt     = 0x00080000UL;
    private const ulong FlagCommand = 0x00100000UL;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate nint CGEventCallback(nint proxy, uint type, nint eventRef, nint userInfo);

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern nint CGEventTapCreate(
        int tap, int place, int options,
        ulong eventsOfInterest,
        CGEventCallback callback,
        nint userInfo);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern nint CFMachPortCreateRunLoopSource(
        nint allocator, nint tap, nint order);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern nint CFRunLoopGetCurrent();

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern void CFRunLoopAddSource(nint runloop, nint source, nint mode);

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern long CGEventGetIntegerValueField(nint eventRef, int field);

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern ulong CGEventGetFlags(nint eventRef);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern void CFMachPortInvalidate(nint port);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern void CFRelease(nint cf);

    [DllImport("/System/Library/Frameworks/ApplicationServices.framework/ApplicationServices")]
    private static extern bool AXIsProcessTrustedWithOptions(nint options);

    private readonly Dictionary<string, (KeyCombo combo, Action callback)> _hotkeys = new();
    private nint _tapPort;
    private readonly CGEventCallback _nativeCallback;

    public MacHotkeyManager()
    {
        _nativeCallback = HandleEvent;
    }

    public bool IsAvailable
    {
        get
        {
            try { return AXIsProcessTrustedWithOptions(0); }
            catch { return false; }
        }
    }

    public void Register(string id, KeyCombo combo, Action callback)
    {
        _hotkeys[id] = (combo, callback);
        EnsureTapRunning();
    }

    public void Unregister(string id) => _hotkeys.Remove(id);

    public void UnregisterAll() => _hotkeys.Clear();

    private void EnsureTapRunning()
    {
        if (_tapPort != 0) return;
        _tapPort = CGEventTapCreate(
            kCGHIDEventTap, kCGHeadInsertEventTap,
            kCGEventTapOptionListenOnly,
            kCGEventKeyDownMask,
            _nativeCallback, 0);
        if (_tapPort == 0) return;
        nint source = CFMachPortCreateRunLoopSource(0, _tapPort, 0);
        if (source == 0) return;
        CFRunLoopAddSource(CFRunLoopGetCurrent(), source, 0);
    }

    private nint HandleEvent(nint proxy, uint type, nint eventRef, nint userInfo)
    {
        if (type == 10) // kCGEventKeyDown
        {
            long keycode = CGEventGetIntegerValueField(eventRef, kCGKeyboardEventKeycode);
            ulong flags  = CGEventGetFlags(eventRef);
            bool cmdHeld   = (flags & FlagCommand) != 0;
            bool shiftHeld = (flags & FlagShift)   != 0;
            bool ctrlHeld  = (flags & FlagControl) != 0;
            bool altHeld   = (flags & FlagAlt)     != 0;

            foreach (var (_, (combo, callback)) in _hotkeys)
            {
                if (GetVirtualKeyCode(combo.Key) != keycode) continue;
                bool needsCmd   = combo.Modifiers.Contains("Cmd");
                bool needsShift = combo.Modifiers.Contains("Shift");
                bool needsCtrl  = combo.Modifiers.Contains("Ctrl");
                bool needsAlt   = combo.Modifiers.Contains("Alt");
                if (cmdHeld == needsCmd && shiftHeld == needsShift
                    && ctrlHeld == needsCtrl && altHeld == needsAlt)
                    callback();
            }
        }
        return eventRef;
    }

    private static long GetVirtualKeyCode(string key) => key.ToUpperInvariant() switch
    {
        "A" => 0, "S" => 1, "D" => 2, "F" => 3, "H" => 4, "G" => 5,
        "Z" => 6, "X" => 7, "C" => 8, "V" => 9, "B" => 11, "Q" => 12,
        "W" => 13, "E" => 14, "R" => 15, "Y" => 16, "T" => 17,
        "1" => 18, "2" => 19, "3" => 20, "4" => 21, "5" => 23,
        "6" => 22, "7" => 26, "8" => 28, "9" => 25, "0" => 29,
        "F1" => 122, "F2" => 120, "F3" => 99, "F4" => 118,
        "F5" => 96, "F6" => 97, "F7" => 98, "F8" => 100,
        _ => -1
    };

    public void Dispose()
    {
        if (_tapPort != 0)
        {
            CFMachPortInvalidate(_tapPort);
            CFRelease(_tapPort);
            _tapPort = 0;
        }
    }
}
```

- [ ] **Step 5: Update TrayViewModel to accept IHotkeyManager and register hotkeys**

Replace `ShareXMac/ViewModels/TrayViewModel.cs`:

```csharp
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShareX.HelpersLib;
using ShareXMac.Models;
using ShareXMac.ScreenCaptureLib;
using ShareXMac.Services;
using ShareXMac.Views;
using System.Diagnostics;

namespace ShareXMac.ViewModels;

public partial class TrayViewModel : ObservableObject
{
    private readonly IScreenCapture _capture;
    private readonly SettingsService _settings;
    private readonly HistoryService _history;
    private readonly UploadService _upload;
    private readonly IHotkeyManager _hotkeyManager;

    [ObservableProperty] private bool _isRecording;

    public string RecordVideoHeader => IsRecording ? "Stop Recording (Video)" : "Record Video";
    public string RecordGifHeader   => IsRecording ? "Stop Recording (GIF)"   : "Record GIF";

    public TrayViewModel(
        IScreenCapture capture,
        SettingsService settings,
        HistoryService history,
        UploadService upload,
        IHotkeyManager hotkeyManager)
    {
        _capture      = capture;
        _settings     = settings;
        _history      = history;
        _upload       = upload;
        _hotkeyManager = hotkeyManager;
        settings.Saved += RegisterHotkeys;
        RegisterHotkeys();
    }

    partial void OnIsRecordingChanged(bool value)
    {
        OnPropertyChanged(nameof(RecordVideoHeader));
        OnPropertyChanged(nameof(RecordGifHeader));
    }

    private void RegisterHotkeys()
    {
        _hotkeyManager.UnregisterAll();
        var h = _settings.Current.Hotkeys;
        RegisterHotkey("capture-region",    h.CaptureRegion,    CaptureRegion);
        RegisterHotkey("capture-window",    h.CaptureWindow,    CaptureWindow);
        RegisterHotkey("capture-fullscreen", h.CaptureFullscreen, CaptureFullscreen);
        RegisterHotkey("record-video",      h.RecordVideo,      RecordVideo);
        RegisterHotkey("record-gif",        h.RecordGif,        RecordGif);
    }

    private void RegisterHotkey(string id, KeyCombo? combo, Func<Task> action)
    {
        if (combo == null) return;
        _hotkeyManager.Register(id, combo,
            () => Dispatcher.UIThread.Post(() => _ = action()));
    }

    [RelayCommand]
    private async Task CaptureRegion()
    {
        byte[]? data = await _capture.CaptureRegionAsync();
        if (data != null) await OnCaptureComplete(data);
    }

    [RelayCommand]
    private async Task CaptureWindow()
    {
        byte[]? data = await _capture.CaptureWindowAsync();
        if (data != null) await OnCaptureComplete(data);
    }

    [RelayCommand]
    private async Task CaptureFullscreen()
    {
        byte[]? data = await _capture.CaptureFullscreenAsync();
        if (data != null) await OnCaptureComplete(data);
    }

    [RelayCommand]
    private async Task RecordVideo()
    {
        if (IsRecording)
        {
            await _capture.StopRecordingAsync();
            IsRecording = false;
        }
        else
        {
            string dir = _settings.Current.SavePath;
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, $"ShareX-{DateTime.Now:yyyy-MM-dd-HHmmss}.mp4");
            await _capture.StartRecordingAsync(path, RecordingFormat.MP4);
            IsRecording = true;
        }
    }

    [RelayCommand]
    private async Task RecordGif()
    {
        if (IsRecording)
        {
            await _capture.StopRecordingAsync();
            IsRecording = false;
        }
        else
        {
            string dir = _settings.Current.SavePath;
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, $"ShareX-{DateTime.Now:yyyy-MM-dd-HHmmss}.gif");
            await _capture.StartRecordingAsync(path, RecordingFormat.GIF);
            IsRecording = true;
        }
    }

    [RelayCommand]
    private void OpenSettings()
    {
        Dispatcher.UIThread.Post(() =>
        {
            var vm = new SettingsViewModel(_settings);
            new SettingsWindow(vm).Show();
        });
    }

    [RelayCommand]
    private void OpenHistory()
    {
        Dispatcher.UIThread.Post(() =>
        {
            var vm = new HistoryViewModel(_history);
            new HistoryWindow(vm).Show();
        });
    }

    [RelayCommand]
    private static void Quit()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime app)
            app.Shutdown();
    }

    private async Task OnCaptureComplete(byte[] data)
    {
        string dir = _settings.Current.SavePath;
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, $"ShareX-{DateTime.Now:yyyy-MM-dd-HHmmss}.png");
        await File.WriteAllBytesAsync(path, data);

        string? url = null;
        if (_settings.Current.AutoUploadAfterCapture)
        {
            url = await _upload.UploadImageAsync(data, Path.GetFileName(path), _settings.Current);
            if (url != null)
                MacClipboard.SetText(url);
        }

        _history.AddCapture(path, url);

        if (url == null)
        {
            if (_settings.Current.AutoCopyImage)
                MacClipboard.SetImage(data);
            else
                MacClipboard.SetText(path);
        }

        if (_settings.Current.ShowPostCaptureToolbar)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var result = new CaptureResult(data, path);
                var vm = new PostCaptureViewModel(result, _upload, _settings.Current)
                {
                    AutoDismissSeconds = _settings.Current.PostCaptureToolbarTimeoutSeconds,
                    UploadedUrl = url
                };
                new PostCaptureWindow(vm).Show();
            });
        }
    }
}
```

- [ ] **Step 6: Update TrayViewModelTests.cs — add StubHotkeyManager to the 3 existing tests**

Read `ShareXMac.Tests/TrayViewModelTests.cs`. Every `new TrayViewModel(...)` call currently has 4 args. Update each one to add `new StubHotkeyManager()` as the 5th arg:

Old:
```csharp
new TrayViewModel(
    new StubScreenCapture(),
    new SettingsService(Path.GetTempFileName()),
    new HistoryService(Path.GetTempFileName()),
    new UploadService())
```

New:
```csharp
new TrayViewModel(
    new StubScreenCapture(),
    new SettingsService(Path.GetTempFileName()),
    new HistoryService(Path.GetTempFileName()),
    new UploadService(),
    new StubHotkeyManager())
```

There are 3 such constructors in that file. Update all 3.

- [ ] **Step 7: Update App.axaml.cs to instantiate MacHotkeyManager**

Replace `ShareXMac/App.axaml.cs`:

```csharp
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

        var settings     = new SettingsService(Path.Combine(appSupport, "settings.json"));
        var history      = new HistoryService(Path.Combine(appSupport, "history.json"));
        var capture      = new MacScreenCapture();
        var upload       = new UploadService();
        var hotkeyManager = new MacHotkeyManager();

        DataContext = new TrayViewModel(capture, settings, history, upload, hotkeyManager);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.ShutdownMode = Avalonia.Controls.ShutdownMode.OnExplicitShutdown;

        base.OnFrameworkInitializationCompleted();
    }
}
```

- [ ] **Step 8: Build and run all tests**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet build ShareX-Mac.sln 2>&1 | tail -5
dotnet test ShareXMac.Tests/ShareXMac.Tests.csproj 2>&1 | tail -3
```

Expected: 0 errors, 59 tests pass (55 original + 1 SettingsService.Saved + 3 hotkey registration)

- [ ] **Step 9: Commit**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && git add ShareXMac/Services/SettingsService.cs ShareXMac.ScreenCaptureLib/MacHotkeyManager.cs ShareXMac/ViewModels/TrayViewModel.cs ShareXMac/App.axaml.cs ShareXMac.Tests/TrayViewModelTests.cs ShareXMac.Tests/TrayViewModelHotkeyTests.cs ShareXMac.Tests/SettingsServiceTests.cs && git commit -m "feat: wire MacHotkeyManager with modifier matching and hotkey registration"
```

---

### Task 3: IsRecording observable + dynamic menu headers in App.axaml

The `IsRecording` property was added in Task 2. This task binds it in the tray menu so "Record Video" becomes "Stop Recording (Video)" while active.

**Files:**
- Modify: `ShareXMac/App.axaml`
- Create/Modify: `ShareXMac.Tests/TrayViewModelRecordingTests.cs`

- [ ] **Step 1: Write the failing test**

Create `ShareXMac.Tests/TrayViewModelRecordingTests.cs`:

```csharp
using ShareXMac.Platform;
using ShareXMac.Services;
using ShareXMac.ViewModels;
using Xunit;

namespace ShareXMac.Tests;

public class TrayViewModelRecordingTests
{
    private static TrayViewModel MakeVm() => new TrayViewModel(
        new StubScreenCapture(),
        new SettingsService(Path.GetTempFileName()),
        new HistoryService(Path.GetTempFileName()),
        new UploadService(),
        new StubHotkeyManager());

    [Fact]
    public void IsRecording_DefaultsFalse()
        => Assert.False(MakeVm().IsRecording);

    [Fact]
    public void RecordVideoHeader_IsRecordVideo_WhenNotRecording()
        => Assert.Equal("Record Video", MakeVm().RecordVideoHeader);

    [Fact]
    public void RecordGifHeader_IsRecordGif_WhenNotRecording()
        => Assert.Equal("Record GIF", MakeVm().RecordGifHeader);

    [Fact]
    public void RecordVideoHeader_ChangesWhenRecordingStarts()
    {
        var vm = MakeVm();
        vm.IsRecording = true;
        Assert.Equal("Stop Recording (Video)", vm.RecordVideoHeader);
    }

    [Fact]
    public void RecordGifHeader_ChangesWhenRecordingStarts()
    {
        var vm = MakeVm();
        vm.IsRecording = true;
        Assert.Equal("Stop Recording (GIF)", vm.RecordGifHeader);
    }
}
```

- [ ] **Step 2: Run to verify tests pass immediately** (RecordVideoHeader and IsRecording were added in Task 2)

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet test ShareXMac.Tests/ShareXMac.Tests.csproj --filter "TrayViewModelRecordingTests" -v normal 2>&1 | tail -10
```

Expected: 5 passed (this was already implemented in Task 2 — these tests just confirm it)

- [ ] **Step 3: Update App.axaml to bind menu headers**

Replace `ShareXMac/App.axaml`:

```xml
<Application xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="ShareXMac.App">
    <Application.Styles>
        <FluentTheme />
    </Application.Styles>

    <TrayIcon.Icons>
        <TrayIcons>
            <TrayIcon Icon="/Assets/icon.ico" ToolTipText="ShareX Mac">
                <TrayIcon.Menu>
                    <NativeMenu>
                        <NativeMenuItem Header="Capture Region"
                                        Command="{Binding CaptureRegionCommand}" />
                        <NativeMenuItem Header="Capture Window"
                                        Command="{Binding CaptureWindowCommand}" />
                        <NativeMenuItem Header="Capture Fullscreen"
                                        Command="{Binding CaptureFullscreenCommand}" />
                        <NativeMenuItemSeparator />
                        <NativeMenuItem Header="{Binding RecordVideoHeader}"
                                        Command="{Binding RecordVideoCommand}" />
                        <NativeMenuItem Header="{Binding RecordGifHeader}"
                                        Command="{Binding RecordGifCommand}" />
                        <NativeMenuItemSeparator />
                        <NativeMenuItem Header="Settings..."
                                        Command="{Binding OpenSettingsCommand}" />
                        <NativeMenuItem Header="History..."
                                        Command="{Binding OpenHistoryCommand}" />
                        <NativeMenuItemSeparator />
                        <NativeMenuItem Header="Quit"
                                        Command="{Binding QuitCommand}" />
                    </NativeMenu>
                </TrayIcon.Menu>
            </TrayIcon>
        </TrayIcons>
    </TrayIcon.Icons>
</Application>
```

- [ ] **Step 4: Build and run all tests**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet build ShareX-Mac.sln 2>&1 | tail -5
dotnet test ShareXMac.Tests/ShareXMac.Tests.csproj 2>&1 | tail -3
```

Expected: 0 errors, 64 tests pass

- [ ] **Step 5: Commit**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && git add ShareXMac/App.axaml ShareXMac.Tests/TrayViewModelRecordingTests.cs && git commit -m "feat: dynamic record menu headers bound to IsRecording"
```

---

### Task 4: SettingsViewModel hotkey properties

Add 5 hotkey string properties to `SettingsViewModel` so the UI can display and edit configured hotkeys. Each maps to a `KeyCombo?` in `HotkeySettings` via `KeyComboHelper`.

**Files:**
- Modify: `ShareXMac/ViewModels/SettingsViewModel.cs`
- Create: `ShareXMac.Tests/SettingsViewModelTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `ShareXMac.Tests/SettingsViewModelTests.cs`:

```csharp
using ShareX.HelpersLib;
using ShareXMac.Services;
using ShareXMac.ViewModels;
using Xunit;

namespace ShareXMac.Tests;

public class SettingsViewModelTests
{
    private static SettingsService MakeSvc()
        => new SettingsService(Path.Combine(Path.GetTempPath(), $"s-{Guid.NewGuid():N}.json"));

    [Fact]
    public void SettingsViewModel_LoadsEmptyHotkey_WhenNotConfigured()
    {
        var vm = new SettingsViewModel(MakeSvc());
        Assert.Equal("", vm.CaptureRegionHotkey);
    }

    [Fact]
    public void SettingsViewModel_LoadsHotkey_FromSettings()
    {
        var svc = MakeSvc();
        svc.Current.Hotkeys.CaptureRegion = new KeyCombo("Cmd+Shift", "3");
        var vm = new SettingsViewModel(svc);
        Assert.Equal("Cmd+Shift+3", vm.CaptureRegionHotkey);
    }

    [Fact]
    public void SettingsViewModel_SavesHotkey_ToSettings()
    {
        var svc = MakeSvc();
        var vm = new SettingsViewModel(svc);
        vm.CaptureRegionHotkey = "Cmd+4";
        vm.SaveCommand.Execute(null);
        Assert.Equal(new KeyCombo("Cmd", "4"), svc.Current.Hotkeys.CaptureRegion);
    }

    [Fact]
    public void SettingsViewModel_SavesNullHotkey_WhenEmpty()
    {
        var svc = MakeSvc();
        svc.Current.Hotkeys.CaptureWindow = new KeyCombo("Cmd", "2");
        var vm = new SettingsViewModel(svc);
        vm.CaptureWindowHotkey = "";
        vm.SaveCommand.Execute(null);
        Assert.Null(svc.Current.Hotkeys.CaptureWindow);
    }

    [Fact]
    public void SettingsViewModel_ClearCaptureRegionHotkeyCommand_SetsEmpty()
    {
        var svc = MakeSvc();
        svc.Current.Hotkeys.CaptureRegion = new KeyCombo("Cmd", "1");
        var vm = new SettingsViewModel(svc);
        vm.ClearCaptureRegionHotkeyCommand.Execute(null);
        Assert.Equal("", vm.CaptureRegionHotkey);
    }

    [Fact]
    public void SettingsViewModel_LoadsAllFiveHotkeys()
    {
        var svc = MakeSvc();
        svc.Current.Hotkeys.CaptureRegion    = new KeyCombo("Cmd+Shift", "1");
        svc.Current.Hotkeys.CaptureWindow    = new KeyCombo("Cmd+Shift", "2");
        svc.Current.Hotkeys.CaptureFullscreen = new KeyCombo("Cmd+Shift", "3");
        svc.Current.Hotkeys.RecordVideo      = new KeyCombo("Cmd+Shift", "5");
        svc.Current.Hotkeys.RecordGif        = new KeyCombo("Cmd+Shift", "6");
        var vm = new SettingsViewModel(svc);
        Assert.Equal("Cmd+Shift+1", vm.CaptureRegionHotkey);
        Assert.Equal("Cmd+Shift+2", vm.CaptureWindowHotkey);
        Assert.Equal("Cmd+Shift+3", vm.CaptureFullscreenHotkey);
        Assert.Equal("Cmd+Shift+5", vm.RecordVideoHotkey);
        Assert.Equal("Cmd+Shift+6", vm.RecordGifHotkey);
    }
}
```

- [ ] **Step 2: Run to confirm compile failure**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet test ShareXMac.Tests/ShareXMac.Tests.csproj --filter "SettingsViewModelTests" 2>&1 | tail -5
```

Expected: compile error (missing hotkey properties and clear commands on SettingsViewModel)

- [ ] **Step 3: Update SettingsViewModel.cs**

Replace `ShareXMac/ViewModels/SettingsViewModel.cs`:

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShareX.UploadersLib;
using ShareXMac.Models;
using ShareXMac.Services;

namespace ShareXMac.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly SettingsService _service;
    private readonly LoginItemService _loginItems;

    [ObservableProperty] private string _savePath = "";
    [ObservableProperty] private bool _autoCopyImage;
    [ObservableProperty] private bool _showPostCaptureToolbar;
    [ObservableProperty] private int _postCaptureTimeoutSeconds;
    [ObservableProperty] private string _imgurClientId = "";
    [ObservableProperty] private ImageDestination _activeImageDestination;
    [ObservableProperty] private bool _autoUploadAfterCapture;
    [ObservableProperty] private bool _launchAtLogin;

    // Hotkeys — displayed and edited as "Modifier+Key" strings (e.g. "Cmd+Shift+3")
    [ObservableProperty] private string _captureRegionHotkey = "";
    [ObservableProperty] private string _captureWindowHotkey = "";
    [ObservableProperty] private string _captureFullscreenHotkey = "";
    [ObservableProperty] private string _recordVideoHotkey = "";
    [ObservableProperty] private string _recordGifHotkey = "";

    public event Action? CloseRequested;

    public SettingsViewModel(SettingsService service, LoginItemService? loginItems = null)
    {
        _service    = service;
        _loginItems = loginItems ?? new LoginItemService();
        var s = service.Current;
        SavePath                  = s.SavePath;
        AutoCopyImage             = s.AutoCopyImage;
        ShowPostCaptureToolbar    = s.ShowPostCaptureToolbar;
        PostCaptureTimeoutSeconds = s.PostCaptureToolbarTimeoutSeconds;
        ImgurClientId             = s.ImgurClientId;
        ActiveImageDestination    = s.ActiveImageDestination;
        AutoUploadAfterCapture    = s.AutoUploadAfterCapture;
        LaunchAtLogin             = _loginItems.IsEnabled;

        var h = s.Hotkeys;
        CaptureRegionHotkey    = KeyComboHelper.ToString(h.CaptureRegion);
        CaptureWindowHotkey    = KeyComboHelper.ToString(h.CaptureWindow);
        CaptureFullscreenHotkey = KeyComboHelper.ToString(h.CaptureFullscreen);
        RecordVideoHotkey      = KeyComboHelper.ToString(h.RecordVideo);
        RecordGifHotkey        = KeyComboHelper.ToString(h.RecordGif);
    }

    [RelayCommand]
    private async Task Browse(Avalonia.Controls.Window? owner)
    {
        if (owner is null) return;
        var result = await owner.StorageProvider.OpenFolderPickerAsync(
            new Avalonia.Platform.Storage.FolderPickerOpenOptions
            {
                Title = "Select Save Location",
                AllowMultiple = false
            });
        if (result.Count > 0)
            SavePath = result[0].Path.LocalPath;
    }

    [RelayCommand] private void ClearCaptureRegionHotkey()    => CaptureRegionHotkey = "";
    [RelayCommand] private void ClearCaptureWindowHotkey()    => CaptureWindowHotkey = "";
    [RelayCommand] private void ClearCaptureFullscreenHotkey() => CaptureFullscreenHotkey = "";
    [RelayCommand] private void ClearRecordVideoHotkey()      => RecordVideoHotkey = "";
    [RelayCommand] private void ClearRecordGifHotkey()        => RecordGifHotkey = "";

    [RelayCommand]
    private void Save()
    {
        _service.Current.SavePath                        = SavePath;
        _service.Current.AutoCopyImage                   = AutoCopyImage;
        _service.Current.ShowPostCaptureToolbar          = ShowPostCaptureToolbar;
        _service.Current.PostCaptureToolbarTimeoutSeconds = PostCaptureTimeoutSeconds;
        _service.Current.ImgurClientId                   = ImgurClientId;
        _service.Current.ActiveImageDestination          = ActiveImageDestination;
        _service.Current.AutoUploadAfterCapture          = AutoUploadAfterCapture;

        _service.Current.Hotkeys.CaptureRegion     = KeyComboHelper.Parse(CaptureRegionHotkey);
        _service.Current.Hotkeys.CaptureWindow     = KeyComboHelper.Parse(CaptureWindowHotkey);
        _service.Current.Hotkeys.CaptureFullscreen = KeyComboHelper.Parse(CaptureFullscreenHotkey);
        _service.Current.Hotkeys.RecordVideo       = KeyComboHelper.Parse(RecordVideoHotkey);
        _service.Current.Hotkeys.RecordGif         = KeyComboHelper.Parse(RecordGifHotkey);

        _service.Save();

        if (LaunchAtLogin && !_loginItems.IsEnabled)
        {
            string exe = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName
                         ?? Path.Combine(AppContext.BaseDirectory, "SnapX");
            _loginItems.Enable(exe);
        }
        else if (!LaunchAtLogin && _loginItems.IsEnabled)
        {
            _loginItems.Disable();
        }

        CloseRequested?.Invoke();
    }
}
```

- [ ] **Step 4: Run SettingsViewModelTests**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet test ShareXMac.Tests/ShareXMac.Tests.csproj --filter "SettingsViewModelTests" -v normal 2>&1 | tail -10
```

Expected: 6 passed

- [ ] **Step 5: Run all tests**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet test ShareXMac.Tests/ShareXMac.Tests.csproj 2>&1 | tail -3
```

Expected: all pass (70 tests)

- [ ] **Step 6: Commit**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && git add ShareXMac/ViewModels/SettingsViewModel.cs ShareXMac.Tests/SettingsViewModelTests.cs && git commit -m "feat: add hotkey properties to SettingsViewModel"
```

---

### Task 5: SettingsWindow Hotkeys section + key capture UI

Add a Hotkeys section to `SettingsWindow` with 5 rows. Each row has a read-only TextBox that captures a key combo when focused (via `KeyDown` in code-behind), a label, and a Clear button.

**Files:**
- Modify: `ShareXMac/Views/SettingsWindow.axaml`
- Modify: `ShareXMac/Views/SettingsWindow.axaml.cs`

No new unit tests — the behavior is covered by Task 4's SettingsViewModel tests and by manual verification.

- [ ] **Step 1: Add Hotkeys section to SettingsWindow.axaml**

Replace `ShareXMac/Views/SettingsWindow.axaml` entirely:

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="using:ShareXMac.ViewModels"
        x:Class="ShareXMac.Views.SettingsWindow"
        x:DataType="vm:SettingsViewModel"
        Width="480" Height="600"
        CanResize="False"
        WindowStartupLocation="CenterScreen"
        Title="ShareX Mac — Settings">
    <ScrollViewer>
        <StackPanel Margin="20" Spacing="14">

            <!-- Save location -->
            <TextBlock Text="Save Location" FontWeight="SemiBold" />
            <Grid ColumnDefinitions="*,Auto">
                <TextBox Grid.Column="0" Text="{Binding SavePath}"
                         Watermark="/Users/you/Pictures/ShareX Mac" />
                <Button Grid.Column="1" Content="Browse…"
                        Command="{Binding BrowseCommand}"
                        CommandParameter="{Binding $parent[Window]}"
                        Margin="8,0,0,0" Padding="12,6" />
            </Grid>

            <Separator />

            <!-- Capture behavior -->
            <TextBlock Text="After Capture" FontWeight="SemiBold" />
            <CheckBox Content="Copy image to clipboard after capture"
                      IsChecked="{Binding AutoCopyImage}" />
            <CheckBox Content="Show post-capture toolbar"
                      IsChecked="{Binding ShowPostCaptureToolbar}" />
            <Grid ColumnDefinitions="Auto,*">
                <TextBlock Grid.Column="0" Text="Toolbar timeout (seconds):"
                           VerticalAlignment="Center" Margin="0,0,8,0" />
                <NumericUpDown Grid.Column="1" Value="{Binding PostCaptureTimeoutSeconds}"
                               Minimum="2" Maximum="60" Width="80" HorizontalAlignment="Left" />
            </Grid>

            <Separator />

            <!-- Upload -->
            <TextBlock Text="Upload" FontWeight="SemiBold" />
            <CheckBox Content="Auto-upload after capture"
                      IsChecked="{Binding AutoUploadAfterCapture}" />
            <Grid ColumnDefinitions="Auto,*">
                <TextBlock Grid.Column="0" Text="Destination:"
                           VerticalAlignment="Center" Margin="0,0,8,0" />
                <TextBlock Grid.Column="1" Text="Imgur" VerticalAlignment="Center" />
            </Grid>
            <Grid ColumnDefinitions="Auto,*">
                <TextBlock Grid.Column="0" Text="Imgur Client ID:"
                           VerticalAlignment="Center" Margin="0,0,8,0" />
                <TextBox Grid.Column="1" Text="{Binding ImgurClientId}"
                         Watermark="Register free at api.imgur.com/oauth2/addclient" />
            </Grid>
            <TextBlock Text="Imgur anonymous upload requires a free Client ID (no login needed)."
                       FontSize="11" Foreground="#FFAAAAAA" TextWrapping="Wrap" />

            <Separator />

            <!-- Hotkeys -->
            <TextBlock Text="Hotkeys" FontWeight="SemiBold" />
            <TextBlock Text="Click a field and press a key combo (e.g. Cmd+Shift+1). Clear to disable."
                       FontSize="11" Foreground="#FFAAAAAA" TextWrapping="Wrap" />

            <Grid ColumnDefinitions="130,*,Auto">
                <TextBlock Grid.Column="0" Text="Capture Region:"
                           VerticalAlignment="Center" />
                <TextBox Grid.Column="1" x:Name="CaptureRegionBox"
                         Text="{Binding CaptureRegionHotkey}"
                         Watermark="Not set" IsReadOnly="True"
                         Cursor="Hand" />
                <Button Grid.Column="2" Content="✕"
                        Command="{Binding ClearCaptureRegionHotkeyCommand}"
                        Margin="4,0,0,0" Padding="8,4" />
            </Grid>

            <Grid ColumnDefinitions="130,*,Auto">
                <TextBlock Grid.Column="0" Text="Capture Window:"
                           VerticalAlignment="Center" />
                <TextBox Grid.Column="1" x:Name="CaptureWindowBox"
                         Text="{Binding CaptureWindowHotkey}"
                         Watermark="Not set" IsReadOnly="True"
                         Cursor="Hand" />
                <Button Grid.Column="2" Content="✕"
                        Command="{Binding ClearCaptureWindowHotkeyCommand}"
                        Margin="4,0,0,0" Padding="8,4" />
            </Grid>

            <Grid ColumnDefinitions="130,*,Auto">
                <TextBlock Grid.Column="0" Text="Capture Fullscreen:"
                           VerticalAlignment="Center" />
                <TextBox Grid.Column="1" x:Name="CaptureFullscreenBox"
                         Text="{Binding CaptureFullscreenHotkey}"
                         Watermark="Not set" IsReadOnly="True"
                         Cursor="Hand" />
                <Button Grid.Column="2" Content="✕"
                        Command="{Binding ClearCaptureFullscreenHotkeyCommand}"
                        Margin="4,0,0,0" Padding="8,4" />
            </Grid>

            <Grid ColumnDefinitions="130,*,Auto">
                <TextBlock Grid.Column="0" Text="Record Video:"
                           VerticalAlignment="Center" />
                <TextBox Grid.Column="1" x:Name="RecordVideoBox"
                         Text="{Binding RecordVideoHotkey}"
                         Watermark="Not set" IsReadOnly="True"
                         Cursor="Hand" />
                <Button Grid.Column="2" Content="✕"
                        Command="{Binding ClearRecordVideoHotkeyCommand}"
                        Margin="4,0,0,0" Padding="8,4" />
            </Grid>

            <Grid ColumnDefinitions="130,*,Auto">
                <TextBlock Grid.Column="0" Text="Record GIF:"
                           VerticalAlignment="Center" />
                <TextBox Grid.Column="1" x:Name="RecordGifBox"
                         Text="{Binding RecordGifHotkey}"
                         Watermark="Not set" IsReadOnly="True"
                         Cursor="Hand" />
                <Button Grid.Column="2" Content="✕"
                        Command="{Binding ClearRecordGifHotkeyCommand}"
                        Margin="4,0,0,0" Padding="8,4" />
            </Grid>

            <Separator />

            <!-- System -->
            <TextBlock Text="System" FontWeight="SemiBold" />
            <CheckBox Content="Launch at Login"
                      IsChecked="{Binding LaunchAtLogin}" />

            <Separator />

            <Button Content="Save" Command="{Binding SaveCommand}"
                    HorizontalAlignment="Right" Background="#FF0078D4"
                    Foreground="White" Padding="16,8" />
        </StackPanel>
    </ScrollViewer>
</Window>
```

- [ ] **Step 2: Add key-capture handlers to SettingsWindow.axaml.cs**

Replace `ShareXMac/Views/SettingsWindow.axaml.cs`:

```csharp
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
        Key.F1 => "F1", Key.F2 => "F2", Key.F3 => "F3", Key.F4 => "F4",
        Key.F5 => "F5", Key.F6 => "F6", Key.F7 => "F7", Key.F8 => "F8",
        _ => null
    };
}
```

- [ ] **Step 3: Build to verify no compile errors**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet build ShareX-Mac.sln 2>&1 | tail -5
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 4: Run all tests**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet test ShareXMac.Tests/ShareXMac.Tests.csproj 2>&1 | tail -3
```

Expected: all pass (70 tests)

- [ ] **Step 5: Manual smoke test**

Launch the app:

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet run --project ShareXMac/ShareXMac.csproj
```

1. Open Settings (tray menu → Settings...)
2. Scroll to the Hotkeys section — 5 rows should appear with "Not set" placeholders
3. Click "Capture Region" field → press `Cmd+Shift+1` → field should display `Cmd+Shift+1`
4. Click "✕" next to it — field should clear
5. Click Save
6. Re-open Settings — field should be empty (hotkey was cleared)
7. Configure `Cmd+1` for Capture Region, save, close
8. Re-open Settings — field shows `Cmd+1`
9. Press `Cmd+1` anywhere on the desktop — Capture Region should activate

- [ ] **Step 6: Commit and push**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && git add ShareXMac/Views/SettingsWindow.axaml ShareXMac/Views/SettingsWindow.axaml.cs && git commit -m "feat: add Hotkeys section to SettingsWindow with key capture"
git push origin master
```

---

## Plan Complete

After all 5 tasks:

- Global hotkeys work end-to-end: configured in Settings, persisted in `settings.json`, registered via `MacHotkeyManager` with correct modifier matching (`CGEventGetFlags`)
- Hotkeys re-register automatically whenever Settings are saved (`SettingsService.Saved` event)
- Tray menu shows "Stop Recording (Video)" / "Stop Recording (GIF)" while recording is active, reverting when stopped
- Settings window has a Hotkeys section: click a field, press a combo, it captures it; ✕ clears it; Save persists to disk
- 70 tests passing

**To configure hotkeys after launch:**
1. Open Settings from tray
2. Scroll to Hotkeys section
3. Click a field and press your desired key combo
4. Click Save

**Next plan:** Plan 7 — Color Picker tool (screen magnifier + hex/RGB display) and image annotation editor (draw arrows, boxes, text on captured screenshots before saving/uploading).
