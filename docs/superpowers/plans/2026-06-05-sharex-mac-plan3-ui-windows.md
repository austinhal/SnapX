# ShareX Mac — Plan 3: Post-Capture Toolbar, Settings & History Windows

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make captures immediately useful — show a post-capture toolbar after each screenshot, add a Settings window for save path and preferences, and a History window for browsing past captures.

**Architecture:** All windows are Avalonia `Window` subclasses with MVVM ViewModels. A `SettingsService` owns `AppSettings` (JSON file, `~/Library/Application Support/ShareX-Mac/settings.json`). A `HistoryService` wraps the existing `HistoryManagerJSON` from HistoryLib. `TrayViewModel` is updated to inject both services and open windows via `Dispatcher.UIThread`. The post-capture flow goes: capture → save to disk → append history → show toolbar.

**Tech Stack:** .NET 10, Avalonia 11.2.1, CommunityToolkit.Mvvm 8.4.0, Newtonsoft.Json 13.0.3 (transitive via HelpersLib), MacClipboard (ObjC), HistoryManagerJSON (HistoryLib).

---

## File Structure

```
ShareXMac/
  Models/
    AppSettings.cs              CREATE — POCO with JSON serialization, default values
    CaptureResult.cs            CREATE — record(byte[] ImageData, string FilePath)
  Services/
    SettingsService.cs          CREATE — load/save AppSettings from/to JSON file
    HistoryService.cs           CREATE — wraps HistoryManagerJSON
  ViewModels/
    PostCaptureViewModel.cs     CREATE — toolbar actions (copy image, copy path, open, dismiss)
    SettingsViewModel.cs        CREATE — binds to AppSettings, Browse + Save commands
    HistoryViewModel.cs         CREATE — ObservableCollection + search filter
    TrayViewModel.cs            MODIFY — inject SettingsService + HistoryService, open windows
  Views/
    PostCaptureWindow.axaml     CREATE — floating thumbnail + action buttons
    PostCaptureWindow.axaml.cs  CREATE
    SettingsWindow.axaml        CREATE — save path, checkboxes
    SettingsWindow.axaml.cs     CREATE
    HistoryWindow.axaml         CREATE — filtered list of captures
    HistoryWindow.axaml.cs      CREATE
  App.axaml.cs                  MODIFY — construct services, pass to TrayViewModel

ShareXMac.ScreenCaptureLib/
  MacClipboard.cs               MODIFY — add SetImage(byte[]) + NSAutoreleasePool wrappers

ShareXMac.Tests/
  SettingsServiceTests.cs       CREATE
  HistoryServiceTests.cs        CREATE
  PostCaptureViewModelTests.cs  CREATE
```

---

### Task 1: Plan 2 debt — MacClipboard fixes

Fix the two open issues from Plan 2: add `NSAutoreleasePool` wrappers around all MacClipboard methods (to drain NSStrings), and add `SetImage(byte[])` for copying PNG images to the clipboard.

**Files:**
- Modify: `ShareXMac.ScreenCaptureLib/MacClipboard.cs`
- Test: `ShareXMac.Tests/ScreenCaptureTests.cs`

- [ ] **Step 1: Write failing test**

Add to `ShareXMac.Tests/ScreenCaptureTests.cs` inside `MacClipboardTests`:
```csharp
[Fact]
public void SetImage_DoesNotThrow()
{
    // 1x1 white PNG (minimal valid PNG)
    byte[] png = new byte[]
    {
        0x89,0x50,0x4E,0x47,0x0D,0x0A,0x1A,0x0A,0x00,0x00,0x00,0x0D,
        0x49,0x48,0x44,0x52,0x00,0x00,0x00,0x01,0x00,0x00,0x00,0x01,
        0x08,0x02,0x00,0x00,0x00,0x90,0x77,0x53,0xDE,0x00,0x00,0x00,
        0x0C,0x49,0x44,0x41,0x54,0x08,0xD7,0x63,0xF8,0xCF,0xC0,0x00,
        0x00,0x00,0x02,0x00,0x01,0xE2,0x21,0xBC,0x33,0x00,0x00,0x00,
        0x00,0x49,0x45,0x4E,0x44,0xAE,0x42,0x60,0x82
    };
    MacClipboard.SetImage(png); // must not throw
}
```

- [ ] **Step 2: Run to confirm failure**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet test ShareXMac.Tests/ShareXMac.Tests.csproj --filter "SetImage_DoesNotThrow" 2>&1 | tail -5
```
Expected: compile error (`SetImage` does not exist)

- [ ] **Step 3: Replace MacClipboard.cs with fixed implementation**

Replace `ShareXMac.ScreenCaptureLib/MacClipboard.cs` entirely:

```csharp
using System.Runtime.InteropServices;
using ShareXMac.ScreenCaptureLib.ObjC;

namespace ShareXMac.ScreenCaptureLib;

public static class MacClipboard
{
    private const string NSPasteboardTypeString = "public.utf8-plain-text";
    private const string NSPasteboardTypePNG    = "public.png";

    private static bool s_frameworkLoaded;

    private static void EnsureFrameworkLoaded()
    {
        if (s_frameworkLoaded) return;
        ObjCRuntime.dlopen("/System/Library/Frameworks/AppKit.framework/AppKit", 1);
        s_frameworkLoaded = true;
    }

    private static nint GetGeneralPasteboard() =>
        ObjCRuntime.Send(ObjCRuntime.GetClass("NSPasteboard"), "generalPasteboard");

