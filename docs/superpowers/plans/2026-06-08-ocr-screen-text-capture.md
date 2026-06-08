# OCR Screen Text Capture — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add hotkey/tray-menu-triggered region OCR that shows extracted text in an editable popup, allowing copy before dismissal.

**Architecture:** `OcrService` wraps `IScreenCapture.CaptureRegionAsync` + `RecognizeTextAsync` into one call. `TrayViewModel` instantiates it internally and opens `OcrResultWindow` with the result. `HotkeySettings` gains an `OcrText` entry wired through `SettingsViewModel` and `SettingsWindow` using the exact same pattern as the five existing hotkeys.

**Tech Stack:** .NET 10, Avalonia 11.2.1, CommunityToolkit.Mvvm 8.4.0, Apple Vision via existing `MacVision.RecognizeTextAsync`

---

## File Map

| Action | Path | Responsibility |
|--------|------|----------------|
| Create | `ShareXMac/Services/OcrService.cs` | Capture region → OCR → return text |
| Create | `ShareXMac/ViewModels/OcrResultViewModel.cs` | Holds text, Copy/Dismiss commands, auto-dismiss timer value |
| Create | `ShareXMac/Views/OcrResultWindow.axaml` | Editable text popup |
| Create | `ShareXMac/Views/OcrResultWindow.axaml.cs` | Window code-behind, auto-dismiss wiring |
| Create | `ShareXMac.Tests/OcrServiceTests.cs` | OcrService unit tests |
| Create | `ShareXMac.Tests/OcrResultViewModelTests.cs` | OcrResultViewModel unit tests |
| Modify | `ShareXMac/Models/HotkeySettings.cs` | Add `OcrText` property |
| Modify | `ShareXMac/ViewModels/TrayViewModel.cs` | Add `_ocr` field, `CaptureTextOcr` command, hotkey registration |
| Modify | `ShareXMac/App.axaml` | Add "Capture Text (OCR)" tray menu item |
| Modify | `ShareXMac/ViewModels/SettingsViewModel.cs` | Add `OcrTextHotkey` property, clear command, save/load |
| Modify | `ShareXMac/Views/SettingsWindow.axaml` | Add OCR hotkey row |
| Modify | `ShareXMac/Views/SettingsWindow.axaml.cs` | Wire OCR hotkey TextBox key capture |

---

## Task 1: OcrService

**Files:**
- Create: `ShareXMac/Services/OcrService.cs`
- Create: `ShareXMac.Tests/OcrServiceTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `ShareXMac.Tests/OcrServiceTests.cs`:

```csharp
using ShareX.HelpersLib;
using ShareXMac.Services;
using Xunit;

namespace ShareXMac.Tests;

public class OcrServiceTests
{
    [Fact]
    public async Task CaptureAndRecognize_WhenCaptureReturnsNull_ReturnsNull()
    {
        var stub = new StubOcrCapture(regionResult: null, textResult: null);
        var svc = new OcrService(stub);
        string? result = await svc.CaptureAndRecognizeAsync();
        Assert.Null(result);
    }

    [Fact]
    public async Task CaptureAndRecognize_WhenRecognitionReturnsNull_ReturnsNull()
    {
        var stub = new StubOcrCapture(regionResult: new byte[] { 1, 2, 3 }, textResult: null);
        var svc = new OcrService(stub);
        string? result = await svc.CaptureAndRecognizeAsync();
        Assert.Null(result);
    }

    [Fact]
    public async Task CaptureAndRecognize_WhenRecognitionSucceeds_ReturnsText()
    {
        var stub = new StubOcrCapture(regionResult: new byte[] { 1, 2, 3 }, textResult: "Hello World");
        var svc = new OcrService(stub);
        string? result = await svc.CaptureAndRecognizeAsync();
        Assert.Equal("Hello World", result);
    }

    [Fact]
    public async Task CaptureAndRecognize_DoesNotCallRecognize_WhenCaptureCancelled()
    {
        var stub = new StubOcrCapture(regionResult: null, textResult: "should not be returned");
        var svc = new OcrService(stub);
        await svc.CaptureAndRecognizeAsync();
        Assert.False(stub.RecognizeCalled);
    }
}

