# SnapX Plan 4: Uploaders Integration

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Wire UploadersLib into the post-capture flow — an Upload button on the post-capture toolbar sends images to Imgur (anonymous, requires a free Client ID), copies the URL to clipboard, and records it in history; plus an optional auto-upload setting.

**Architecture:** A new `UploadService` wraps UploadersLib's existing `GenericUploader` hierarchy and is injected into `TrayViewModel` and `PostCaptureViewModel`. `AppSettings` gains three fields (`ImgurClientId`, `ActiveImageDestination`, `AutoUploadAfterCapture`). Imgur anonymous upload is the only destination in Plan 4 (OAuth flows and other destinations come later). All upload work runs on a background thread via `Task.Run`; results surface back to the UI via observable properties.

**Tech Stack:** .NET 10, Avalonia 11.2.1, CommunityToolkit.Mvvm 8.4.0, `ShareX.UploadersLib` (already in project), `ShareX.UploadersLib.ImageUploaders.Imgur`, `ShareX.UploadersLib.OAuth2Info`, `AccountType.Anonymous`.

---

## File Structure

```
ShareXMac/
  Models/
    AppSettings.cs              MODIFY — add ImgurClientId, ActiveImageDestination, AutoUploadAfterCapture
  Services/
    UploadService.cs            CREATE — async upload via UploadersLib, Imgur anonymous
  ViewModels/
    PostCaptureViewModel.cs     MODIFY — add UploadCommand, IsUploading, UploadedUrl, CopyUrlCommand
    SettingsViewModel.cs        MODIFY — add ImgurClientId, ActiveImageDestination, AutoUploadAfterCapture
    TrayViewModel.cs            MODIFY — inject UploadService, auto-upload in OnCaptureComplete
  Views/
    PostCaptureWindow.axaml     MODIFY — add Upload button, status indicator, URL label, Copy URL button
    SettingsWindow.axaml        MODIFY — add Upload section (destination dropdown, Client ID field, auto-upload checkbox)
  App.axaml.cs                  MODIFY — construct UploadService, pass to TrayViewModel

ShareXMac.Tests/
  UploadServiceTests.cs         CREATE
```

---

### Task 1: AppSettings additions

Add three new properties to `AppSettings` so the settings round-trip correctly before any other code is written.

**Files:**
- Modify: `ShareXMac/Models/AppSettings.cs`
- Test: `ShareXMac.Tests/SettingsServiceTests.cs`

- [ ] **Step 1: Write failing test**

Add at the bottom of `ShareXMac.Tests/SettingsServiceTests.cs` (inside the existing `SettingsServiceTests` class):

```csharp
[Fact]
public void AppSettings_UploadDefaults_AreCorrect()
{
    var s = new AppSettings();
    Assert.Equal("", s.ImgurClientId);
    Assert.Equal(ShareX.UploadersLib.ImageDestination.Imgur, s.ActiveImageDestination);
    Assert.False(s.AutoUploadAfterCapture);
}
```

Add `using ShareX.UploadersLib;` to the top of `SettingsServiceTests.cs`.

- [ ] **Step 2: Run to confirm compile failure**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet test ShareXMac.Tests/ShareXMac.Tests.csproj --filter "AppSettings_UploadDefaults" 2>&1 | tail -5
```
Expected: compile error (`ImgurClientId` not found)

- [ ] **Step 3: Add the three properties to AppSettings.cs**

Replace `ShareXMac/Models/AppSettings.cs` entirely:

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
}
```

- [ ] **Step 4: Run tests**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet test ShareXMac.Tests/ShareXMac.Tests.csproj --filter "AppSettings_UploadDefaults" -v normal 2>&1 | tail -5
```
Expected: 1 passed

- [ ] **Step 5: Run all tests (no regressions)**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet test ShareXMac.Tests/ShareXMac.Tests.csproj 2>&1 | tail -3
```
Expected: all pass (39 tests)