    private static nint CreatePool() =>
        ObjCRuntime.Send(ObjCRuntime.Send(ObjCRuntime.GetClass("NSAutoreleasePool"), "alloc"), "init");

    private static void DrainPool(nint pool) =>
        ObjCRuntime.Send(pool, "drain");

    public static void SetText(string text)
    {
        EnsureFrameworkLoaded();
        nint pool = CreatePool();
        try
        {
            nint pb = GetGeneralPasteboard();
            ObjCRuntime.Send(pb, "clearContents");
            nint nsStr = ObjCRuntime.ToNSString(text);
            nint array = ObjCRuntime.ArrayWithObject(nsStr);
            ObjCRuntime.Send(pb, "writeObjects:", array);
            ObjCRuntime.Send(nsStr, "release");
        }
        finally { DrainPool(pool); }
    }

    public static string? GetText()
    {
        EnsureFrameworkLoaded();
        nint pool = CreatePool();
        try
        {
            nint pb = GetGeneralPasteboard();
            nint typeStr = ObjCRuntime.ToNSString(NSPasteboardTypeString);
            nint result = ObjCRuntime.Send(pb, "stringForType:", typeStr);
            return ObjCRuntime.ToManagedString(result);
        }
        finally { DrainPool(pool); }
    }

    public static bool ContainsText()
    {
        EnsureFrameworkLoaded();
        nint pool = CreatePool();
        try
        {
            nint pb = GetGeneralPasteboard();
            nint typeStr = ObjCRuntime.ToNSString(NSPasteboardTypeString);
            nint types = ObjCRuntime.ArrayWithObject(typeStr);
            nint available = ObjCRuntime.Send(pb, "availableTypeFromArray:", types);
            return available != 0;
        }
        finally { DrainPool(pool); }
    }

    public static bool ContainsImage()
    {
        EnsureFrameworkLoaded();
        nint pool = CreatePool();
        try
        {
            nint pb = GetGeneralPasteboard();
            nint typeStr = ObjCRuntime.ToNSString(NSPasteboardTypePNG);
            nint types = ObjCRuntime.ArrayWithObject(typeStr);
            nint available = ObjCRuntime.Send(pb, "availableTypeFromArray:", types);
            return available != 0;
        }
        finally { DrainPool(pool); }
    }

    public static void Clear()
    {
        EnsureFrameworkLoaded();
        nint pool = CreatePool();
        try { ObjCRuntime.Send(GetGeneralPasteboard(), "clearContents"); }
        finally { DrainPool(pool); }
    }

    public static unsafe void SetImage(byte[] png)
    {
        if (png.Length == 0) return;
        EnsureFrameworkLoaded();
        nint pool = CreatePool();
        try
        {
            nint nsData;
            fixed (byte* ptr = png)
            {
                nint alloc = ObjCRuntime.Send(ObjCRuntime.GetClass("NSData"), "alloc");
                nsData = ObjCRuntime.objc_msgSend_bytes(alloc,
                    ObjCRuntime.Sel("initWithBytes:length:"),
                    (nint)ptr, (nuint)png.Length);
            }
            nint nsImage = ObjCRuntime.Send(
                ObjCRuntime.Send(ObjCRuntime.GetClass("NSImage"), "alloc"),
                "initWithData:", nsData);
            ObjCRuntime.Send(nsData, "release");
            if (nsImage == 0) return;
            nint pb = GetGeneralPasteboard();
            ObjCRuntime.Send(pb, "clearContents");
            ObjCRuntime.Send(pb, "writeObjects:", ObjCRuntime.ArrayWithObject(nsImage));
            ObjCRuntime.Send(nsImage, "release");
        }
        finally { DrainPool(pool); }
    }
}
```

- [ ] **Step 4: Run tests**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet test ShareXMac.Tests/ShareXMac.Tests.csproj --filter "MacClipboardTests" 2>&1 | tail -5
```
Expected: 4 passed

- [ ] **Step 5: Run all tests**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet test ShareXMac.Tests/ShareXMac.Tests.csproj 2>&1 | tail -3
```
Expected: all pass

- [ ] **Step 6: Commit**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && git add -A && git commit -m "fix: MacClipboard autorelease pools and SetImage"
```

---

### Task 2: AppSettings model + SettingsService

**Files:**
- Create: `ShareXMac/Models/AppSettings.cs`
- Create: `ShareXMac/Services/SettingsService.cs`
- Test: `ShareXMac.Tests/SettingsServiceTests.cs`

- [ ] **Step 1: Write failing tests**

Create `ShareXMac.Tests/SettingsServiceTests.cs`:
```csharp
using ShareXMac.Models;
using ShareXMac.Services;
using Xunit;

namespace ShareXMac.Tests;

public class SettingsServiceTests
{
    [Fact]
    public void AppSettings_Defaults_AreReasonable()
    {
        var s = new AppSettings();
        Assert.False(string.IsNullOrEmpty(s.SavePath));
        Assert.True(s.AutoCopyImage);
        Assert.True(s.ShowPostCaptureToolbar);
    }

    [Fact]
    public void SettingsService_SaveAndLoad_RoundTrips()
    {
        string tempFile = Path.Combine(Path.GetTempPath(), $"sharexmac-test-{Guid.NewGuid():N}.json");
        try
        {
            var svc = new SettingsService(tempFile);
            svc.Current.SavePath = "/tmp/sharexmac-test";
            svc.Current.AutoCopyImage = false;
            svc.Save();

            var svc2 = new SettingsService(tempFile);
            Assert.Equal("/tmp/sharexmac-test", svc2.Current.SavePath);
            Assert.False(svc2.Current.AutoCopyImage);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void SettingsService_MissingFile_ReturnsDefaults()
    {
        string nonexistent = Path.Combine(Path.GetTempPath(), $"no-such-file-{Guid.NewGuid():N}.json");
        var svc = new SettingsService(nonexistent);
        Assert.False(string.IsNullOrEmpty(svc.Current.SavePath));
    }
}
```