internal class StubOcrCapture : IScreenCapture
{
    private readonly byte[]? _regionResult;
    private readonly string? _textResult;
    public bool RecognizeCalled { get; private set; }

    public StubOcrCapture(byte[]? regionResult, string? textResult)
    {
        _regionResult = regionResult;
        _textResult   = textResult;
    }

    public Task<byte[]?> CaptureRegionAsync()     => Task.FromResult(_regionResult);
    public Task<byte[]?> CaptureWindowAsync()     => Task.FromResult<byte[]?>(null);
    public Task<byte[]?> CaptureFullscreenAsync() => Task.FromResult<byte[]?>(null);
    public Task StartRecordingAsync(string outputPath, RecordingFormat format) => Task.CompletedTask;
    public Task StopRecordingAsync()              => Task.CompletedTask;
    public Task<string?> RecognizeTextAsync(byte[] imageData)
    {
        RecognizeCalled = true;
        return Task.FromResult(_textResult);
    }
}
```

- [ ] **Step 2: Run tests to confirm they fail**

```bash
cd /Users/austin/Dev/ShareX-Mac
dotnet test ShareXMac.Tests --filter "OcrServiceTests" -v minimal 2>&1 | tail -20
```

Expected: build error — `OcrService` type not found.

- [ ] **Step 3: Implement OcrService**

Create `ShareXMac/Services/OcrService.cs`:

```csharp
using ShareX.HelpersLib;

namespace ShareXMac.Services;

public class OcrService
{
    private readonly IScreenCapture _capture;

    public OcrService(IScreenCapture capture) => _capture = capture;

    public virtual async Task<string?> CaptureAndRecognizeAsync()
    {
        byte[]? image = await _capture.CaptureRegionAsync();
        if (image == null) return null;
        return await _capture.RecognizeTextAsync(image);
    }
}
```

- [ ] **Step 4: Run tests to confirm they pass**

```bash
dotnet test ShareXMac.Tests --filter "OcrServiceTests" -v minimal 2>&1 | tail -10
```

Expected: `4 passed, 0 failed`

- [ ] **Step 5: Commit**

```bash
git add ShareXMac/Services/OcrService.cs ShareXMac.Tests/OcrServiceTests.cs
git commit -m "feat: add OcrService wrapping CaptureRegion + RecognizeText"
```

---

## Task 2: OcrResultViewModel

**Files:**
- Create: `ShareXMac/ViewModels/OcrResultViewModel.cs`
- Create: `ShareXMac.Tests/OcrResultViewModelTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `ShareXMac.Tests/OcrResultViewModelTests.cs`:

```csharp
using ShareXMac.ViewModels;
using Xunit;

namespace ShareXMac.Tests;

[Collection(nameof(HeadlessAvaloniaFixture))]
public class OcrResultViewModelTests
{
    [Fact]
    public void Constructor_SetsRecognizedText()
    {
        var vm = new OcrResultViewModel("Hello World");
        Assert.Equal("Hello World", vm.RecognizedText);
    }

    [Fact]
    public void AutoDismissSeconds_DefaultIs30()
    {
        var vm = new OcrResultViewModel("text");
        Assert.Equal(30, vm.AutoDismissSeconds);
    }

    [Fact]
    public void DismissCommand_RaisesCloseRequested()
    {
        var vm = new OcrResultViewModel("text");
        bool raised = false;
        vm.CloseRequested += () => raised = true;
        vm.DismissCommand.Execute(null);
        Assert.True(raised);
    }

    [Fact]
    public void CopyCommand_RaisesCloseRequested()
    {
        var vm = new OcrResultViewModel("text");
        bool raised = false;
        vm.CloseRequested += () => raised = true;
        vm.CopyCommand.Execute(null);
        Assert.True(raised);
    }

    [Fact]
    public void CopyCommand_DoesNotThrow_WhenTextIsEmpty()
    {
        var vm = new OcrResultViewModel("");
        vm.CopyCommand.Execute(null);
    }
}
```

- [ ] **Step 2: Run tests to confirm they fail**

```bash
dotnet test ShareXMac.Tests --filter "OcrResultViewModelTests" -v minimal 2>&1 | tail -10
```

Expected: build error — `OcrResultViewModel` type not found.

- [ ] **Step 3: Implement OcrResultViewModel**