- [ ] **Step 6: Commit**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && git add -A && git commit -m "feat: add upload fields to AppSettings"
```

---

### Task 2: UploadService

Wrap UploadersLib's `Imgur` class in a service that performs async uploads and returns a URL string on success.

**Files:**
- Create: `ShareXMac/Services/UploadService.cs`
- Create: `ShareXMac.Tests/UploadServiceTests.cs`

- [ ] **Step 1: Write failing tests**

Create `ShareXMac.Tests/UploadServiceTests.cs`:

```csharp
using ShareX.UploadersLib;
using ShareXMac.Models;
using ShareXMac.Services;
using Xunit;

namespace ShareXMac.Tests;

public class UploadServiceTests
{
    [Fact]
    public void UploadService_Constructor_DoesNotThrow()
    {
        var svc = new UploadService();
        Assert.NotNull(svc);
    }

    [Fact]
    public async Task UploadImageAsync_EmptyClientId_ReturnsNull()
    {
        var svc = new UploadService();
        var settings = new AppSettings { ImgurClientId = "" };
        byte[] data = new byte[] { 0x89, 0x50, 0x4E, 0x47 }; // PNG magic bytes only
        var result = await svc.UploadImageAsync(data, "test.png", settings);
        Assert.Null(result);
    }

    [Fact]
    public async Task UploadImageAsync_EmptyData_ReturnsNull()
    {
        var svc = new UploadService();
        var settings = new AppSettings { ImgurClientId = "any-id" };
        var result = await svc.UploadImageAsync(Array.Empty<byte>(), "test.png", settings);
        Assert.Null(result);
    }
}
```

- [ ] **Step 2: Run to confirm compile failure**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet test ShareXMac.Tests/ShareXMac.Tests.csproj --filter "UploadServiceTests" 2>&1 | tail -5
```
Expected: compile error

- [ ] **Step 3: Create UploadService.cs**

Create `ShareXMac/Services/UploadService.cs`:

```csharp
using ShareX.UploadersLib;
using ShareX.UploadersLib.ImageUploaders;
using ShareXMac.Models;

namespace ShareXMac.Services;

public class UploadService
{
    /// <summary>
    /// Uploads image data to the configured destination.
    /// Returns the public URL on success, null if configuration is missing or upload fails.
    /// </summary>
    public async Task<string?> UploadImageAsync(byte[] data, string fileName, AppSettings settings)
    {
        if (data.Length == 0) return null;
        if (settings.ActiveImageDestination == ImageDestination.Imgur
            && string.IsNullOrWhiteSpace(settings.ImgurClientId))
            return null;

        return await Task.Run(() =>
        {
            try
            {
                GenericUploader? uploader = CreateUploader(settings);
                if (uploader == null) return null;
                using var ms = new MemoryStream(data);
                UploadResult result = uploader.Upload(ms, fileName);
                return result?.IsSuccess == true ? result.URL : null;
            }
            catch
            {
                return null;
            }
        });
    }

    private static GenericUploader? CreateUploader(AppSettings settings) =>
        settings.ActiveImageDestination switch
        {
            ImageDestination.Imgur => new Imgur(
                new OAuth2Info(settings.ImgurClientId, ""))
            {
                UploadMethod = AccountType.Anonymous,
                DirectLink = true
            },
            _ => null
        };
}
```

- [ ] **Step 4: Run tests**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet test ShareXMac.Tests/ShareXMac.Tests.csproj --filter "UploadServiceTests" -v normal 2>&1 | tail -10
```
Expected: 3 passed (no network calls — empty Client ID and empty data guard return null early)

- [ ] **Step 5: Run all tests**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet test ShareXMac.Tests/ShareXMac.Tests.csproj 2>&1 | tail -3
```
Expected: all pass (42 tests)

- [ ] **Step 6: Commit**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && git add -A && git commit -m "feat: add UploadService wrapping Imgur anonymous upload"
```

---

### Task 3: PostCaptureViewModel upload support

Add `UploadCommand`, `IsUploading`, `UploadedUrl`, and `CopyUrlCommand` to `PostCaptureViewModel`. The view model receives an `UploadService` and `AppSettings` so it can perform the upload itself.

**Files:**
- Modify: `ShareXMac/ViewModels/PostCaptureViewModel.cs`
- Test: `ShareXMac.Tests/PostCaptureViewModelTests.cs`

- [ ] **Step 1: Write failing tests**

Add to `ShareXMac.Tests/PostCaptureViewModelTests.cs` (inside the existing `PostCaptureViewModelTests` class):

```csharp
[Fact]
public void PostCaptureViewModel_WithUploadService_HasUploadCommand()
{
    var result = new CaptureResult(MinimalPng, "/tmp/test.png");
    var vm = new PostCaptureViewModel(result, new UploadService(), new AppSettings());
    Assert.NotNull(vm.UploadCommand);
}