- [ ] **Step 2: Run to confirm failure**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet test ShareXMac.Tests/ShareXMac.Tests.csproj --filter "SettingsServiceTests" 2>&1 | tail -5
```
Expected: compile error

- [ ] **Step 3: Create AppSettings.cs**

Create `ShareXMac/Models/AppSettings.cs`:
```csharp
namespace ShareXMac.Models;

public class AppSettings
{
    public string SavePath { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
        "ShareX Mac");
    public bool AutoCopyImage { get; set; } = true;
    public bool ShowPostCaptureToolbar { get; set; } = true;
    public int PostCaptureToolbarTimeoutSeconds { get; set; } = 8;
}
```

- [ ] **Step 4: Create SettingsService.cs**

Create `ShareXMac/Services/SettingsService.cs`:
```csharp
using Newtonsoft.Json;
using ShareXMac.Models;

namespace ShareXMac.Services;

public class SettingsService
{
    private readonly string _filePath;

    public AppSettings Current { get; private set; }

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
    }
}
```

- [ ] **Step 5: Run tests**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet test ShareXMac.Tests/ShareXMac.Tests.csproj --filter "SettingsServiceTests" -v normal 2>&1 | tail -10
```
Expected: 3 passed

- [ ] **Step 6: Commit**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && git add -A && git commit -m "feat: add AppSettings model and SettingsService"
```

---

### Task 3: HistoryService

**Files:**
- Create: `ShareXMac/Models/CaptureResult.cs`
- Create: `ShareXMac/Services/HistoryService.cs`
- Test: `ShareXMac.Tests/HistoryServiceTests.cs`

- [ ] **Step 1: Write failing tests**

Create `ShareXMac.Tests/HistoryServiceTests.cs`:
```csharp
using ShareXMac.Services;
using Xunit;

namespace ShareXMac.Tests;

