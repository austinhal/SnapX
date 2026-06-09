# After-Capture Workflow — Design Spec

**Date:** 2026-06-08
**Status:** Approved

---

## Goal

Replace the three scattered global booleans (`ShowPostCaptureToolbar`, `AutoCopyImage`, `AutoUploadAfterCapture`) with a structured `WorkflowSettings` model that supports global defaults plus per-capture-type overrides for four actions: show toolbar, auto-copy image, auto-upload, and save to a custom folder.

---

## Current State

`AppSettings` has three flat booleans that apply uniformly to all capture types. `TrayViewModel.OnCaptureComplete` reads them directly with no awareness of which capture type triggered it.

---

## New Components

### `CaptureType.cs` (`ShareXMac/Models/CaptureType.cs`)

```csharp
namespace ShareXMac.Models;

public enum CaptureType { Region, Window, Fullscreen }

public record ResolvedWorkflow(
    bool ShowToolbar,
    bool AutoCopyImage,
    bool AutoUpload,
    string? SaveFolder);
```

`ResolvedWorkflow` is the output of `WorkflowSettings.Resolve()` — all values are non-nullable so callers don't need to null-check.

### `CaptureWorkflow.cs` (`ShareXMac/Models/CaptureWorkflow.cs`)

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

All properties nullable: `null` means "inherit from global default."

### `WorkflowSettings.cs` (`ShareXMac/Models/WorkflowSettings.cs`)

```csharp
namespace ShareXMac.Models;

public class WorkflowSettings
{
    // Global defaults
    public bool ShowToolbar   { get; set; } = true;
    public bool AutoCopyImage { get; set; } = true;
    public bool AutoUpload    { get; set; } = false;
    public string? SaveFolder { get; set; }  // null = use AppSettings.SavePath

    // Per-type overrides (null properties = inherit global)
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

---

## Modified Components

### `AppSettings.cs`

- **Remove:** `AutoCopyImage`, `ShowPostCaptureToolbar`, `AutoUploadAfterCapture`
- **Keep:** `SavePath`, `PostCaptureToolbarTimeoutSeconds`, `ImgurClientId`, `ActiveImageDestination`, `Hotkeys`
- **Add:** `public WorkflowSettings Workflow { get; set; } = new WorkflowSettings();`

Old settings JSON files will have the three removed booleans ignored by Newtonsoft.Json (unknown properties are silently skipped). Users re-configure on first run — acceptable for a personal tool at this stage.

### `TrayViewModel.cs`

`OnCaptureComplete` gains a `CaptureType` parameter:

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

The three capture commands pass their type:
```csharp
private async Task CaptureRegion()
{
    byte[]? data = await _capture.CaptureRegionAsync();
    if (data != null) await OnCaptureComplete(data, CaptureType.Region);
}
// Same for CaptureWindow → CaptureType.Window
// Same for CaptureFullscreen → CaptureType.Fullscreen
```

### `SettingsViewModel.cs`

Remove: `AutoCopyImage`, `ShowPostCaptureToolbar`, `AutoUploadAfterCapture` properties and their load/save lines.

Add 16 workflow properties — global defaults (non-nullable) and per-type overrides (nullable for three-state binding):

```csharp
// Global defaults
[ObservableProperty] private bool   _wfShowToolbar;
[ObservableProperty] private bool   _wfAutoCopyImage;
[ObservableProperty] private bool   _wfAutoUpload;
[ObservableProperty] private string _wfSaveFolder = "";  // empty string = use SavePath

// Region overrides (null = inherit)
[ObservableProperty] private bool?   _wfRegionShowToolbar;
[ObservableProperty] private bool?   _wfRegionAutoCopyImage;
[ObservableProperty] private bool?   _wfRegionAutoUpload;
[ObservableProperty] private string? _wfRegionSaveFolder;

// Window overrides
[ObservableProperty] private bool?   _wfWindowShowToolbar;
[ObservableProperty] private bool?   _wfWindowAutoCopyImage;
[ObservableProperty] private bool?   _wfWindowAutoUpload;
[ObservableProperty] private string? _wfWindowSaveFolder;

// Fullscreen overrides
[ObservableProperty] private bool?   _wfFullscreenShowToolbar;
[ObservableProperty] private bool?   _wfFullscreenAutoCopyImage;
[ObservableProperty] private bool?   _wfFullscreenAutoUpload;
[ObservableProperty] private string? _wfFullscreenSaveFolder;
```

Load in constructor from `s.Workflow.*`, save back in `Save()` to `_service.Current.Workflow.*`.

`WfSaveFolder` uses empty string = null semantics: loaded as `h.SaveFolder ?? ""`, saved as `string.IsNullOrEmpty(WfSaveFolder) ? null : WfSaveFolder`.

### `SettingsWindow.axaml`

Restructure from a flat `ScrollViewer` into a `TabControl` with three `TabItem`s:

**General tab:** Save Location, toolbar timeout, Upload (Imgur Client ID only — auto-upload moves to Workflow), System (Launch at Login), Save button.

**Hotkeys tab:** All existing hotkey rows (unchanged).

**Workflow tab:** Grid table with header row + Defaults row + Region/Window/Fullscreen override rows. Columns: capture type label, Toolbar, Auto-Copy, Auto-Upload, Save Folder.

- **Defaults row:** four normal `CheckBox`es (non-three-state) + a `TextBox`/`Button` for folder
- **Override rows:** four `CheckBox IsThreeState="True"` + a `TextBox`/`Button` for folder
- Three-state checkbox states: indeterminate (−) = inherit, checked (✓) = force ON, unchecked (□) = force OFF
- A small helper text below the grid: "Checked = force ON · Unchecked = force OFF · Indeterminate (−) = inherit default"

The Save button moves into each tab's bottom area (or stays in a shared footer below the `TabControl`).

`SettingsWindow.axaml.cs` needs no structural changes — `FindControl` still works across tab panes.

---

## Settings Migration

Existing `SettingsViewModelTests` tests that reference `AutoCopyImage`, `ShowPostCaptureToolbar`, `AutoUploadAfterCapture` are **deleted** and replaced with new workflow tests.

---

## Testing

### `WorkflowSettingsTests.cs` (new)

Key tests for `Resolve()`:
- All four actions inherit global defaults when no override is set (all null)
- `ShowToolbar` override `true` wins over global `false`
- `ShowToolbar` override `false` wins over global `true`
- `null` override falls through to global value
- `SaveFolder` override on a specific type overrides global folder
- `SaveFolder = null` on a type falls back to global `SaveFolder`
- `SaveFolder = null` globally returns `null` in `ResolvedWorkflow`

### `SettingsViewModelTests.cs` (updated)

Remove tests for `AutoCopyImage`, `ShowPostCaptureToolbar`, `AutoUploadAfterCapture`. Add:
- Workflow global defaults load from `settings.Workflow.*`
- Workflow override loads (null when not set)
- `Save()` persists global defaults correctly
- `Save()` persists a per-type override correctly
- `Save()` saves `null` for an unset override

---

## File Summary

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