Create `ShareXMac/ViewModels/OcrResultViewModel.cs`:

```csharp
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
```

- [ ] **Step 4: Run tests to confirm they pass**

```bash
dotnet test ShareXMac.Tests --filter "OcrResultViewModelTests" -v minimal 2>&1 | tail -10
```

Expected: `5 passed, 0 failed`

- [ ] **Step 5: Commit**

```bash
git add ShareXMac/ViewModels/OcrResultViewModel.cs ShareXMac.Tests/OcrResultViewModelTests.cs
git commit -m "feat: add OcrResultViewModel with Copy and Dismiss commands"
```

---

## Task 3: OcrResultWindow

**Files:**
- Create: `ShareXMac/Views/OcrResultWindow.axaml`
- Create: `ShareXMac/Views/OcrResultWindow.axaml.cs`

No unit tests for this task — pure UI wiring tested by running the app.

- [ ] **Step 1: Create the AXAML**

Create `ShareXMac/Views/OcrResultWindow.axaml`:

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="using:ShareXMac.ViewModels"
        x:Class="ShareXMac.Views.OcrResultWindow"
        x:DataType="vm:OcrResultViewModel"
        Width="400" Height="240"
        CanResize="False"
        SystemDecorations="BorderOnly"
        WindowStartupLocation="CenterScreen"
        Title="ShareX Mac — OCR Result"
        Background="#FF1E1E1E">
    <StackPanel Margin="12" Spacing="8">

        <TextBlock Text="Recognized Text"
                   FontWeight="SemiBold" Foreground="#FFDDDDDD" />

        <TextBox x:Name="TextResult"
                 Text="{Binding RecognizedText}"
                 AcceptsReturn="True"
                 TextWrapping="Wrap"
                 Height="140"
                 Background="#FF2D2D2D"
                 Foreground="#FFDDDDDD"
                 BorderThickness="1"
                 BorderBrush="#FF444444" />

        <StackPanel Orientation="Horizontal"
                    HorizontalAlignment="Right" Spacing="6">
            <Button Content="Copy &amp; Close"
                    Command="{Binding CopyCommand}"
                    Background="#FF0078D4" Foreground="White"
                    Padding="12,5" />
            <Button Content="Dismiss"
                    Command="{Binding DismissCommand}"
                    Padding="12,5" />
        </StackPanel>

    </StackPanel>
</Window>
```

- [ ] **Step 2: Create the code-behind**

Create `ShareXMac/Views/OcrResultWindow.axaml.cs`:

```csharp
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
```

- [ ] **Step 3: Verify project builds**

```bash
dotnet build ShareXMac/ShareXMac.csproj -v minimal 2>&1 | tail -10
```

Expected: `Build succeeded. 0 Warning(s). 0 Error(s)`

- [ ] **Step 4: Run full test suite**

```bash
dotnet test ShareXMac.Tests -v minimal 2>&1 | tail -10
```

Expected: all tests pass.

- [ ] **Step 5: Commit**

```bash
git add ShareXMac/Views/OcrResultWindow.axaml ShareXMac/Views/OcrResultWindow.axaml.cs
git commit -m "feat: add OcrResultWindow with editable text, auto-dismiss, and copy action"
```

---

## Task 4: HotkeySettings + TrayViewModel + App.axaml

**Files:**
- Modify: `ShareXMac/Models/HotkeySettings.cs`
- Modify: `ShareXMac/ViewModels/TrayViewModel.cs`
- Modify: `ShareXMac/App.axaml`

- [ ] **Step 1: Write failing test for OCR command existence**

Add to `ShareXMac.Tests/TrayViewModelTests.cs` (append after the existing `TrayViewModel_WithServices_DoesNotThrow` test, before the closing `}`):

```csharp
[Fact]
public void TrayViewModel_CaptureTextOcrCommand_IsNotNull()
{
    var vm = new TrayViewModel(
        new StubScreenCapture(),
        new SettingsService(Path.GetTempFileName()),
        new HistoryService(Path.GetTempFileName()),
        new UploadService(),
        new StubHotkeyManager());
    Assert.NotNull(vm.CaptureTextOcrCommand);
}
```

- [ ] **Step 2: Run test to confirm it fails**

```bash
dotnet test ShareXMac.Tests --filter "TrayViewModel_CaptureTextOcrCommand_IsNotNull" -v minimal 2>&1 | tail -10
```

Expected: build error — `CaptureTextOcrCommand` not found.

- [ ] **Step 3: Add OcrText to HotkeySettings**

File: `ShareXMac/Models/HotkeySettings.cs`

Current full content:
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

Replace with:
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
    public KeyCombo? OcrText { get; set; }
}
```

