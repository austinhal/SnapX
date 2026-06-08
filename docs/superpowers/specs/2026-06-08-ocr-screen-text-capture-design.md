# OCR Screen Text Capture — Design Spec

**Date:** 2026-06-08
**Status:** Approved

---

## Goal

Hotkey/menu-triggered region selection that runs Apple Vision OCR on the captured pixels and shows the extracted text in an editable popup, allowing the user to clean up and copy it.

---

## What Already Exists

- `MacVision.RecognizeTextAsync(byte[] imageData) → Task<string?>` — fully implemented, zero deps beyond the Vision framework
- `IScreenCapture.RecognizeTextAsync(byte[] imageData)` — interface method, implemented by `MacScreenCapture`
- `IScreenCapture.CaptureRegionAsync()` — region selection already works
- `IHotkeyManager` + `HotkeySettings` + `KeyComboHelper` — hotkey infrastructure
- `SettingsViewModel` hotkey pattern — each hotkey has a string property + Clear command, serialised via `KeyComboHelper`
- `TrayViewModel` — accepts `IScreenCapture`, registers hotkeys from `HotkeySettings`

---

## New Components

### `OcrService` (`ShareXMac/Services/OcrService.cs`)

Single method: `CaptureAndRecognizeAsync() → Task<string?>`.

1. Call `_capture.CaptureRegionAsync()` — returns `null` if user cancels
2. If `null`, return `null` (no-op)
3. Call `_capture.RecognizeTextAsync(bytes)` — returns `null` or empty string if no text found
4. Return the recognized text string (caller handles null/empty)

Constructor takes `IScreenCapture`. Virtual method for testability (same pattern as `UploadService`).

### `OcrResultViewModel` (`ShareXMac/ViewModels/OcrResultViewModel.cs`)

Properties:
- `[ObservableProperty] string RecognizedText` — initialized with extracted text, user-editable
- `int AutoDismissSeconds = 30` — longer than PostCapture's 8s to allow reading/editing
- `event Action? CloseRequested`

Commands:
- `CopyCommand` — `MacClipboard.SetText(RecognizedText)`, then `CloseRequested?.Invoke()`
- `DismissCommand` — `CloseRequested?.Invoke()`

### `OcrResultWindow` (`ShareXMac/Views/OcrResultWindow.axaml`)

Compact popup (~400×240px), same dark styling as `PostCaptureWindow`.

Layout:
```
┌─────────────────────────────────┐
│  Recognized Text                │  ← header label
│  ┌───────────────────────────┐  │
│  │  (editable TextBox,        │  │
│  │   multiline, pre-selected) │  │
│  └───────────────────────────┘  │
│  [Copy & Close]  [Dismiss]      │  ← bottom button row
└─────────────────────────────────┘
```

Code-behind (`OcrResultWindow.axaml.cs`):
- Constructor takes `OcrResultViewModel`, wires `vm.CloseRequested += Close`
- Auto-dismiss timer (`DispatcherTimer`) using `vm.AutoDismissSeconds`
- On open: select all text in the TextBox (so user can immediately type to replace or Cmd+C to copy)

---

## Modified Components

### `HotkeySettings` (`ShareXMac/Models/HotkeySettings.cs`)

Add one property:
```csharp
public KeyCombo? OcrText { get; set; }
```

### `TrayViewModel` (`ShareXMac/ViewModels/TrayViewModel.cs`)

- Add `OcrService _ocr` field
- Add `OcrService` parameter to constructor
- Add hotkey registration: `RegisterHotkey("ocr-text", h.OcrText, CaptureTextOcr)`
- Add `[RelayCommand] private async Task CaptureTextOcr()`:
  ```csharp
  string? text = await _ocr.CaptureAndRecognizeAsync();
  if (string.IsNullOrWhiteSpace(text)) return;
  await Dispatcher.UIThread.InvokeAsync(() =>
  {
      var vm = new OcrResultViewModel(text);
      new OcrResultWindow(vm).Show();
  });
  ```

### `SettingsViewModel` (`ShareXMac/ViewModels/SettingsViewModel.cs`)

Following existing hotkey pattern:
- Add `[ObservableProperty] private string _ocrTextHotkey = ""`
- Load in constructor: `OcrTextHotkey = KeyComboHelper.ToString(h.OcrText)`
- Add `[RelayCommand] private void ClearOcrTextHotkey() => OcrTextHotkey = ""`
- Save in `Save()`: `_service.Current.Hotkeys.OcrText = KeyComboHelper.Parse(OcrTextHotkey)`

### `SettingsWindow` (`ShareXMac/Views/SettingsWindow.axaml`)

Add a new row in the Hotkeys tab for "Capture Text (OCR)" — identical AXAML structure to the existing `CaptureRegion` row:
```xml
<TextBlock Text="Capture Text (OCR)" />
<TextBox Text="{Binding OcrTextHotkey}" ... />
<Button Command="{Binding ClearOcrTextHotkeyCommand}" ... />
```

### `App.axaml`

Add tray menu item between color picker and settings:
```xml
<NativeMenuItem Header="Capture Text (OCR)" Command="{Binding CaptureTextOcrCommand}" />
```

### `App.axaml.cs`

Inject `OcrService` into `TrayViewModel` constructor:
```csharp
var ocrService = new OcrService(capture);
var tray = new TrayViewModel(capture, settingsService, historyService, uploadService, hotkeyManager, ocrService);
```

---

## Testing

### Unit tests (`ShareXMac.Tests/OcrServiceTests.cs`)

1. **Returns null when capture is cancelled** — stub returns `null` from `CaptureRegionAsync`, assert `CaptureAndRecognizeAsync` returns `null`
2. **Returns null when recognition returns null** — stub returns image bytes, recognition returns `null`, assert result is `null`
3. **Returns text when recognition succeeds** — stub returns image bytes + text, assert result matches

### Unit tests (`ShareXMac.Tests/OcrResultViewModelTests.cs`)

1. **RecognizedText initialized from constructor** — assert `vm.RecognizedText == inputText`
2. **CopyCommand fires CloseRequested** — subscribe to event, invoke command, assert fired
3. **DismissCommand fires CloseRequested** — same pattern

No Avalonia headless setup needed — `OcrResultViewModel` has no Avalonia dependencies.

---

## Out of Scope

- Language selection (Vision uses OS locale by default)
- OCR history / saving recognized text to file
- Clipboard auto-copy without popup (user chose editable popup)
- Confidence threshold filtering

---

## File Summary

| Action | Path |
|--------|------|
| Create | `ShareXMac/Services/OcrService.cs` |
| Create | `ShareXMac/ViewModels/OcrResultViewModel.cs` |
| Create | `ShareXMac/Views/OcrResultWindow.axaml` |
| Create | `ShareXMac/Views/OcrResultWindow.axaml.cs` |
| Create | `ShareXMac.Tests/OcrServiceTests.cs` |
| Create | `ShareXMac.Tests/OcrResultViewModelTests.cs` |
| Modify | `ShareXMac/Models/HotkeySettings.cs` |
| Modify | `ShareXMac/ViewModels/TrayViewModel.cs` |
| Modify | `ShareXMac/ViewModels/SettingsViewModel.cs` |
| Modify | `ShareXMac/Views/SettingsWindow.axaml` |
| Modify | `ShareXMac/App.axaml` |
| Modify | `ShareXMac/App.axaml.cs` |