public class HistoryServiceTests
{
    [Fact]
    public void AddCapture_ThenGetItems_ContainsEntry()
    {
        string tempFile = Path.Combine(Path.GetTempPath(), $"sharexmac-hist-{Guid.NewGuid():N}.json");
        try
        {
            var svc = new HistoryService(tempFile);
            svc.AddCapture("/tmp/test.png");
            var items = svc.GetItems();
            Assert.Single(items);
            Assert.Equal("test.png", items[0].FileName);
            Assert.Equal("/tmp/test.png", items[0].FilePath);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task GetItemsAsync_ReturnsItems()
    {
        string tempFile = Path.Combine(Path.GetTempPath(), $"sharexmac-hist-{Guid.NewGuid():N}.json");
        try
        {
            var svc = new HistoryService(tempFile);
            svc.AddCapture("/tmp/async-test.png");
            var items = await svc.GetItemsAsync();
            Assert.NotEmpty(items);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void MissingFile_GetItems_ReturnsEmpty()
    {
        string nonexistent = Path.Combine(Path.GetTempPath(), $"no-hist-{Guid.NewGuid():N}.json");
        var svc = new HistoryService(nonexistent);
        Assert.Empty(svc.GetItems());
    }
}
```

- [ ] **Step 2: Run to confirm failure**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet test ShareXMac.Tests/ShareXMac.Tests.csproj --filter "HistoryServiceTests" 2>&1 | tail -5
```
Expected: compile error

- [ ] **Step 3: Create CaptureResult.cs**

Create `ShareXMac/Models/CaptureResult.cs`:
```csharp
namespace ShareXMac.Models;

public record CaptureResult(byte[] ImageData, string FilePath);
```

- [ ] **Step 4: Create HistoryService.cs**

Create `ShareXMac/Services/HistoryService.cs`:
```csharp
using ShareX.HistoryLib;

namespace ShareXMac.Services;

public class HistoryService
{
    private readonly HistoryManagerJSON _manager;

    public HistoryService(string historyFilePath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(historyFilePath)!);
        _manager = new HistoryManagerJSON(historyFilePath);
    }

    public void AddCapture(string filePath, string? url = null)
    {
        _manager.AppendHistoryItem(new HistoryItem
        {
            FileName = Path.GetFileName(filePath),
            FilePath = filePath,
            DateTime = DateTime.Now,
            Type = "Image",
            Host = url != null ? new Uri(url).Host : "",
            URL = url ?? ""
        });
    }

    public List<HistoryItem> GetItems() => _manager.GetHistoryItems();

    public Task<List<HistoryItem>> GetItemsAsync() => _manager.GetHistoryItemsAsync();
}
```

- [ ] **Step 5: Run tests**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet test ShareXMac.Tests/ShareXMac.Tests.csproj --filter "HistoryServiceTests" -v normal 2>&1 | tail -10
```
Expected: 3 passed

- [ ] **Step 6: Commit**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && git add -A && git commit -m "feat: add CaptureResult model and HistoryService"
```

---

### Task 4: PostCaptureViewModel

**Files:**
- Create: `ShareXMac/ViewModels/PostCaptureViewModel.cs`
- Test: `ShareXMac.Tests/PostCaptureViewModelTests.cs`

- [ ] **Step 1: Write failing tests**

Create `ShareXMac.Tests/PostCaptureViewModelTests.cs`:
```csharp
using ShareXMac.Models;
using ShareXMac.ViewModels;
using Xunit;

namespace ShareXMac.Tests;

public class PostCaptureViewModelTests
{
    // Minimal 1x1 white PNG
    private static readonly byte[] MinimalPng = new byte[]
    {
        0x89,0x50,0x4E,0x47,0x0D,0x0A,0x1A,0x0A,0x00,0x00,0x00,0x0D,
        0x49,0x48,0x44,0x52,0x00,0x00,0x00,0x01,0x00,0x00,0x00,0x01,
        0x08,0x02,0x00,0x00,0x00,0x90,0x77,0x53,0xDE,0x00,0x00,0x00,
        0x0C,0x49,0x44,0x41,0x54,0x08,0xD7,0x63,0xF8,0xCF,0xC0,0x00,
        0x00,0x00,0x02,0x00,0x01,0xE2,0x21,0xBC,0x33,0x00,0x00,0x00,
        0x00,0x49,0x45,0x4E,0x44,0xAE,0x42,0x60,0x82
    };

    [Fact]
    public void Constructor_SetsFilePath()
    {
        var result = new CaptureResult(MinimalPng, "/tmp/test.png");
        var vm = new PostCaptureViewModel(result);
        Assert.Equal("/tmp/test.png", vm.FilePath);
    }

    [Fact]
    public void Constructor_LoadsThumbnail()
    {
        var result = new CaptureResult(MinimalPng, "/tmp/test.png");
        var vm = new PostCaptureViewModel(result);
        Assert.NotNull(vm.Thumbnail);
    }

    [Fact]
    public void DismissCommand_RaisesCloseRequested()
    {
        var result = new CaptureResult(MinimalPng, "/tmp/test.png");
        var vm = new PostCaptureViewModel(result);
        bool raised = false;
        vm.CloseRequested += () => raised = true;
        vm.DismissCommand.Execute(null);
        Assert.True(raised);
    }

    [Fact]
    public void CopyPathCommand_DoesNotThrow()
    {
        var result = new CaptureResult(MinimalPng, "/tmp/test.png");
        var vm = new PostCaptureViewModel(result);
        vm.CopyPathCommand.Execute(null); // copies "/tmp/test.png" to clipboard
    }
}
```

- [ ] **Step 2: Run to confirm failure**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet test ShareXMac.Tests/ShareXMac.Tests.csproj --filter "PostCaptureViewModelTests" 2>&1 | tail -5
```
Expected: compile error

- [ ] **Step 3: Create PostCaptureViewModel.cs**

Create `ShareXMac/ViewModels/PostCaptureViewModel.cs`:
```csharp
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShareXMac.Models;
using ShareXMac.ScreenCaptureLib;
using System.Diagnostics;

namespace ShareXMac.ViewModels;

public partial class PostCaptureViewModel : ObservableObject
{
    public string FilePath { get; }
    public Bitmap Thumbnail { get; }
    private readonly byte[] _imageData;

    public event Action? CloseRequested;

    public PostCaptureViewModel(CaptureResult result)
    {
        FilePath = result.FilePath;
        _imageData = result.ImageData;
        using var ms = new MemoryStream(result.ImageData);
        Thumbnail = new Bitmap(ms);
    }

    [RelayCommand]
    private void CopyImage() => MacClipboard.SetImage(_imageData);

    [RelayCommand]
    private void CopyPath() => MacClipboard.SetText(FilePath);

    [RelayCommand]
    private void OpenInFinder() =>
        Process.Start(new ProcessStartInfo("open", $"-R \"{FilePath}\"")
            { UseShellExecute = false });

    [RelayCommand]
    private void Dismiss() => CloseRequested?.Invoke();
}
```

- [ ] **Step 4: Run tests**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet test ShareXMac.Tests/ShareXMac.Tests.csproj --filter "PostCaptureViewModelTests" -v normal 2>&1 | tail -10
```
Expected: 4 passed

- [ ] **Step 5: Commit**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && git add -A && git commit -m "feat: add PostCaptureViewModel"
```

---

### Task 5: PostCaptureWindow

**Files:**
- Create: `ShareXMac/Views/PostCaptureWindow.axaml`
- Create: `ShareXMac/Views/PostCaptureWindow.axaml.cs`

No new unit tests — window correctness is verified by running the app. This task is confirmed by the build passing.

- [ ] **Step 1: Create PostCaptureWindow.axaml**

Create `ShareXMac/Views/PostCaptureWindow.axaml`:
```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="using:ShareXMac.ViewModels"
        x:Class="ShareXMac.Views.PostCaptureWindow"
        x:DataType="vm:PostCaptureViewModel"
        Width="360" Height="240"
        MinWidth="360" MinHeight="240"
        CanResize="False"
        SystemDecorations="BorderOnly"
        WindowStartupLocation="Manual"
        Title="ShareX Mac — Capture"
        Background="#FF1E1E1E">
    <Grid RowDefinitions="*,Auto" Margin="12">
        <Border Grid.Row="0" Background="#FF2D2D2D" CornerRadius="4">
            <Image Source="{Binding Thumbnail}" Stretch="Uniform" Margin="4" />
        </Border>
        <StackPanel Grid.Row="1" Orientation="Horizontal"
                    HorizontalAlignment="Center" Spacing="8" Margin="0,10,0,0">
            <Button Content="Copy Image"
                    Command="{Binding CopyImageCommand}"
                    Background="#FF0078D4" Foreground="White"
                    Padding="12,6" />
            <Button Content="Copy Path"
                    Command="{Binding CopyPathCommand}"
                    Padding="12,6" />
            <Button Content="Open in Finder"
                    Command="{Binding OpenInFinderCommand}"
                    Padding="12,6" />
            <Button Content="Dismiss"
                    Command="{Binding DismissCommand}"
                    Padding="12,6" />
        </StackPanel>
    </Grid>
</Window>
```

- [ ] **Step 2: Create PostCaptureWindow.axaml.cs**

Create `ShareXMac/Views/PostCaptureWindow.axaml.cs`:
```csharp
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ShareXMac.ViewModels;

namespace ShareXMac.Views;

public partial class PostCaptureWindow : Window
{
    public PostCaptureWindow(PostCaptureViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.CloseRequested += Close;
        // Position bottom-right of primary screen, inset 20px
        this.Loaded += (_, _) => PositionBottomRight();
        // Auto-dismiss after timeout
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
```

- [ ] **Step 3: Add AutoDismissSeconds to PostCaptureViewModel**

Open `ShareXMac/ViewModels/PostCaptureViewModel.cs` and add:
```csharp
public int AutoDismissSeconds { get; init; } = 8;
```
Add it after `public event Action? CloseRequested;`.

- [ ] **Step 4: Build to verify AXAML compiles**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet build ShareXMac/ShareXMac.csproj 2>&1 | grep -E "^.*error" | head -10
```
Expected: 0 errors. Common errors and fixes:
- `x:DataType` unknown: remove it (it's optional in Avalonia 11)
- `Screens.Primary` null reference at design time: the `Loaded` event handler guards this
- `AutoDismissSeconds` not found: verify Step 3 was applied

- [ ] **Step 5: Run all tests (no regressions)**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet test ShareXMac.Tests/ShareXMac.Tests.csproj 2>&1 | tail -3
```
Expected: all pass

- [ ] **Step 6: Commit**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && git add -A && git commit -m "feat: add PostCaptureWindow with thumbnail and action buttons"
```

---

### Task 6: SettingsViewModel + SettingsWindow

**Files:**
- Create: `ShareXMac/ViewModels/SettingsViewModel.cs`
- Create: `ShareXMac/Views/SettingsWindow.axaml`
- Create: `ShareXMac/Views/SettingsWindow.axaml.cs`
- Test: `ShareXMac.Tests/SettingsServiceTests.cs` (add ViewModel tests)

- [ ] **Step 1: Write failing tests**

Add to `ShareXMac.Tests/SettingsServiceTests.cs`:
```csharp
public class SettingsViewModelTests
{
    [Fact]
    public void SettingsViewModel_LoadsCurrentValues()
    {
        string tempFile = Path.Combine(Path.GetTempPath(), $"sharexmac-vm-{Guid.NewGuid():N}.json");
        try
        {
            var svc = new SettingsService(tempFile);
            svc.Current.SavePath = "/tmp/pics";
            svc.Current.AutoCopyImage = false;

            var vm = new SettingsViewModel(svc);
            Assert.Equal("/tmp/pics", vm.SavePath);
            Assert.False(vm.AutoCopyImage);
        }
        finally { if (File.Exists(tempFile)) File.Delete(tempFile); }
    }

    [Fact]
    public void SettingsViewModel_SaveCommand_PersistsValues()
    {
        string tempFile = Path.Combine(Path.GetTempPath(), $"sharexmac-vm2-{Guid.NewGuid():N}.json");
        try
        {
            var svc = new SettingsService(tempFile);
            var vm = new SettingsViewModel(svc);
            vm.SavePath = "/tmp/new-path";
            vm.AutoCopyImage = false;
            vm.SaveCommand.Execute(null);

            // reload
            var svc2 = new SettingsService(tempFile);
            Assert.Equal("/tmp/new-path", svc2.Current.SavePath);
            Assert.False(svc2.Current.AutoCopyImage);
        }
        finally { if (File.Exists(tempFile)) File.Delete(tempFile); }
    }
}
```

Also add `using ShareXMac.ViewModels;` to the top of `SettingsServiceTests.cs`.

- [ ] **Step 2: Run to confirm failure**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet test ShareXMac.Tests/ShareXMac.Tests.csproj --filter "SettingsViewModelTests" 2>&1 | tail -5
```
Expected: compile error

- [ ] **Step 3: Create SettingsViewModel.cs**

Create `ShareXMac/ViewModels/SettingsViewModel.cs`:
```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShareXMac.Services;

namespace ShareXMac.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly SettingsService _service;

    [ObservableProperty] private string _savePath = "";
    [ObservableProperty] private bool _autoCopyImage;
    [ObservableProperty] private bool _showPostCaptureToolbar;
    [ObservableProperty] private int _postCaptureTimeoutSeconds;

    public event Action? CloseRequested;

    public SettingsViewModel(SettingsService service)
    {
        _service = service;
        var s = service.Current;
        SavePath = s.SavePath;
        AutoCopyImage = s.AutoCopyImage;
        ShowPostCaptureToolbar = s.ShowPostCaptureToolbar;
        PostCaptureTimeoutSeconds = s.PostCaptureToolbarTimeoutSeconds;
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

    [RelayCommand]
    private void Save()
    {
        _service.Current.SavePath = SavePath;
        _service.Current.AutoCopyImage = AutoCopyImage;
        _service.Current.ShowPostCaptureToolbar = ShowPostCaptureToolbar;
        _service.Current.PostCaptureToolbarTimeoutSeconds = PostCaptureTimeoutSeconds;
        _service.Save();
        CloseRequested?.Invoke();
    }
}
```

- [ ] **Step 4: Create SettingsWindow.axaml**

Create `ShareXMac/Views/SettingsWindow.axaml`:
```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="using:ShareXMac.ViewModels"
        x:Class="ShareXMac.Views.SettingsWindow"
        x:DataType="vm:SettingsViewModel"
        Width="480" Height="320"
        CanResize="False"
        WindowStartupLocation="CenterScreen"
        Title="ShareX Mac — Settings">
    <StackPanel Margin="20" Spacing="14">
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

        <Button Content="Save" Command="{Binding SaveCommand}"
                HorizontalAlignment="Right" Background="#FF0078D4"
                Foreground="White" Padding="16,8" />
    </StackPanel>
</Window>
```

- [ ] **Step 5: Create SettingsWindow.axaml.cs**

Create `ShareXMac/Views/SettingsWindow.axaml.cs`:
```csharp
using Avalonia.Controls;
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
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
```

- [ ] **Step 6: Run ViewModel tests**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet test ShareXMac.Tests/ShareXMac.Tests.csproj --filter "SettingsViewModelTests" -v normal 2>&1 | tail -10
```
Expected: 2 passed

- [ ] **Step 7: Build check**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet build ShareXMac/ShareXMac.csproj 2>&1 | grep -E "^.*error" | head -10
```
Expected: 0 errors. If `x:DataType` causes issues, remove it.

- [ ] **Step 8: Commit**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && git add -A && git commit -m "feat: add SettingsViewModel and SettingsWindow"
```

---

### Task 7: HistoryViewModel + HistoryWindow

**Files:**
- Create: `ShareXMac/ViewModels/HistoryViewModel.cs`
- Create: `ShareXMac/Views/HistoryWindow.axaml`
- Create: `ShareXMac/Views/HistoryWindow.axaml.cs`
- Test: `ShareXMac.Tests/HistoryServiceTests.cs` (add ViewModel tests)

- [ ] **Step 1: Write failing tests**

Add to `ShareXMac.Tests/HistoryServiceTests.cs`:
```csharp
public class HistoryViewModelTests
{
    [Fact]
    public void HistoryViewModel_Constructor_DoesNotThrow()
    {
        string tempFile = Path.Combine(Path.GetTempPath(), $"sharexmac-hvm-{Guid.NewGuid():N}.json");
        try
        {
            var svc = new HistoryService(tempFile);
            var vm = new HistoryViewModel(svc);
            Assert.NotNull(vm);
        }
        finally { if (File.Exists(tempFile)) File.Delete(tempFile); }
    }

    [Fact]
    public void HistoryViewModel_SearchText_FiltersItems()
    {
        string tempFile = Path.Combine(Path.GetTempPath(), $"sharexmac-hvm2-{Guid.NewGuid():N}.json");
        try
        {
            var svc = new HistoryService(tempFile);
            svc.AddCapture("/tmp/screenshot-001.png");
            svc.AddCapture("/tmp/screenshot-002.png");
            svc.AddCapture("/tmp/recording-001.mp4");
            // Force load
            var vm = new HistoryViewModel(svc);
            vm.LoadItems();
            // Before filter
            Assert.Equal(3, vm.FilteredItems.Count);
            // Filter by "recording"
            vm.SearchText = "recording";
            Assert.Equal(1, vm.FilteredItems.Count);
            Assert.Contains("recording", vm.FilteredItems[0].FileName);
        }
        finally { if (File.Exists(tempFile)) File.Delete(tempFile); }
    }
}
```

Also add `using ShareXMac.ViewModels;` to the top of `HistoryServiceTests.cs`.

- [ ] **Step 2: Run to confirm failure**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet test ShareXMac.Tests/ShareXMac.Tests.csproj --filter "HistoryViewModelTests" 2>&1 | tail -5
```
Expected: compile error

- [ ] **Step 3: Create HistoryViewModel.cs**

Create `ShareXMac/ViewModels/HistoryViewModel.cs`:
```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShareX.HistoryLib;
using ShareXMac.Services;
using System.Diagnostics;

namespace ShareXMac.ViewModels;

public partial class HistoryViewModel : ObservableObject
{
    private readonly HistoryService _service;
    private List<HistoryItem> _allItems = new();

    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private HistoryItem? _selectedItem;

    public ObservableCollection<HistoryItem> FilteredItems { get; } = new();

    public event Action? CloseRequested;

    public HistoryViewModel(HistoryService service)
    {
        _service = service;
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    public void LoadItems()
    {
        _allItems = _service.GetItems();
        _allItems.Reverse(); // newest first
        ApplyFilter();
    }

    public async Task LoadItemsAsync()
    {
        _allItems = await _service.GetItemsAsync();
        _allItems.Reverse();
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        FilteredItems.Clear();
        string q = SearchText.Trim().ToLowerInvariant();
        foreach (var item in _allItems)
        {
            if (string.IsNullOrEmpty(q)
                || (item.FileName?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false)
                || (item.URL?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false))
            {
                FilteredItems.Add(item);
            }
        }
    }

    [RelayCommand]
    private void OpenSelected()
    {
        if (SelectedItem?.FilePath is { } path && File.Exists(path))
            Process.Start(new ProcessStartInfo("open", $"-R \"{path}\"")
                { UseShellExecute = false });
    }

    [RelayCommand]
    private void Close() => CloseRequested?.Invoke();
}
```

- [ ] **Step 4: Create HistoryWindow.axaml**

Create `ShareXMac/Views/HistoryWindow.axaml`:
```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="using:ShareXMac.ViewModels"
        xmlns:history="using:ShareX.HistoryLib"
        x:Class="ShareXMac.Views.HistoryWindow"
        x:DataType="vm:HistoryViewModel"
        Width="700" Height="450"
        WindowStartupLocation="CenterScreen"
        Title="ShareX Mac — History">
    <Grid RowDefinitions="Auto,*,Auto">
        <!-- Search bar -->
        <TextBox Grid.Row="0" Text="{Binding SearchText}"
                 Watermark="Search by filename or URL…"
                 Margin="8,8,8,4" />

        <!-- History list -->
        <ListBox Grid.Row="1"
                 ItemsSource="{Binding FilteredItems}"
                 SelectedItem="{Binding SelectedItem}"
                 Margin="8,4">
            <ListBox.ItemTemplate>
                <DataTemplate x:DataType="history:HistoryItem">
                    <Grid ColumnDefinitions="140,*,180" Margin="4,2">
                        <TextBlock Grid.Column="0"
                                   Text="{Binding DateTime, StringFormat='{}{0:yyyy-MM-dd HH:mm}'}"
                                   VerticalAlignment="Center" FontSize="12" />
                        <TextBlock Grid.Column="1"
                                   Text="{Binding FileName}"
                                   VerticalAlignment="Center"
                                   TextTrimming="CharacterEllipsis" />
                        <TextBlock Grid.Column="2"
                                   Text="{Binding URL}"
                                   VerticalAlignment="Center"
                                   FontSize="11"
                                   Foreground="#FF0078D4"
                                   TextTrimming="CharacterEllipsis" />
                    </Grid>
                </DataTemplate>
            </ListBox.ItemTemplate>
        </ListBox>

        <!-- Footer buttons -->
        <StackPanel Grid.Row="2" Orientation="Horizontal"
                    HorizontalAlignment="Right" Margin="8" Spacing="8">
            <Button Content="Open in Finder"
                    Command="{Binding OpenSelectedCommand}"
                    IsEnabled="{Binding SelectedItem, Converter={x:Static ObjectConverters.IsNotNull}}"
                    Padding="12,6" />
            <Button Content="Close"
                    Command="{Binding CloseCommand}"
                    Padding="12,6" />
        </StackPanel>
    </Grid>
</Window>
```

- [ ] **Step 5: Create HistoryWindow.axaml.cs**

Create `ShareXMac/Views/HistoryWindow.axaml.cs`:
```csharp
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
```

- [ ] **Step 6: Run ViewModel tests**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet test ShareXMac.Tests/ShareXMac.Tests.csproj --filter "HistoryViewModelTests" -v normal 2>&1 | tail -10
```
Expected: 2 passed

- [ ] **Step 7: Build check**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet build ShareXMac/ShareXMac.csproj 2>&1 | grep -E "^.*error" | head -10
```
Expected: 0 errors

Common XAML errors and fixes:
- `ObjectConverters` not found: add `xmlns:conv="clr-namespace:Avalonia.Data.Converters;assembly=Avalonia"` and use `conv:ObjectConverters.IsNotNull`, or replace with a simple binding to `SelectedItem` and handle in code-behind
- `x:DataType` on DataTemplate: if it causes issues, remove it

- [ ] **Step 8: Commit**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && git add -A && git commit -m "feat: add HistoryViewModel and HistoryWindow"
```

---

### Task 8: Wire everything together

Update `TrayViewModel` and `App.axaml.cs` to inject `SettingsService` and `HistoryService`, replace the stub `OnCaptureComplete` with the full flow, and implement `OpenSettings`/`OpenHistory`.

**Files:**
- Modify: `ShareXMac/ViewModels/TrayViewModel.cs`
- Modify: `ShareXMac/App.axaml.cs`
- Modify: `ShareXMac.Tests/TrayViewModelTests.cs`

- [ ] **Step 1: Write failing test**

Add to `ShareXMac.Tests/TrayViewModelTests.cs`:
```csharp
[Fact]
public void TrayViewModel_WithServices_DoesNotThrow()
{
    string settingsFile = Path.Combine(Path.GetTempPath(), $"s-{Guid.NewGuid():N}.json");
    string historyFile = Path.Combine(Path.GetTempPath(), $"h-{Guid.NewGuid():N}.json");
    try
    {
        var vm = new TrayViewModel(
            new StubScreenCapture(),
            new SettingsService(settingsFile),
            new HistoryService(historyFile));
        Assert.NotNull(vm.CaptureRegionCommand);
    }
    finally
    {
        if (File.Exists(settingsFile)) File.Delete(settingsFile);
        if (File.Exists(historyFile)) File.Delete(historyFile);
    }
}
```

Add `using ShareXMac.Services;` to `TrayViewModelTests.cs`.

- [ ] **Step 2: Run to confirm failure**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet test ShareXMac.Tests/ShareXMac.Tests.csproj --filter "TrayViewModel_WithServices" 2>&1 | tail -5
```
Expected: compile error (TrayViewModel doesn't accept services yet)

- [ ] **Step 3: Replace TrayViewModel.cs**

Replace `ShareXMac/ViewModels/TrayViewModel.cs` entirely:
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
    private bool _isRecording;

    public TrayViewModel(
        IScreenCapture capture,
        SettingsService settings,
        HistoryService history)
    {
        _capture = capture;
        _settings = settings;
        _history = history;
    }

    [RelayCommand]
    private async Task CaptureRegion()
    {
        byte[]? data = await _capture.CaptureRegionAsync();
        if (data != null)
            await OnCaptureComplete(data);
    }

    [RelayCommand]
    private async Task CaptureWindow()
    {
        byte[]? data = await _capture.CaptureWindowAsync();
        if (data != null)
            await OnCaptureComplete(data);
    }

    [RelayCommand]
    private async Task CaptureFullscreen()
    {
        byte[]? data = await _capture.CaptureFullscreenAsync();
        if (data != null)
            await OnCaptureComplete(data);
    }

    [RelayCommand]
    private async Task RecordVideo()
    {
        if (_isRecording)
        {
            await _capture.StopRecordingAsync();
            _isRecording = false;
        }
        else
        {
            string dir = _settings.Current.SavePath;
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, $"ShareX-{DateTime.Now:yyyy-MM-dd-HHmmss}.mp4");
            await _capture.StartRecordingAsync(path, RecordingFormat.MP4);
            _isRecording = true;
        }
    }

    [RelayCommand]
    private async Task RecordGif()
    {
        if (_isRecording)
        {
            await _capture.StopRecordingAsync();
            _isRecording = false;
        }
        else
        {
            string dir = _settings.Current.SavePath;
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, $"ShareX-{DateTime.Now:yyyy-MM-dd-HHmmss}.gif");
            await _capture.StartRecordingAsync(path, RecordingFormat.GIF);
            _isRecording = true;
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
        // Ensure save directory exists
        string dir = _settings.Current.SavePath;
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, $"ShareX-{DateTime.Now:yyyy-MM-dd-HHmmss}.png");
        await File.WriteAllBytesAsync(path, data);

        // Record in history
        _history.AddCapture(path);

        // Clipboard
        if (_settings.Current.AutoCopyImage)
            MacClipboard.SetImage(data);
        else
            MacClipboard.SetText(path);

        // Post-capture toolbar
        if (_settings.Current.ShowPostCaptureToolbar)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var result = new CaptureResult(data, path);
                var vm = new PostCaptureViewModel(result)
                {
                    AutoDismissSeconds = _settings.Current.PostCaptureToolbarTimeoutSeconds
                };
                new PostCaptureWindow(vm).Show();
            });
        }
    }
}
```

- [ ] **Step 4: Update TrayViewModelTests.cs existing tests**

The two existing tests in `TrayViewModelTests.cs` use `new TrayViewModel(new StubScreenCapture())`. Update them to also pass `SettingsService` and `HistoryService`.

Read `ShareXMac.Tests/TrayViewModelTests.cs` and replace all `new TrayViewModel(new StubScreenCapture())` with:
```csharp
new TrayViewModel(
    new StubScreenCapture(),
    new SettingsService(Path.GetTempFileName()),
    new HistoryService(Path.GetTempFileName()))
```

- [ ] **Step 5: Update App.axaml.cs**

Replace `ShareXMac/App.axaml.cs` entirely:
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

        var settings = new SettingsService(Path.Combine(appSupport, "settings.json"));
        var history  = new HistoryService(Path.Combine(appSupport, "history.json"));
        var capture  = new MacScreenCapture();

        DataContext = new TrayViewModel(capture, settings, history);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.ShutdownMode = Avalonia.Controls.ShutdownMode.OnExplicitShutdown;

        base.OnFrameworkInitializationCompleted();
    }
}
```

- [ ] **Step 6: Run all tests**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet test ShareXMac.Tests/ShareXMac.Tests.csproj -v normal 2>&1 | tail -10
```
Expected: all pass (count should be 30+)

- [ ] **Step 7: Full build check**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet build ShareXMac/ShareXMac.csproj 2>&1 | tail -5
```
Expected: `0 Error(s)`

- [ ] **Step 8: Commit**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && git add -A && git commit -m "feat: wire SettingsService and HistoryService into TrayViewModel"
```

---

## Plan Complete

After all 8 tasks:
- Taking a screenshot shows a floating post-capture toolbar (bottom-right, auto-dismiss in 8s) with Copy Image / Copy Path / Open in Finder / Dismiss
- Settings window lets users change the save path, clipboard behavior, and toolbar timeout
- History window shows all past captures, searchable, with Open in Finder
- Captures save to `~/Pictures/ShareX Mac/` by default (configurable)
- Capture history persists at `~/Library/Application Support/ShareX-Mac/history.json`
- MacClipboard no longer leaks NSStrings

**Next plan:** Plan 4 — Uploaders integration: wire UploadersLib to the post-capture toolbar "Upload" button, OAuth flows, URL copy on success.
