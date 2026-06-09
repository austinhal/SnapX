# After-Capture Workflow Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace three scattered global booleans (`AutoCopyImage`, `ShowPostCaptureToolbar`, `AutoUploadAfterCapture`) in `AppSettings` with a structured `WorkflowSettings` model that supports global defaults plus per-capture-type overrides for four actions: show toolbar, auto-copy image, auto-upload, and save to custom folder.

**Architecture:** Three new model classes (`CaptureType`/`ResolvedWorkflow`, `CaptureWorkflow`, `WorkflowSettings`) are created first as a pure addition. Then `AppSettings` gains a `Workflow` property and `TrayViewModel.OnCaptureComplete` gains a `CaptureType` parameter, replacing the old boolean reads with `Workflow.Resolve(captureType)`. Finally, `SettingsViewModel` and `SettingsWindow.axaml` are migrated: old boolean properties removed, 16 workflow properties added, and the flat settings layout converted to a `TabControl` with General / Hotkeys / Workflow tabs.

**Tech Stack:** .NET 10, Avalonia 11.2.1, CommunityToolkit.Mvvm 8.4.0, xUnit, Newtonsoft.Json

---

## File Map

| Action | Path |
|--------|------|
| Create | `ShareXMac/Models/CaptureType.cs` |
| Create | `ShareXMac/Models/CaptureWorkflow.cs` |
| Create | `ShareXMac/Models/WorkflowSettings.cs` |
| Create | `ShareXMac.Tests/WorkflowSettingsTests.cs` |
| Modify | `ShareXMac/Models/AppSettings.cs` |
| Modify | `ShareXMac/ViewModels/TrayViewModel.cs` |
| Modify | `ShareXMac/ViewModels/SettingsViewModel.cs` |
| Modify | `ShareXMac/Views/SettingsWindow.axaml` |
| Modify | `ShareXMac.Tests/SettingsViewModelTests.cs` |

---

## Task 1: New Model Types + Tests

**Files:**
- Create: `ShareXMac/Models/CaptureType.cs`
- Create: `ShareXMac/Models/CaptureWorkflow.cs`
- Create: `ShareXMac/Models/WorkflowSettings.cs`
- Create: `ShareXMac.Tests/WorkflowSettingsTests.cs`

- [ ] **Step 1: Write failing tests**

Create `ShareXMac.Tests/WorkflowSettingsTests.cs`:

```csharp
using ShareXMac.Models;
using Xunit;

namespace ShareXMac.Tests;

public class WorkflowSettingsTests
{
    [Fact]
    public void Resolve_InheritsGlobalDefaults_WhenNoOverrideSet()
    {
        var wf = new WorkflowSettings { ShowToolbar = true, AutoCopyImage = true, AutoUpload = false, SaveFolder = null };
        var r = wf.Resolve(CaptureType.Region);
        Assert.True(r.ShowToolbar);
        Assert.True(r.AutoCopyImage);
        Assert.False(r.AutoUpload);
        Assert.Null(r.SaveFolder);
    }

    [Fact]
    public void Resolve_ShowToolbarOverrideTrue_WinsOverGlobalFalse()
    {
        var wf = new WorkflowSettings { ShowToolbar = false };
        wf.RegionOverride.ShowToolbar = true;
        Assert.True(wf.Resolve(CaptureType.Region).ShowToolbar);
    }

    [Fact]
    public void Resolve_ShowToolbarOverrideFalse_WinsOverGlobalTrue()
    {
        var wf = new WorkflowSettings { ShowToolbar = true };
        wf.WindowOverride.ShowToolbar = false;
        Assert.False(wf.Resolve(CaptureType.Window).ShowToolbar);
    }

    [Fact]
    public void Resolve_NullOverride_FallsThroughToGlobal()
    {
        var wf = new WorkflowSettings { ShowToolbar = true };
        // RegionOverride.ShowToolbar is null by default
        Assert.True(wf.Resolve(CaptureType.Region).ShowToolbar);
    }

    [Fact]
    public void Resolve_SaveFolderOverride_WinsOverGlobal()
    {
        var wf = new WorkflowSettings { SaveFolder = "/global" };
        wf.FullscreenOverride.SaveFolder = "/fullscreen";
        Assert.Equal("/fullscreen", wf.Resolve(CaptureType.Fullscreen).SaveFolder);
    }

    [Fact]
    public void Resolve_SaveFolderOverrideNull_FallsBackToGlobal()
    {
        var wf = new WorkflowSettings { SaveFolder = "/global" };
        // FullscreenOverride.SaveFolder is null by default
        Assert.Equal("/global", wf.Resolve(CaptureType.Fullscreen).SaveFolder);
    }

    [Fact]
    public void Resolve_SaveFolderBothNull_ReturnsNull()
    {
        var wf = new WorkflowSettings();
        Assert.Null(wf.Resolve(CaptureType.Region).SaveFolder);
    }

    [Fact]
    public void Resolve_AllActionsInheritForWindowCapture()
    {
        var wf = new WorkflowSettings { ShowToolbar = false, AutoCopyImage = false, AutoUpload = true, SaveFolder = "/s" };
        var r = wf.Resolve(CaptureType.Window);
        Assert.False(r.ShowToolbar);
        Assert.False(r.AutoCopyImage);
        Assert.True(r.AutoUpload);
        Assert.Equal("/s", r.SaveFolder);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
cd /Users/austin/Dev/ShareX-Mac && dotnet test ShareXMac.Tests --filter "WorkflowSettingsTests" 2>&1 | tail -20
```

Expected: compile error — `WorkflowSettings`, `CaptureType` not found.

- [ ] **Step 3: Create CaptureType.cs**

Create `ShareXMac/Models/CaptureType.cs`:

```csharp
namespace ShareXMac.Models;

public enum CaptureType { Region, Window, Fullscreen }

public record ResolvedWorkflow(
    bool ShowToolbar,
    bool AutoCopyImage,
    bool AutoUpload,
    string? SaveFolder);
```

- [ ] **Step 4: Create CaptureWorkflow.cs**

Create `ShareXMac/Models/CaptureWorkflow.cs`:

```csharp
namespace ShareXMac.Models;

public class CaptureWorkflow
{
    public bool? ShowToolbar   { get; set; }
    public bool? AutoCopyImage { get; set; }
    public bool? AutoUpload    { get; set; }
    public string? SaveFolder  { get; set; }
}
```

- [ ] **Step 5: Create WorkflowSettings.cs**

Create `ShareXMac/Models/WorkflowSettings.cs`:

```csharp
namespace ShareXMac.Models;

public class WorkflowSettings
{
    public bool ShowToolbar   { get; set; } = true;
    public bool AutoCopyImage { get; set; } = true;
    public bool AutoUpload    { get; set; } = false;
    public string? SaveFolder { get; set; }

    public CaptureWorkflow RegionOverride     { get; set; } = new();
    public CaptureWorkflow WindowOverride     { get; set; } = new();
    public CaptureWorkflow FullscreenOverride { get; set; } = new();

    public ResolvedWorkflow Resolve(CaptureType type)
    {
        var o = type switch
        {
            CaptureType.Region     => RegionOverride,
            CaptureType.Window     => WindowOverride,
            CaptureType.Fullscreen => FullscreenOverride,
            _                      => new CaptureWorkflow()
        };
        return new ResolvedWorkflow(
            ShowToolbar:   o.ShowToolbar   ?? ShowToolbar,
            AutoCopyImage: o.AutoCopyImage ?? AutoCopyImage,
            AutoUpload:    o.AutoUpload    ?? AutoUpload,
            SaveFolder:    o.SaveFolder    ?? SaveFolder);
    }
}
```

- [ ] **Step 6: Run tests to verify they pass**

```bash
cd /Users/austin/Dev/ShareX-Mac && dotnet test ShareXMac.Tests --filter "WorkflowSettingsTests" 2>&1 | tail -10
```

Expected: 8 tests pass.

- [ ] **Step 7: Run full test suite to verify no regressions**

```bash
cd /Users/austin/Dev/ShareX-Mac && dotnet test ShareXMac.Tests 2>&1 | tail -10
```

Expected: all tests pass (same count as before this task).

- [ ] **Step 8: Commit**

```bash
cd /Users/austin/Dev/ShareX-Mac
git add ShareXMac/Models/CaptureType.cs ShareXMac/Models/CaptureWorkflow.cs ShareXMac/Models/WorkflowSettings.cs ShareXMac.Tests/WorkflowSettingsTests.cs
git commit -m "feat: add WorkflowSettings model with per-type override resolution"
```

---

## Task 2: AppSettings + TrayViewModel

Add `Workflow` to `AppSettings` and update `TrayViewModel.OnCaptureComplete` to use `Resolve()`. The three old AppSettings booleans are kept in this task (removed in Task 3 along with their SettingsViewModel bindings).

**Files:**
- Modify: `ShareXMac/Models/AppSettings.cs`
- Modify: `ShareXMac/ViewModels/TrayViewModel.cs`

- [ ] **Step 1: Add Workflow to AppSettings**

Open `ShareXMac/Models/AppSettings.cs`. Add the `Workflow` property. Keep the three old booleans in place for now — `SettingsViewModel` still reads them and removing them here would break compilation until Task 3 updates `SettingsViewModel`.

Replace the entire file with:

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

    // Workflow
    public WorkflowSettings Workflow { get; set; } = new WorkflowSettings();
}
```

The old three booleans (`AutoCopyImage`, `ShowPostCaptureToolbar`, `AutoUploadAfterCapture`) will be removed in Task 3 once `SettingsViewModel` no longer references them.

- [ ] **Step 2: Update TrayViewModel.OnCaptureComplete**

Open `ShareXMac/ViewModels/TrayViewModel.cs`.

Replace the three capture command methods and `OnCaptureComplete`:

**Old `CaptureRegion`, `CaptureWindow`, `CaptureFullscreen` (lines 72–91):**

```csharp
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
```

**New versions (pass CaptureType):**

```csharp
[RelayCommand]
private async Task CaptureRegion()
{
    byte[]? data = await _capture.CaptureRegionAsync();
    if (data != null) await OnCaptureComplete(data, CaptureType.Region);
}

[RelayCommand]
private async Task CaptureWindow()
{
    byte[]? data = await _capture.CaptureWindowAsync();
    if (data != null) await OnCaptureComplete(data, CaptureType.Window);
}