[Fact]
public void PostCaptureViewModel_InitialState_IsNotUploading()
{
    var result = new CaptureResult(MinimalPng, "/tmp/test.png");
    var vm = new PostCaptureViewModel(result, new UploadService(), new AppSettings());
    Assert.False(vm.IsUploading);
    Assert.Null(vm.UploadedUrl);
}

[Fact]
public void PostCaptureViewModel_CopyUrlCommand_NotNull()
{
    var result = new CaptureResult(MinimalPng, "/tmp/test.png");
    var vm = new PostCaptureViewModel(result, new UploadService(), new AppSettings());
    Assert.NotNull(vm.CopyUrlCommand);
}
```

Add `using ShareXMac.Services;` to the top of `PostCaptureViewModelTests.cs`.

- [ ] **Step 2: Run to confirm compile failure**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet test ShareXMac.Tests/ShareXMac.Tests.csproj --filter "PostCaptureViewModelTests" 2>&1 | tail -5
```
Expected: compile error (3-arg constructor doesn't exist yet)

- [ ] **Step 3: Replace PostCaptureViewModel.cs**

Replace `ShareXMac/ViewModels/PostCaptureViewModel.cs` entirely:

```csharp
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShareXMac.Models;
using ShareXMac.ScreenCaptureLib;
using ShareXMac.Services;
using System.Diagnostics;

namespace ShareXMac.ViewModels;

public partial class PostCaptureViewModel : ObservableObject, IDisposable
{
    public string FilePath { get; }
    public Bitmap Thumbnail { get; }
    public int AutoDismissSeconds { get; init; } = 8;

    [ObservableProperty] private bool _isUploading;
    [ObservableProperty] private string? _uploadedUrl;

    private readonly byte[] _imageData;
    private readonly UploadService _uploadService;
    private readonly AppSettings _settings;

    public event Action? CloseRequested;

    public PostCaptureViewModel(CaptureResult result, UploadService uploadService, AppSettings settings)
    {
        FilePath = result.FilePath;
        _imageData = result.ImageData;
        _uploadService = uploadService;
        _settings = settings;
        using var ms = new MemoryStream(result.ImageData);
        Thumbnail = Bitmap.DecodeToWidth(ms, 360);
    }

    [RelayCommand]
    private void CopyImage() => MacClipboard.SetImage(_imageData);

    [RelayCommand]
    private void CopyPath() => MacClipboard.SetText(FilePath);

    [RelayCommand]
    private void OpenInFinder() =>
        Process.Start(new ProcessStartInfo("open")
            { UseShellExecute = false, ArgumentList = { "-R", FilePath } });

    [RelayCommand]
    private async Task Upload()
    {
        if (IsUploading) return;
        IsUploading = true;
        try
        {
            string fileName = Path.GetFileName(FilePath);
            string? url = await _uploadService.UploadImageAsync(_imageData, fileName, _settings);
            if (url != null)
            {
                UploadedUrl = url;
                MacClipboard.SetText(url);
            }
        }
        finally
        {
            IsUploading = false;
        }
    }

    [RelayCommand]
    private void CopyUrl()
    {
        if (UploadedUrl != null)
            MacClipboard.SetText(UploadedUrl);
    }

    [RelayCommand]
    private void Dismiss() => CloseRequested?.Invoke();

    public void Dispose() => Thumbnail.Dispose();
}
```

- [ ] **Step 4: Update existing PostCaptureViewModelTests — fix 2-arg constructor calls**

The existing tests in `PostCaptureViewModelTests.cs` use `new PostCaptureViewModel(result)` (old 1-arg constructor). Update all four existing instantiations to use the new 3-arg form:

```csharp
new PostCaptureViewModel(result, new UploadService(), new AppSettings())
```

The four affected tests are: `Constructor_SetsFilePath`, `Constructor_CreatesThumbnailProperty`, `DismissCommand_RaisesCloseRequested`, `CopyPathCommand_DoesNotThrow`.

- [ ] **Step 5: Run tests**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet test ShareXMac.Tests/ShareXMac.Tests.csproj --filter "PostCaptureViewModelTests" -v normal 2>&1 | tail -10
```
Expected: 7 passed

- [ ] **Step 6: Run all tests**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet test ShareXMac.Tests/ShareXMac.Tests.csproj 2>&1 | tail -3
```
Expected: all pass

- [ ] **Step 7: Commit**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && git add -A && git commit -m "feat: add upload commands to PostCaptureViewModel"
```

---

### Task 4: PostCaptureWindow AXAML — Upload button + URL display

Add an Upload button that shows a spinner while uploading, and a URL label + Copy URL button after success.

**Files:**
- Modify: `ShareXMac/Views/PostCaptureWindow.axaml`

No new unit tests — verified by build passing.

- [ ] **Step 1: Replace PostCaptureWindow.axaml**

Replace `ShareXMac/Views/PostCaptureWindow.axaml` entirely:

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="using:ShareXMac.ViewModels"
        x:Class="ShareXMac.Views.PostCaptureWindow"
        x:DataType="vm:PostCaptureViewModel"
        Width="360" Height="280"
        MinWidth="360" MinHeight="280"
        CanResize="False"
        SystemDecorations="BorderOnly"
        WindowStartupLocation="Manual"
        Title="ShareX Mac — Capture"
        Background="#FF1E1E1E">
    <Grid RowDefinitions="*,Auto,Auto" Margin="12">

        <!-- Thumbnail -->
        <Border Grid.Row="0" Background="#FF2D2D2D" CornerRadius="4">
            <Image Source="{Binding Thumbnail}" Stretch="Uniform" Margin="4" />
        </Border>

        <!-- Action buttons -->
        <StackPanel Grid.Row="1" Orientation="Horizontal"
                    HorizontalAlignment="Center" Spacing="6" Margin="0,8,0,0">
            <Button Content="Copy Image"
                    Command="{Binding CopyImageCommand}"
                    Background="#FF0078D4" Foreground="White"
                    Padding="10,5" />
            <Button Content="Copy Path"
                    Command="{Binding CopyPathCommand}"
                    Padding="10,5" />
            <Button Content="Open in Finder"
                    Command="{Binding OpenInFinderCommand}"
                    Padding="10,5" />
            <Button Content="Upload"
                    Command="{Binding UploadCommand}"
                    IsEnabled="{Binding !IsUploading}"
                    Background="#FF107C10" Foreground="White"
                    Padding="10,5" />
            <Button Content="Dismiss"
                    Command="{Binding DismissCommand}"
                    Padding="10,5" />
        </StackPanel>

        <!-- Upload status row -->
        <Grid Grid.Row="2" Margin="0,6,0,0" ColumnDefinitions="*,Auto"
              IsVisible="{Binding IsUploading}">
            <TextBlock Grid.Column="0" Text="Uploading…"
                       Foreground="#FFAAAAAA" FontSize="11"
                       VerticalAlignment="Center" />
        </Grid>

        <!-- URL row (shown after successful upload) -->
        <Grid Grid.Row="2" Margin="0,6,0,0" ColumnDefinitions="*,Auto"
              IsVisible="{Binding UploadedUrl, Converter={x:Static StringConverters.IsNotNullOrEmpty}}">
            <TextBlock Grid.Column="0"
                       Text="{Binding UploadedUrl}"
                       Foreground="#FF0078D4" FontSize="11"
                       TextTrimming="CharacterEllipsis"
                       VerticalAlignment="Center" />
            <Button Grid.Column="1" Content="Copy URL"
                    Command="{Binding CopyUrlCommand}"
                    Padding="8,4" Margin="6,0,0,0" />
        </Grid>
    </Grid>
</Window>
```

**Note:** The URL row and uploading row both use Grid.Row="2". Avalonia allows multiple children in the same grid row — only one will be visible at a time via `IsVisible` bindings. `StringConverters.IsNotNullOrEmpty` is built into Avalonia — no extra namespace needed.

- [ ] **Step 2: Update PositionBottomRight in PostCaptureWindow.axaml.cs**

The window height increased from 240 to 280. Open `ShareXMac/Views/PostCaptureWindow.axaml.cs` — the `PositionBottomRight` method uses `Height` which Avalonia reads from the AXAML at load time, so no code change is needed. Verify the file still builds.

- [ ] **Step 3: Build check**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet build ShareXMac/ShareXMac.csproj 2>&1 | tail -5
```
Expected: `0 Error(s)`

If `StringConverters` is not found, add `xmlns:str="clr-namespace:Avalonia.Data.Converters;assembly=Avalonia"` to the Window element and use `str:StringConverters.IsNotNullOrEmpty`.

- [ ] **Step 4: Run all tests (no regressions)**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet test ShareXMac.Tests/ShareXMac.Tests.csproj 2>&1 | tail -3
```
Expected: all pass

- [ ] **Step 5: Commit**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && git add -A && git commit -m "feat: add Upload button and URL display to PostCaptureWindow"
```

---

### Task 5: SettingsViewModel + SettingsWindow upload section

Add upload fields to `SettingsViewModel` and a new Upload section to `SettingsWindow.axaml`.

**Files:**
- Modify: `ShareXMac/ViewModels/SettingsViewModel.cs`
- Modify: `ShareXMac/Views/SettingsWindow.axaml`
- Test: `ShareXMac.Tests/SettingsServiceTests.cs` (add SettingsViewModelUploadTests class)

- [ ] **Step 1: Write failing tests**

Add a new class at the bottom of `ShareXMac.Tests/SettingsServiceTests.cs`:

```csharp
public class SettingsViewModelUploadTests
{
    [Fact]
    public void SettingsViewModel_LoadsUploadValues()
    {
        string tempFile = Path.Combine(Path.GetTempPath(), $"s-up-{Guid.NewGuid():N}.json");
        try
        {
            var svc = new SettingsService(tempFile);
            svc.Current.ImgurClientId = "myclientid";
            svc.Current.AutoUploadAfterCapture = true;

            var vm = new SettingsViewModel(svc);
            Assert.Equal("myclientid", vm.ImgurClientId);
            Assert.True(vm.AutoUploadAfterCapture);
        }
        finally { if (File.Exists(tempFile)) File.Delete(tempFile); }
    }

    [Fact]
    public void SettingsViewModel_SaveCommand_PersistsUploadValues()
    {
        string tempFile = Path.Combine(Path.GetTempPath(), $"s-up2-{Guid.NewGuid():N}.json");
        try
        {
            var svc = new SettingsService(tempFile);
            var vm = new SettingsViewModel(svc);
            vm.ImgurClientId = "newid";
            vm.AutoUploadAfterCapture = true;
            vm.SaveCommand.Execute(null);

            var svc2 = new SettingsService(tempFile);
            Assert.Equal("newid", svc2.Current.ImgurClientId);
            Assert.True(svc2.Current.AutoUploadAfterCapture);
        }
        finally { if (File.Exists(tempFile)) File.Delete(tempFile); }
    }
}
```

- [ ] **Step 2: Run to confirm compile failure**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet test ShareXMac.Tests/ShareXMac.Tests.csproj --filter "SettingsViewModelUploadTests" 2>&1 | tail -5
```
Expected: compile error (`ImgurClientId` not on SettingsViewModel yet)

- [ ] **Step 3: Update SettingsViewModel.cs**

Replace `ShareXMac/ViewModels/SettingsViewModel.cs` entirely:

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShareX.UploadersLib;
using ShareXMac.Services;

namespace ShareXMac.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly SettingsService _service;

    [ObservableProperty] private string _savePath = "";
    [ObservableProperty] private bool _autoCopyImage;
    [ObservableProperty] private bool _showPostCaptureToolbar;
    [ObservableProperty] private int _postCaptureTimeoutSeconds;
    [ObservableProperty] private string _imgurClientId = "";
    [ObservableProperty] private ImageDestination _activeImageDestination;
    [ObservableProperty] private bool _autoUploadAfterCapture;

    public event Action? CloseRequested;

    public SettingsViewModel(SettingsService service)
    {
        _service = service;
        var s = service.Current;
        SavePath = s.SavePath;
        AutoCopyImage = s.AutoCopyImage;
        ShowPostCaptureToolbar = s.ShowPostCaptureToolbar;
        PostCaptureTimeoutSeconds = s.PostCaptureToolbarTimeoutSeconds;
        ImgurClientId = s.ImgurClientId;
        ActiveImageDestination = s.ActiveImageDestination;
        AutoUploadAfterCapture = s.AutoUploadAfterCapture;
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
        _service.Current.ImgurClientId = ImgurClientId;
        _service.Current.ActiveImageDestination = ActiveImageDestination;
        _service.Current.AutoUploadAfterCapture = AutoUploadAfterCapture;
        _service.Save();
        CloseRequested?.Invoke();
    }
}
```

- [ ] **Step 4: Update SettingsWindow.axaml**

Replace `ShareXMac/Views/SettingsWindow.axaml` entirely:

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="using:ShareXMac.ViewModels"
        xmlns:uploaders="using:ShareX.UploadersLib"
        x:Class="ShareXMac.Views.SettingsWindow"
        x:DataType="vm:SettingsViewModel"
        Width="480" Height="440"
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
                <ComboBox Grid.Column="1"
                          SelectedItem="{Binding ActiveImageDestination}"
                          HorizontalAlignment="Left" Width="160">
                    <uploaders:ImageDestination>Imgur</uploaders:ImageDestination>
                </ComboBox>
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

            <Button Content="Save" Command="{Binding SaveCommand}"
                    HorizontalAlignment="Right" Background="#FF0078D4"
                    Foreground="White" Padding="16,8" />
        </StackPanel>
    </ScrollViewer>
</Window>
```