- [ ] **Step 4: Update TrayViewModel**

File: `ShareXMac/ViewModels/TrayViewModel.cs`

Add field after `private readonly IHotkeyManager _hotkeyManager;` (line 21):
```csharp
private readonly OcrService _ocr;
```

In the constructor, after `_hotkeyManager = hotkeyManager;` (line 39), add:
```csharp
_ocr = new OcrService(_capture);
```

In `RegisterHotkeys()`, after the `RegisterHotkey("record-gif", ...)` line (line 58), add:
```csharp
RegisterHotkey("ocr-text", h.OcrText, CaptureTextOcr);
```

Add the new command method after the `RecordGif` method (after line 123):
```csharp
[RelayCommand]
private async Task CaptureTextOcr()
{
    string? text = await _ocr.CaptureAndRecognizeAsync();
    if (string.IsNullOrWhiteSpace(text)) return;
    await Dispatcher.UIThread.InvokeAsync(() =>
    {
        var vm = new OcrResultViewModel(text);
        new OcrResultWindow(vm).Show();
    });
}
```

Add `using ShareXMac.Services;` at the top if not already present (it already is — `UploadService` lives there).

- [ ] **Step 5: Add tray menu item in App.axaml**

File: `ShareXMac/App.axaml`

The current menu has `Color Picker` then a separator then `Settings...`. Add the OCR item after Color Picker (between Color Picker and the separator before Settings):

Replace:
```xml
                        <NativeMenuItem Header="Color Picker"
                                        Command="{Binding OpenColorPickerCommand}" />
                        <NativeMenuItemSeparator />
                        <NativeMenuItem Header="Settings..."
```

With:
```xml
                        <NativeMenuItem Header="Color Picker"
                                        Command="{Binding OpenColorPickerCommand}" />
                        <NativeMenuItem Header="Capture Text (OCR)"
                                        Command="{Binding CaptureTextOcrCommand}" />
                        <NativeMenuItemSeparator />
                        <NativeMenuItem Header="Settings..."
```

- [ ] **Step 6: Run test to confirm it passes**

```bash
dotnet test ShareXMac.Tests --filter "TrayViewModel_CaptureTextOcrCommand_IsNotNull" -v minimal 2>&1 | tail -10
```

Expected: `1 passed, 0 failed`

- [ ] **Step 7: Run full test suite**

```bash
dotnet test ShareXMac.Tests -v minimal 2>&1 | tail -10
```

Expected: all tests pass.

- [ ] **Step 8: Commit**

```bash
git add ShareXMac/Models/HotkeySettings.cs ShareXMac/ViewModels/TrayViewModel.cs ShareXMac/App.axaml ShareXMac.Tests/TrayViewModelTests.cs
git commit -m "feat: wire OCR capture command to tray menu and hotkey"
```

---

## Task 5: SettingsViewModel + SettingsWindow + App.axaml.cs

**Files:**
- Modify: `ShareXMac/ViewModels/SettingsViewModel.cs`
- Modify: `ShareXMac/Views/SettingsWindow.axaml`
- Modify: `ShareXMac/Views/SettingsWindow.axaml.cs`

- [ ] **Step 1: Write failing test for OCR hotkey in SettingsViewModel**

Add to `ShareXMac.Tests/SettingsViewModelTests.cs`, inside the existing `SettingsViewModelHotkeyTests` class (append after the last test, before the final `}`):

```csharp
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
```

- [ ] **Step 2: Run tests to confirm they fail**

```bash
dotnet test ShareXMac.Tests --filter "OcrText" -v minimal 2>&1 | tail -10
```

Expected: build error — `OcrTextHotkey` not found.

- [ ] **Step 3: Update SettingsViewModel**

File: `ShareXMac/ViewModels/SettingsViewModel.cs`