[RelayCommand]
private async Task CaptureFullscreen()
{
    byte[]? data = await _capture.CaptureFullscreenAsync();
    if (data != null) await OnCaptureComplete(data, CaptureType.Fullscreen);
}
```

**Old `OnCaptureComplete` (lines 194–233):**

```csharp
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

    // Clipboard (image or path) — only if we didn't already set a URL
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
```

**New `OnCaptureComplete` (gains CaptureType param, reads Resolve()):**

```csharp
private async Task OnCaptureComplete(byte[] data, CaptureType captureType)
{
    var wf = _settings.Current.Workflow.Resolve(captureType);

    string dir = wf.SaveFolder ?? _settings.Current.SavePath;
    Directory.CreateDirectory(dir);
    string path = Path.Combine(dir, $"ShareX-{DateTime.Now:yyyy-MM-dd-HHmmss}.png");
    await File.WriteAllBytesAsync(path, data);

    string? url = null;
    if (wf.AutoUpload)
    {
        url = await _upload.UploadImageAsync(data, Path.GetFileName(path), _settings.Current);
        if (url != null)
            MacClipboard.SetText(url);
    }

    _history.AddCapture(path, url);

    if (url == null)
    {
        if (wf.AutoCopyImage)
            MacClipboard.SetImage(data);
        else
            MacClipboard.SetText(path);
    }

    if (wf.ShowToolbar)
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
```

- [ ] **Step 3: Build to verify compilation**

```bash
cd /Users/austin/Dev/ShareX-Mac && dotnet build ShareXMac/ShareXMac.csproj 2>&1 | tail -15
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 4: Run full test suite**

```bash
cd /Users/austin/Dev/ShareX-Mac && dotnet test ShareXMac.Tests 2>&1 | tail -10
```

Expected: all tests pass.

- [ ] **Step 5: Commit**

```bash
cd /Users/austin/Dev/ShareX-Mac
git add ShareXMac/Models/AppSettings.cs ShareXMac/ViewModels/TrayViewModel.cs
git commit -m "feat: wire WorkflowSettings into AppSettings and TrayViewModel"
```

---

## Task 3: SettingsViewModel + SettingsWindow + Tests

Remove the three old boolean properties from `SettingsViewModel` (and from `AppSettings` now that nothing references them), add 16 workflow properties, restructure `SettingsWindow.axaml` into a `TabControl` with General / Hotkeys / Workflow tabs, and update the tests.

**Files:**
- Modify: `ShareXMac/ViewModels/SettingsViewModel.cs`
- Modify: `ShareXMac/Models/AppSettings.cs` (remove the 3 old booleans that Task 2 kept temporarily)
- Modify: `ShareXMac/Views/SettingsWindow.axaml`
- Modify: `ShareXMac.Tests/SettingsViewModelTests.cs`

**Note:** `ShareXMac/Views/SettingsWindow.axaml.cs` does NOT need changes. `FindControl<TextBox>` searches the full visual tree and will locate hotkey TextBoxes inside the Hotkeys tab automatically.

- [ ] **Step 1: Replace SettingsViewModel.cs**

Replace the entire content of `ShareXMac/ViewModels/SettingsViewModel.cs` with:

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
    [ObservableProperty] private int    _postCaptureTimeoutSeconds;
    [ObservableProperty] private string _imgurClientId = "";
    [ObservableProperty] private ImageDestination _activeImageDestination;
    [ObservableProperty] private bool   _launchAtLogin;

    // Hotkeys — displayed and edited as "Modifier+Key" strings (e.g. "Cmd+Shift+3")
    [ObservableProperty] private string _captureRegionHotkey = "";
    [ObservableProperty] private string _captureWindowHotkey = "";
    [ObservableProperty] private string _captureFullscreenHotkey = "";
    [ObservableProperty] private string _recordVideoHotkey = "";
    [ObservableProperty] private string _recordGifHotkey = "";
    [ObservableProperty] private string _ocrTextHotkey = "";

    // Workflow — global defaults (non-nullable)
    [ObservableProperty] private bool   _wfShowToolbar;
    [ObservableProperty] private bool   _wfAutoCopyImage;
    [ObservableProperty] private bool   _wfAutoUpload;
    [ObservableProperty] private string _wfSaveFolder = "";  // empty = use AppSettings.SavePath

    // Workflow — Region overrides (null = inherit global)
    [ObservableProperty] private bool?   _wfRegionShowToolbar;
    [ObservableProperty] private bool?   _wfRegionAutoCopyImage;
    [ObservableProperty] private bool?   _wfRegionAutoUpload;
    [ObservableProperty] private string? _wfRegionSaveFolder;

    // Workflow — Window overrides
    [ObservableProperty] private bool?   _wfWindowShowToolbar;
    [ObservableProperty] private bool?   _wfWindowAutoCopyImage;
    [ObservableProperty] private bool?   _wfWindowAutoUpload;
    [ObservableProperty] private string? _wfWindowSaveFolder;

    // Workflow — Fullscreen overrides
    [ObservableProperty] private bool?   _wfFullscreenShowToolbar;
    [ObservableProperty] private bool?   _wfFullscreenAutoCopyImage;
    [ObservableProperty] private bool?   _wfFullscreenAutoUpload;
    [ObservableProperty] private string? _wfFullscreenSaveFolder;

    public event Action? CloseRequested;

    public SettingsViewModel(SettingsService service, LoginItemService? loginItems = null)
    {
        _service    = service;
        _loginItems = loginItems ?? new LoginItemService();
        var s = service.Current;

        SavePath                  = s.SavePath;
        PostCaptureTimeoutSeconds = s.PostCaptureToolbarTimeoutSeconds;
        ImgurClientId             = s.ImgurClientId;
        ActiveImageDestination    = s.ActiveImageDestination;
        LaunchAtLogin             = _loginItems.IsEnabled;

        var h = s.Hotkeys;
        CaptureRegionHotkey     = KeyComboHelper.ToString(h.CaptureRegion);
        CaptureWindowHotkey     = KeyComboHelper.ToString(h.CaptureWindow);
        CaptureFullscreenHotkey = KeyComboHelper.ToString(h.CaptureFullscreen);
        RecordVideoHotkey       = KeyComboHelper.ToString(h.RecordVideo);
        RecordGifHotkey         = KeyComboHelper.ToString(h.RecordGif);
        OcrTextHotkey           = KeyComboHelper.ToString(h.OcrText);

        var wf = s.Workflow;
        WfShowToolbar   = wf.ShowToolbar;
        WfAutoCopyImage = wf.AutoCopyImage;
        WfAutoUpload    = wf.AutoUpload;
        WfSaveFolder    = wf.SaveFolder ?? "";

        WfRegionShowToolbar   = wf.RegionOverride.ShowToolbar;
        WfRegionAutoCopyImage = wf.RegionOverride.AutoCopyImage;
        WfRegionAutoUpload    = wf.RegionOverride.AutoUpload;
        WfRegionSaveFolder    = wf.RegionOverride.SaveFolder;

        WfWindowShowToolbar   = wf.WindowOverride.ShowToolbar;
        WfWindowAutoCopyImage = wf.WindowOverride.AutoCopyImage;
        WfWindowAutoUpload    = wf.WindowOverride.AutoUpload;
        WfWindowSaveFolder    = wf.WindowOverride.SaveFolder;

        WfFullscreenShowToolbar   = wf.FullscreenOverride.ShowToolbar;
        WfFullscreenAutoCopyImage = wf.FullscreenOverride.AutoCopyImage;
        WfFullscreenAutoUpload    = wf.FullscreenOverride.AutoUpload;
        WfFullscreenSaveFolder    = wf.FullscreenOverride.SaveFolder;
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
    private async Task BrowseWfSaveFolder(Avalonia.Controls.Window? owner)
    {
        if (owner is null) return;
        var result = await owner.StorageProvider.OpenFolderPickerAsync(
            new Avalonia.Platform.Storage.FolderPickerOpenOptions { Title = "Select Default Save Folder", AllowMultiple = false });
        if (result.Count > 0)
            WfSaveFolder = result[0].Path.LocalPath;
    }

    [RelayCommand]
    private async Task BrowseWfRegionSaveFolder(Avalonia.Controls.Window? owner)
    {
        if (owner is null) return;
        var result = await owner.StorageProvider.OpenFolderPickerAsync(
            new Avalonia.Platform.Storage.FolderPickerOpenOptions { Title = "Select Region Save Folder", AllowMultiple = false });
        if (result.Count > 0)
            WfRegionSaveFolder = result[0].Path.LocalPath;
    }

    [RelayCommand]
    private async Task BrowseWfWindowSaveFolder(Avalonia.Controls.Window? owner)
    {
        if (owner is null) return;
        var result = await owner.StorageProvider.OpenFolderPickerAsync(
            new Avalonia.Platform.Storage.FolderPickerOpenOptions { Title = "Select Window Save Folder", AllowMultiple = false });
        if (result.Count > 0)
            WfWindowSaveFolder = result[0].Path.LocalPath;
    }

    [RelayCommand]
    private async Task BrowseWfFullscreenSaveFolder(Avalonia.Controls.Window? owner)
    {
        if (owner is null) return;
        var result = await owner.StorageProvider.OpenFolderPickerAsync(
            new Avalonia.Platform.Storage.FolderPickerOpenOptions { Title = "Select Fullscreen Save Folder", AllowMultiple = false });
        if (result.Count > 0)
            WfFullscreenSaveFolder = result[0].Path.LocalPath;
    }

    [RelayCommand] private void ClearCaptureRegionHotkey()     => CaptureRegionHotkey = "";
    [RelayCommand] private void ClearCaptureWindowHotkey()     => CaptureWindowHotkey = "";
    [RelayCommand] private void ClearCaptureFullscreenHotkey() => CaptureFullscreenHotkey = "";
    [RelayCommand] private void ClearRecordVideoHotkey()       => RecordVideoHotkey = "";
    [RelayCommand] private void ClearRecordGifHotkey()         => RecordGifHotkey = "";
    [RelayCommand] private void ClearOcrTextHotkey()           => OcrTextHotkey = "";

    [RelayCommand]
    private void Save()
    {
        _service.Current.SavePath                         = SavePath;
        _service.Current.PostCaptureToolbarTimeoutSeconds = PostCaptureTimeoutSeconds;
        _service.Current.ImgurClientId                    = ImgurClientId;
        _service.Current.ActiveImageDestination           = ActiveImageDestination;

        _service.Current.Hotkeys.CaptureRegion     = KeyComboHelper.Parse(CaptureRegionHotkey);
        _service.Current.Hotkeys.CaptureWindow     = KeyComboHelper.Parse(CaptureWindowHotkey);
        _service.Current.Hotkeys.CaptureFullscreen = KeyComboHelper.Parse(CaptureFullscreenHotkey);
        _service.Current.Hotkeys.RecordVideo       = KeyComboHelper.Parse(RecordVideoHotkey);
        _service.Current.Hotkeys.RecordGif         = KeyComboHelper.Parse(RecordGifHotkey);
        _service.Current.Hotkeys.OcrText           = KeyComboHelper.Parse(OcrTextHotkey);

        _service.Current.Workflow.ShowToolbar   = WfShowToolbar;
        _service.Current.Workflow.AutoCopyImage = WfAutoCopyImage;
        _service.Current.Workflow.AutoUpload    = WfAutoUpload;
        _service.Current.Workflow.SaveFolder    = string.IsNullOrEmpty(WfSaveFolder) ? null : WfSaveFolder;

        _service.Current.Workflow.RegionOverride.ShowToolbar   = WfRegionShowToolbar;
        _service.Current.Workflow.RegionOverride.AutoCopyImage = WfRegionAutoCopyImage;
        _service.Current.Workflow.RegionOverride.AutoUpload    = WfRegionAutoUpload;
        _service.Current.Workflow.RegionOverride.SaveFolder    = string.IsNullOrEmpty(WfRegionSaveFolder) ? null : WfRegionSaveFolder;

        _service.Current.Workflow.WindowOverride.ShowToolbar   = WfWindowShowToolbar;
        _service.Current.Workflow.WindowOverride.AutoCopyImage = WfWindowAutoCopyImage;
        _service.Current.Workflow.WindowOverride.AutoUpload    = WfWindowAutoUpload;
        _service.Current.Workflow.WindowOverride.SaveFolder    = string.IsNullOrEmpty(WfWindowSaveFolder) ? null : WfWindowSaveFolder;

        _service.Current.Workflow.FullscreenOverride.ShowToolbar   = WfFullscreenShowToolbar;
        _service.Current.Workflow.FullscreenOverride.AutoCopyImage = WfFullscreenAutoCopyImage;
        _service.Current.Workflow.FullscreenOverride.AutoUpload    = WfFullscreenAutoUpload;
        _service.Current.Workflow.FullscreenOverride.SaveFolder    = string.IsNullOrEmpty(WfFullscreenSaveFolder) ? null : WfFullscreenSaveFolder;

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

- [ ] **Step 2: Remove old booleans from AppSettings.cs**

`SettingsViewModel` no longer references `AutoCopyImage`, `ShowPostCaptureToolbar`, or `AutoUploadAfterCapture`, so they can safely be removed from `AppSettings`. Replace the entire file with:

```csharp
using ShareX.UploadersLib;

namespace ShareXMac.Models;

public class AppSettings
{
    public string SavePath { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
        "ShareX Mac");
    public int PostCaptureToolbarTimeoutSeconds { get; set; } = 8;

    // Upload settings
    public string ImgurClientId { get; set; } = "";
    public ImageDestination ActiveImageDestination { get; set; } = ImageDestination.Imgur;

    // Hotkeys
    public HotkeySettings Hotkeys { get; set; } = new HotkeySettings();

    // Workflow
    public WorkflowSettings Workflow { get; set; } = new WorkflowSettings();
}
```

Existing JSON settings files that contain the three removed fields will have them silently ignored by Newtonsoft.Json on next load.

- [ ] **Step 3: Replace SettingsWindow.axaml**

Replace the entire content of `ShareXMac/Views/SettingsWindow.axaml` with:

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="using:ShareXMac.ViewModels"
        x:Class="ShareXMac.Views.SettingsWindow"
        x:DataType="vm:SettingsViewModel"
        Width="520" Height="640"
        CanResize="False"
        WindowStartupLocation="CenterScreen"
        Title="ShareX Mac — Settings">
    <DockPanel>
        <Button DockPanel.Dock="Bottom"
                Content="Save" Command="{Binding SaveCommand}"
                HorizontalAlignment="Right"
                Background="#FF0078D4" Foreground="White"
                Padding="16,8" Margin="20,8" />
        <TabControl>

            <!-- ── General ── -->
            <TabItem Header="General">
                <ScrollViewer>
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

                        <TextBlock Text="Post-Capture Toolbar" FontWeight="SemiBold" />
                        <Grid ColumnDefinitions="Auto,*">
                            <TextBlock Grid.Column="0" Text="Timeout (seconds):"
                                       VerticalAlignment="Center" Margin="0,0,8,0" />
                            <NumericUpDown Grid.Column="1" Value="{Binding PostCaptureTimeoutSeconds}"
                                           Minimum="2" Maximum="60" Width="80" HorizontalAlignment="Left" />
                        </Grid>

                        <Separator />

                        <TextBlock Text="Upload" FontWeight="SemiBold" />
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

                        <TextBlock Text="System" FontWeight="SemiBold" />
                        <CheckBox Content="Launch at Login"
                                  IsChecked="{Binding LaunchAtLogin}" />

                    </StackPanel>
                </ScrollViewer>
            </TabItem>

            <!-- ── Hotkeys ── -->
            <TabItem Header="Hotkeys">
                <ScrollViewer>
                    <StackPanel Margin="20" Spacing="14">

                        <TextBlock Text="Hotkeys" FontWeight="SemiBold" />
                        <TextBlock Text="Click a field and press a key combo (e.g. Cmd+Shift+1). Clear to disable."
                                   FontSize="11" Foreground="#FFAAAAAA" TextWrapping="Wrap" />

                        <Grid ColumnDefinitions="130,*,Auto">
                            <TextBlock Grid.Column="0" Text="Capture Region:"
                                       VerticalAlignment="Center" />
                            <TextBox Grid.Column="1" x:Name="CaptureRegionBox"
                                     Text="{Binding CaptureRegionHotkey}"
                                     Watermark="Not set" IsReadOnly="True" Cursor="Hand" />
                            <Button Grid.Column="2" Content="✕"
                                    Command="{Binding ClearCaptureRegionHotkeyCommand}"
                                    Margin="4,0,0,0" Padding="8,4" />
                        </Grid>

                        <Grid ColumnDefinitions="130,*,Auto">
                            <TextBlock Grid.Column="0" Text="Capture Window:"
                                       VerticalAlignment="Center" />
                            <TextBox Grid.Column="1" x:Name="CaptureWindowBox"
                                     Text="{Binding CaptureWindowHotkey}"
                                     Watermark="Not set" IsReadOnly="True" Cursor="Hand" />
                            <Button Grid.Column="2" Content="✕"
                                    Command="{Binding ClearCaptureWindowHotkeyCommand}"
                                    Margin="4,0,0,0" Padding="8,4" />
                        </Grid>

                        <Grid ColumnDefinitions="130,*,Auto">
                            <TextBlock Grid.Column="0" Text="Capture Fullscreen:"
                                       VerticalAlignment="Center" />
                            <TextBox Grid.Column="1" x:Name="CaptureFullscreenBox"
                                     Text="{Binding CaptureFullscreenHotkey}"
                                     Watermark="Not set" IsReadOnly="True" Cursor="Hand" />
                            <Button Grid.Column="2" Content="✕"
                                    Command="{Binding ClearCaptureFullscreenHotkeyCommand}"
                                    Margin="4,0,0,0" Padding="8,4" />
                        </Grid>

                        <Grid ColumnDefinitions="130,*,Auto">
                            <TextBlock Grid.Column="0" Text="Record Video:"
                                       VerticalAlignment="Center" />
                            <TextBox Grid.Column="1" x:Name="RecordVideoBox"
                                     Text="{Binding RecordVideoHotkey}"
                                     Watermark="Not set" IsReadOnly="True" Cursor="Hand" />
                            <Button Grid.Column="2" Content="✕"
                                    Command="{Binding ClearRecordVideoHotkeyCommand}"
                                    Margin="4,0,0,0" Padding="8,4" />
                        </Grid>

                        <Grid ColumnDefinitions="130,*,Auto">
                            <TextBlock Grid.Column="0" Text="Record GIF:"
                                       VerticalAlignment="Center" />
                            <TextBox Grid.Column="1" x:Name="RecordGifBox"
                                     Text="{Binding RecordGifHotkey}"
                                     Watermark="Not set" IsReadOnly="True" Cursor="Hand" />
                            <Button Grid.Column="2" Content="✕"
                                    Command="{Binding ClearRecordGifHotkeyCommand}"
                                    Margin="4,0,0,0" Padding="8,4" />
                        </Grid>

                        <Grid ColumnDefinitions="130,*,Auto">
                            <TextBlock Grid.Column="0" Text="Capture Text (OCR):"
                                       VerticalAlignment="Center" />
                            <TextBox Grid.Column="1" x:Name="OcrTextBox"
                                     Text="{Binding OcrTextHotkey}"
                                     Watermark="Not set" IsReadOnly="True" Cursor="Hand" />
                            <Button Grid.Column="2" Content="✕"
                                    Command="{Binding ClearOcrTextHotkeyCommand}"
                                    Margin="4,0,0,0" Padding="8,4" />
                        </Grid>

                    </StackPanel>
                </ScrollViewer>
            </TabItem>

            <!-- ── Workflow ── -->
            <TabItem Header="Workflow">
                <ScrollViewer>
                    <StackPanel Margin="20" Spacing="10">

                        <TextBlock Text="Configure what happens after each capture. Override defaults per capture type."
                                   FontSize="11" Foreground="#FFAAAAAA" TextWrapping="Wrap" />

                        <!-- Column headers -->
                        <Grid ColumnDefinitions="110,65,65,70,*" Margin="0,4,0,0">
                            <TextBlock Grid.Column="1" Text="Toolbar"
                                       HorizontalAlignment="Center" FontSize="11" Foreground="#FF888888" />
                            <TextBlock Grid.Column="2" Text="Copy"
                                       HorizontalAlignment="Center" FontSize="11" Foreground="#FF888888" />
                            <TextBlock Grid.Column="3" Text="Upload"
                                       HorizontalAlignment="Center" FontSize="11" Foreground="#FF888888" />
                            <TextBlock Grid.Column="4" Text="Save Folder"
                                       FontSize="11" Foreground="#FF888888" />
                        </Grid>

                        <Separator />

                        <!-- Defaults row (non-three-state checkboxes) -->
                        <Grid ColumnDefinitions="110,65,65,70,*">
                            <TextBlock Grid.Column="0" Text="Defaults"
                                       FontWeight="SemiBold" VerticalAlignment="Center" />
                            <CheckBox Grid.Column="1" IsChecked="{Binding WfShowToolbar}"
                                      HorizontalAlignment="Center" />
                            <CheckBox Grid.Column="2" IsChecked="{Binding WfAutoCopyImage}"
                                      HorizontalAlignment="Center" />
                            <CheckBox Grid.Column="3" IsChecked="{Binding WfAutoUpload}"
                                      HorizontalAlignment="Center" />
                            <Grid Grid.Column="4" ColumnDefinitions="*,Auto">
                                <TextBox Grid.Column="0" Text="{Binding WfSaveFolder}"
                                         Watermark="Use default save path" />
                                <Button Grid.Column="1" Content="…"
                                        Command="{Binding BrowseWfSaveFolderCommand}"
                                        CommandParameter="{Binding $parent[Window]}"
                                        Margin="4,0,0,0" Padding="8,4" />
                            </Grid>
                        </Grid>

                        <Separator />

                        <!-- Region override row (three-state checkboxes) -->
                        <Grid ColumnDefinitions="110,65,65,70,*">
                            <TextBlock Grid.Column="0" Text="Region"
                                       VerticalAlignment="Center" />
                            <CheckBox Grid.Column="1" IsChecked="{Binding WfRegionShowToolbar}"
                                      IsThreeState="True" HorizontalAlignment="Center" />
                            <CheckBox Grid.Column="2" IsChecked="{Binding WfRegionAutoCopyImage}"
                                      IsThreeState="True" HorizontalAlignment="Center" />
                            <CheckBox Grid.Column="3" IsChecked="{Binding WfRegionAutoUpload}"
                                      IsThreeState="True" HorizontalAlignment="Center" />
                            <Grid Grid.Column="4" ColumnDefinitions="*,Auto">
                                <TextBox Grid.Column="0" Text="{Binding WfRegionSaveFolder}"
                                         Watermark="Inherit" />
                                <Button Grid.Column="1" Content="…"
                                        Command="{Binding BrowseWfRegionSaveFolderCommand}"
                                        CommandParameter="{Binding $parent[Window]}"
                                        Margin="4,0,0,0" Padding="8,4" />
                            </Grid>
                        </Grid>

                        <!-- Window override row -->
                        <Grid ColumnDefinitions="110,65,65,70,*">
                            <TextBlock Grid.Column="0" Text="Window"
                                       VerticalAlignment="Center" />
                            <CheckBox Grid.Column="1" IsChecked="{Binding WfWindowShowToolbar}"
                                      IsThreeState="True" HorizontalAlignment="Center" />
                            <CheckBox Grid.Column="2" IsChecked="{Binding WfWindowAutoCopyImage}"
                                      IsThreeState="True" HorizontalAlignment="Center" />
                            <CheckBox Grid.Column="3" IsChecked="{Binding WfWindowAutoUpload}"
                                      IsThreeState="True" HorizontalAlignment="Center" />
                            <Grid Grid.Column="4" ColumnDefinitions="*,Auto">
                                <TextBox Grid.Column="0" Text="{Binding WfWindowSaveFolder}"
                                         Watermark="Inherit" />
                                <Button Grid.Column="1" Content="…"
                                        Command="{Binding BrowseWfWindowSaveFolderCommand}"
                                        CommandParameter="{Binding $parent[Window]}"
                                        Margin="4,0,0,0" Padding="8,4" />
                            </Grid>
                        </Grid>

                        <!-- Fullscreen override row -->
                        <Grid ColumnDefinitions="110,65,65,70,*">
                            <TextBlock Grid.Column="0" Text="Fullscreen"
                                       VerticalAlignment="Center" />
                            <CheckBox Grid.Column="1" IsChecked="{Binding WfFullscreenShowToolbar}"
                                      IsThreeState="True" HorizontalAlignment="Center" />
                            <CheckBox Grid.Column="2" IsChecked="{Binding WfFullscreenAutoCopyImage}"
                                      IsThreeState="True" HorizontalAlignment="Center" />
                            <CheckBox Grid.Column="3" IsChecked="{Binding WfFullscreenAutoUpload}"
                                      IsThreeState="True" HorizontalAlignment="Center" />
                            <Grid Grid.Column="4" ColumnDefinitions="*,Auto">
                                <TextBox Grid.Column="0" Text="{Binding WfFullscreenSaveFolder}"
                                         Watermark="Inherit" />
                                <Button Grid.Column="1" Content="…"
                                        Command="{Binding BrowseWfFullscreenSaveFolderCommand}"
                                        CommandParameter="{Binding $parent[Window]}"
                                        Margin="4,0,0,0" Padding="8,4" />
                            </Grid>
                        </Grid>

                        <TextBlock Text="Checked = force ON · Unchecked = force OFF · Indeterminate (−) = inherit default"
                                   FontSize="11" Foreground="#FFAAAAAA" TextWrapping="Wrap"
                                   Margin="0,6,0,0" />

                    </StackPanel>
                </ScrollViewer>
            </TabItem>

        </TabControl>
    </DockPanel>
</Window>
```

- [ ] **Step 4: Build to verify compilation**

```bash
cd /Users/austin/Dev/ShareX-Mac && dotnet build ShareXMac/ShareXMac.csproj 2>&1 | tail -15
```

Expected: Build succeeded, 0 errors. (If Avalonia compiled bindings fail with a missing property error, double-check property names match exactly between the AXAML and SettingsViewModel.)

- [ ] **Step 5: Update SettingsViewModelTests.cs**

Replace the entire content of `ShareXMac.Tests/SettingsViewModelTests.cs` with:

```csharp
using ShareX.HelpersLib;
using ShareXMac.Models;
using ShareXMac.Services;
using ShareXMac.ViewModels;
using Xunit;

namespace ShareXMac.Tests;

public class SettingsViewModelTests
{
    [Fact]
    public void SettingsViewModel_LoadsSavePath()
    {
        string tempFile = Path.Combine(Path.GetTempPath(), $"sharexmac-vm-{Guid.NewGuid():N}.json");
        try
        {
            var svc = new SettingsService(tempFile);
            svc.Current.SavePath = "/tmp/pics";
            var vm = new SettingsViewModel(svc);
            Assert.Equal("/tmp/pics", vm.SavePath);
        }
        finally { if (File.Exists(tempFile)) File.Delete(tempFile); }
    }

    [Fact]
    public void SettingsViewModel_SaveCommand_PersistsSavePath()
    {
        string tempFile = Path.Combine(Path.GetTempPath(), $"sharexmac-vm2-{Guid.NewGuid():N}.json");
        try
        {
            var svc = new SettingsService(tempFile);
            var vm = new SettingsViewModel(svc);
            vm.SavePath = "/tmp/new-path";
            vm.SaveCommand.Execute(null);
            var svc2 = new SettingsService(tempFile);
            Assert.Equal("/tmp/new-path", svc2.Current.SavePath);
        }
        finally { if (File.Exists(tempFile)) File.Delete(tempFile); }
    }
}

public class SettingsViewModelUploadTests
{
    [Fact]
    public void SettingsViewModel_LoadsImgurClientId()
    {
        string tempFile = Path.Combine(Path.GetTempPath(), $"s-up-{Guid.NewGuid():N}.json");
        try
        {
            var svc = new SettingsService(tempFile);
            svc.Current.ImgurClientId = "myclientid";
            var vm = new SettingsViewModel(svc);
            Assert.Equal("myclientid", vm.ImgurClientId);
        }
        finally { if (File.Exists(tempFile)) File.Delete(tempFile); }
    }

    [Fact]
    public void SettingsViewModel_SaveCommand_PersistsImgurClientId()
    {
        string tempFile = Path.Combine(Path.GetTempPath(), $"s-up2-{Guid.NewGuid():N}.json");
        try
        {
            var svc = new SettingsService(tempFile);
            var vm = new SettingsViewModel(svc);
            vm.ImgurClientId = "newid";
            vm.SaveCommand.Execute(null);
            var svc2 = new SettingsService(tempFile);
            Assert.Equal("newid", svc2.Current.ImgurClientId);
        }
        finally { if (File.Exists(tempFile)) File.Delete(tempFile); }
    }
}

public class SettingsViewModelHotkeyTests
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

    [Fact]
    public void SettingsViewModel_LoadsEmptyOcrTextHotkey_WhenNotConfigured()
    {
        var vm = new SettingsViewModel(MakeSvc());
        Assert.Equal("", vm.OcrTextHotkey);
    }

    [Fact]
    public void SettingsViewModel_LoadsOcrTextHotkey_FromSettings()
    {
        var svc = MakeSvc();
        svc.Current.Hotkeys.OcrText = new KeyCombo("Cmd+Shift", "T");
        var vm = new SettingsViewModel(svc);
        Assert.Equal("Cmd+Shift+T", vm.OcrTextHotkey);
    }

    [Fact]
    public void SettingsViewModel_SavesOcrTextHotkey_ToSettings()
    {
        var svc = MakeSvc();
        var vm = new SettingsViewModel(svc);
        vm.OcrTextHotkey = "Cmd+T";
        vm.SaveCommand.Execute(null);
        Assert.Equal(new KeyCombo("Cmd", "T"), svc.Current.Hotkeys.OcrText);
    }

    [Fact]
    public void SettingsViewModel_ClearOcrTextHotkeyCommand_SetsEmpty()
    {
        var svc = MakeSvc();
        svc.Current.Hotkeys.OcrText = new KeyCombo("Cmd", "T");
        var vm = new SettingsViewModel(svc);
        vm.ClearOcrTextHotkeyCommand.Execute(null);
        Assert.Equal("", vm.OcrTextHotkey);
    }
}

public class SettingsViewModelWorkflowTests
{
    private static SettingsService MakeSvc()
        => new SettingsService(Path.Combine(Path.GetTempPath(), $"s-wf-{Guid.NewGuid():N}.json"));

    [Fact]
    public void SettingsViewModel_LoadsWorkflowGlobalDefaults()
    {
        var svc = MakeSvc();
        svc.Current.Workflow.ShowToolbar   = false;
        svc.Current.Workflow.AutoCopyImage = false;
        svc.Current.Workflow.AutoUpload    = true;

        var vm = new SettingsViewModel(svc);
        Assert.False(vm.WfShowToolbar);
        Assert.False(vm.WfAutoCopyImage);
        Assert.True(vm.WfAutoUpload);
    }

    [Fact]
    public void SettingsViewModel_LoadsWorkflowOverride_NullWhenNotSet()
    {
        var vm = new SettingsViewModel(MakeSvc());
        Assert.Null(vm.WfRegionShowToolbar);
        Assert.Null(vm.WfWindowAutoCopyImage);
        Assert.Null(vm.WfFullscreenAutoUpload);
    }

    [Fact]
    public void SettingsViewModel_LoadsWorkflowOverride_WhenSet()
    {
        var svc = MakeSvc();
        svc.Current.Workflow.FullscreenOverride.AutoUpload    = true;
        svc.Current.Workflow.FullscreenOverride.ShowToolbar   = false;

        var vm = new SettingsViewModel(svc);
        Assert.True(vm.WfFullscreenAutoUpload);
        Assert.False(vm.WfFullscreenShowToolbar);
    }

    [Fact]
    public void SettingsViewModel_Save_PersistsGlobalDefaults()
    {
        var svc = MakeSvc();
        var vm = new SettingsViewModel(svc);
        vm.WfShowToolbar   = false;
        vm.WfAutoUpload    = true;
        vm.WfSaveFolder    = "/custom";
        vm.SaveCommand.Execute(null);

        Assert.False(svc.Current.Workflow.ShowToolbar);
        Assert.True(svc.Current.Workflow.AutoUpload);
        Assert.Equal("/custom", svc.Current.Workflow.SaveFolder);
    }

    [Fact]
    public void SettingsViewModel_Save_PersistsPerTypeOverride()
    {
        var svc = MakeSvc();
        var vm = new SettingsViewModel(svc);
        vm.WfRegionAutoUpload   = true;
        vm.WfRegionSaveFolder   = "/region-folder";
        vm.SaveCommand.Execute(null);

        Assert.True(svc.Current.Workflow.RegionOverride.AutoUpload);
        Assert.Equal("/region-folder", svc.Current.Workflow.RegionOverride.SaveFolder);
    }

    [Fact]
    public void SettingsViewModel_Save_NullForEmptyOverride()
    {
        var svc = MakeSvc();
        svc.Current.Workflow.RegionOverride.ShowToolbar = true;
        var vm = new SettingsViewModel(svc);
        vm.WfRegionShowToolbar = null;
        vm.WfRegionSaveFolder  = "";
        vm.SaveCommand.Execute(null);

        Assert.Null(svc.Current.Workflow.RegionOverride.ShowToolbar);
        Assert.Null(svc.Current.Workflow.RegionOverride.SaveFolder);
    }

    [Fact]
    public void SettingsViewModel_Save_NullWfSaveFolder_WhenEmpty()
    {
        var svc = MakeSvc();
        svc.Current.Workflow.SaveFolder = "/old";
        var vm = new SettingsViewModel(svc);
        vm.WfSaveFolder = "";
        vm.SaveCommand.Execute(null);
        Assert.Null(svc.Current.Workflow.SaveFolder);
    }
}
```

- [ ] **Step 6: Run tests**

```bash
cd /Users/austin/Dev/ShareX-Mac && dotnet test ShareXMac.Tests 2>&1 | tail -15
```

Expected: all tests pass. New total should be prior count + 14 (8 WorkflowSettingsTests + 6 SettingsViewModelWorkflowTests).

- [ ] **Step 7: Commit**

```bash
cd /Users/austin/Dev/ShareX-Mac
git add ShareXMac/Models/AppSettings.cs \
        ShareXMac/ViewModels/SettingsViewModel.cs \
        ShareXMac/Views/SettingsWindow.axaml \
        ShareXMac.Tests/SettingsViewModelTests.cs
git commit -m "feat: migrate SettingsViewModel and SettingsWindow to workflow settings"
```