**Note on ComboBox:** The `ComboBox` above with a single static `ImageDestination.Imgur` item is intentional for Plan 4 — additional destinations will be added in a later plan when their credential UIs are built. If the AXAML compiler rejects the inline enum value syntax, replace the ComboBox with:
```xml
<TextBlock Grid.Column="1" Text="Imgur" VerticalAlignment="Center" />
```
and remove the `xmlns:uploaders` declaration. The `ActiveImageDestination` property will stay `Imgur` (default) until multi-destination support is added.

- [ ] **Step 5: Run ViewModel tests**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet test ShareXMac.Tests/ShareXMac.Tests.csproj --filter "SettingsViewModelUploadTests" -v normal 2>&1 | tail -10
```
Expected: 2 passed

- [ ] **Step 6: Build check**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet build ShareXMac/ShareXMac.csproj 2>&1 | tail -5
```
Expected: `0 Error(s)`. If AXAML ComboBox syntax fails, apply the TextBlock fallback from the note above.

- [ ] **Step 7: Run all tests**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet test ShareXMac.Tests/ShareXMac.Tests.csproj 2>&1 | tail -3
```
Expected: all pass

- [ ] **Step 8: Commit**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && git add -A && git commit -m "feat: add upload settings to SettingsViewModel and SettingsWindow"
```