Add after `[ObservableProperty] private string _recordGifHotkey = "";` (line 28):
```csharp
[ObservableProperty] private string _ocrTextHotkey = "";
```

In the constructor, after `RecordGifHotkey = KeyComboHelper.ToString(h.RecordGif);` (line 51), add:
```csharp
OcrTextHotkey = KeyComboHelper.ToString(h.OcrText);
```

After `[RelayCommand] private void ClearRecordGifHotkey() => RecordGifHotkey = "";` (line 72), add:
```csharp
[RelayCommand] private void ClearOcrTextHotkey() => OcrTextHotkey = "";
```

In the `Save()` method, after `_service.Current.Hotkeys.RecordGif = KeyComboHelper.Parse(RecordGifHotkey);` (line 89), add:
```csharp
_service.Current.Hotkeys.OcrText = KeyComboHelper.Parse(OcrTextHotkey);
```

- [ ] **Step 4: Run tests to confirm they pass**

```bash
dotnet test ShareXMac.Tests --filter "OcrText" -v minimal 2>&1 | tail -10
```

Expected: `4 passed, 0 failed`

- [ ] **Step 5: Add OCR hotkey row to SettingsWindow.axaml**

File: `ShareXMac/Views/SettingsWindow.axaml`

After the Record GIF `<Grid>` block (the one ending at line 124), before `<Separator />`, add:

```xml
            <Grid ColumnDefinitions="130,*,Auto">
                <TextBlock Grid.Column="0" Text="Capture Text (OCR):"
                           VerticalAlignment="Center" />
                <TextBox Grid.Column="1" x:Name="OcrTextBox"
                         Text="{Binding OcrTextHotkey}"
                         Watermark="Not set" IsReadOnly="True"
                         Cursor="Hand" />
                <Button Grid.Column="2" Content="✕"
                        Command="{Binding ClearOcrTextHotkeyCommand}"
                        Margin="4,0,0,0" Padding="8,4" />
            </Grid>
```

- [ ] **Step 6: Wire key capture in SettingsWindow.axaml.cs**

File: `ShareXMac/Views/SettingsWindow.axaml.cs`

After the `RecordGifBox` line (line 21):
```csharp
this.FindControl<TextBox>("RecordGifBox")!.KeyDown        += (_, e) => OnHotkeyKeyDown(e, v => vm.RecordGifHotkey = v);
```

Add:
```csharp
this.FindControl<TextBox>("OcrTextBox")!.KeyDown          += (_, e) => OnHotkeyKeyDown(e, v => vm.OcrTextHotkey = v);
```

- [ ] **Step 7: Verify project builds**

```bash
dotnet build ShareXMac/ShareXMac.csproj -v minimal 2>&1 | tail -10
```

Expected: `Build succeeded. 0 Warning(s). 0 Error(s)`

- [ ] **Step 8: Run full test suite**

```bash
dotnet test ShareXMac.Tests -v minimal 2>&1 | tail -10
```

Expected: all tests pass.

- [ ] **Step 9: Commit**

```bash
git add ShareXMac/ViewModels/SettingsViewModel.cs ShareXMac/Views/SettingsWindow.axaml ShareXMac/Views/SettingsWindow.axaml.cs ShareXMac.Tests/SettingsViewModelTests.cs
git commit -m "feat: add OCR hotkey setting to Settings UI"
```

---

## Self-Review Notes

- Spec requires `OcrService` to be injectable for testing — achieved via `IScreenCapture` parameter which is the testable surface; `TrayViewModel` creates it internally to avoid changing constructor signature and breaking 3 existing test classes.
- `MacVision.RecognizeTextAsync` runs synchronously on the calling thread (it calls `performRequests:error:` which is synchronous). The `Task.FromResult` wrapper in `MacScreenCapture` means `RecognizeTextAsync` returns immediately. This is fine — `OcrService.CaptureAndRecognizeAsync` is still `async` for future flexibility.
- The `OcrResultWindow` uses `WindowStartupLocation="CenterScreen"` (not `PositionBottomRight` like `PostCaptureWindow`) because it requires interaction — a center popup is better for an editable control.
- `TextBox.SelectAll()` + `Focus()` fires in `Opened` (not `Loaded`) to ensure the window is on screen before focus is requested.