---

### Task 6: TrayViewModel — auto-upload + URL in history

Inject `UploadService` into `TrayViewModel`. When `AutoUploadAfterCapture` is true, call the service in `OnCaptureComplete` and record the URL in history.

**Files:**
- Modify: `ShareXMac/ViewModels/TrayViewModel.cs`
- Modify: `ShareXMac.Tests/TrayViewModelTests.cs`

- [ ] **Step 1: Write failing test**

Add to `ShareXMac.Tests/TrayViewModelTests.cs` (inside `TrayViewModelTests`):

```csharp
[Fact]
public void TrayViewModel_WithUploadService_DoesNotThrow()
{
    string sf = Path.Combine(Path.GetTempPath(), $"s-{Guid.NewGuid():N}.json");
    string hf = Path.Combine(Path.GetTempPath(), $"h-{Guid.NewGuid():N}.json");
    try
    {
        var vm = new TrayViewModel(
            new StubScreenCapture(),
            new SettingsService(sf),
            new HistoryService(hf),
            new UploadService());
        Assert.NotNull(vm.CaptureRegionCommand);
    }
    finally
    {
        if (File.Exists(sf)) File.Delete(sf);
        if (File.Exists(hf)) File.Delete(hf);
    }
}
```

Add `using ShareXMac.Services;` to `TrayViewModelTests.cs` if not already present.

- [ ] **Step 2: Run to confirm compile failure**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet test ShareXMac.Tests/ShareXMac.Tests.csproj --filter "TrayViewModel_WithUploadService" 2>&1 | tail -5
```
Expected: compile error

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
    private readonly UploadService _upload;
    private bool _isRecording;

    public TrayViewModel(
        IScreenCapture capture,
        SettingsService settings,
        HistoryService history,
        UploadService upload)
    {
        _capture = capture;
        _settings = settings;
        _history = history;
        _upload = upload;
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
        string dir = _settings.Current.SavePath;
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, $"ShareX-{DateTime.Now:yyyy-MM-dd-HHmmss}.png");
        await File.WriteAllBytesAsync(path, data);

        // Auto-upload before recording history so URL is captured
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
                    AutoDismissSeconds = _settings.Current.PostCaptureToolbarTimeoutSeconds
                };
                new PostCaptureWindow(vm).Show();
            });
        }
    }
}
```

- [ ] **Step 4: Update existing TrayViewModel tests to pass UploadService**

Open `ShareXMac.Tests/TrayViewModelTests.cs`. Update every `new TrayViewModel(...)` call to include `new UploadService()` as the fourth argument. There are three: `TrayViewModel_CaptureRegionCommand_IsNotNull`, `TrayViewModel_QuitCommand_IsNotNull`, and `TrayViewModel_WithServices_DoesNotThrow`. Change each to:

```csharp
new TrayViewModel(
    new StubScreenCapture(),
    new SettingsService(Path.GetTempFileName()),
    new HistoryService(Path.GetTempFileName()),
    new UploadService())
```

- [ ] **Step 5: Run all tests**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet test ShareXMac.Tests/ShareXMac.Tests.csproj 2>&1 | tail -3
```
Expected: all pass

- [ ] **Step 6: Commit**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && git add -A && git commit -m "feat: auto-upload in TrayViewModel, URL recorded in history"
```

---

### Task 7: Wire App.axaml.cs

Construct `UploadService` in `App.axaml.cs` and pass it to `TrayViewModel`.

**Files:**
- Modify: `ShareXMac/App.axaml.cs`

No new tests — the existing tests already exercise the wired constructor. Success is verified by the build and all tests passing.

- [ ] **Step 1: Replace App.axaml.cs**

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
        var upload   = new UploadService();

        DataContext = new TrayViewModel(capture, settings, history, upload);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.ShutdownMode = Avalonia.Controls.ShutdownMode.OnExplicitShutdown;

        base.OnFrameworkInitializationCompleted();
    }
}
```

- [ ] **Step 2: Full build check**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet build ShareXMac/ShareXMac.csproj 2>&1 | tail -5
```
Expected: `0 Error(s)`

- [ ] **Step 3: Run all tests**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet test ShareXMac.Tests/ShareXMac.Tests.csproj 2>&1 | tail -3
```
Expected: all pass

- [ ] **Step 4: Commit**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && git add -A && git commit -m "feat: wire UploadService into App.axaml.cs"
```

---

## Plan Complete

After all 7 tasks:
- Capturing a screenshot and clicking **Upload** on the toolbar sends it to Imgur anonymously and copies the URL to clipboard (requires a free Imgur Client ID configured in Settings)
- **Auto-upload** option in Settings uploads every capture automatically, skipping the clipboard image copy in favour of the URL
- Uploaded URLs appear in the **History** window alongside file paths
- Settings window has a new Upload section: destination (Imgur), Client ID field, and auto-upload checkbox

**To get an Imgur Client ID:** Visit `https://api.imgur.com/oauth2/addclient`, register an application as "Anonymous usage without user authorization", and paste the Client ID into Settings.

**Next plan:** Plan 5 — Packaging & distribution: GitHub Actions CI, app bundle (`Info.plist`, entitlements), code signing, notarization, `.dmg` creation, and Login Items (launch at login).
