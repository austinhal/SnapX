# ShareX Mac — Plan 1: Foundation

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Stand up the `ShareX-Mac` repo with all 6 ported libraries building on macOS and a launchable Avalonia menu bar skeleton with stub platform interfaces.

**Architecture:** Copy each ShareX library, delete Windows UI layers (Forms/, Controls/, UITypeEditors/, Native/, Input/), update csproj targets to `net9.0`, fix remaining compiler errors, then wire them into a minimal Avalonia app that shows a macOS status bar icon. Platform capabilities (capture, hotkeys, notifications) are defined as interfaces with stub implementations — real implementations come in Plan 2.

**Tech Stack:** .NET 9, Avalonia 11, CommunityToolkit.Mvvm 8, xUnit (tests), System.Drawing.Common with Unix support (temporary; SkiaSharp migration is Plan 3 scope)

**Source repo (read-only reference):** `~/Documents/Dev/Projects/ShareX-arm64/`
**Target repo:** `~/Documents/Dev/Projects/ShareX-Mac/`

---

### Task 1: Initialize repo

**Files:**
- Create: `~/Documents/Dev/Projects/ShareX-Mac/.gitignore`
- Create: `~/Documents/Dev/Projects/ShareX-Mac/Directory.Build.props`
- Create: `~/Documents/Dev/Projects/ShareX-Mac/runtimeconfig.template.json`

- [ ] **Step 1: Init git repo**

```bash
cd ~/Documents/Dev/Projects/ShareX-Mac
git init
```

Expected: `Initialized empty Git repository`

- [ ] **Step 2: Create .gitignore**

```bash
cat > .gitignore << 'EOF'
bin/
obj/
.vs/
.idea/
*.user
*.suo
.DS_Store
EOF
```

- [ ] **Step 3: Create Directory.Build.props**

```xml
<!-- Directory.Build.props -->
<Project>
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <RuntimeIdentifiers>osx-arm64;osx-x64</RuntimeIdentifiers>
    <AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
  </PropertyGroup>
</Project>
```

- [ ] **Step 4: Create runtimeconfig.template.json (enables System.Drawing on macOS)**

```json
{
  "configProperties": {
    "System.Drawing.EnableUnixSupport": true
  }
}
```

- [ ] **Step 5: Commit**

```bash
git add .gitignore Directory.Build.props runtimeconfig.template.json
git commit -m "chore: initialize repo"
```

---

### Task 2: Create solution and project stubs

**Files:**
- Create: `ShareX-Mac.sln`
- Create: `ShareXMac/ShareXMac.csproj`
- Create: `ShareXMac.HelpersLib/ShareXMac.HelpersLib.csproj`
- Create: `ShareXMac.HistoryLib/ShareXMac.HistoryLib.csproj`
- Create: `ShareXMac.ImageEffectsLib/ShareXMac.ImageEffectsLib.csproj`
- Create: `ShareXMac.IndexerLib/ShareXMac.IndexerLib.csproj`
- Create: `ShareXMac.MediaLib/ShareXMac.MediaLib.csproj`
- Create: `ShareXMac.UploadersLib/ShareXMac.UploadersLib.csproj`
- Create: `ShareXMac.ScreenCaptureLib/ShareXMac.ScreenCaptureLib.csproj`
- Create: `ShareXMac.Tests/ShareXMac.Tests.csproj`

- [ ] **Step 1: Create solution and project directories**

```bash
cd ~/Documents/Dev/Projects/ShareX-Mac
dotnet new sln -n ShareX-Mac
mkdir -p ShareXMac ShareXMac.HelpersLib ShareXMac.HistoryLib \
  ShareXMac.ImageEffectsLib ShareXMac.IndexerLib ShareXMac.MediaLib \
  ShareXMac.UploadersLib ShareXMac.ScreenCaptureLib ShareXMac.Tests
```

- [ ] **Step 2: Create ShareXMac.HelpersLib.csproj**

```xml
<!-- ShareXMac.HelpersLib/ShareXMac.HelpersLib.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Library</OutputType>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
    <PackageReference Include="System.Drawing.Common" Version="9.0.0" />
  </ItemGroup>
</Project>
```

- [ ] **Step 3: Create ShareXMac.HistoryLib.csproj**

```xml
<!-- ShareXMac.HistoryLib/ShareXMac.HistoryLib.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Library</OutputType>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.Data.Sqlite" Version="9.0.8" />
    <PackageReference Include="Microsoft.Data.Sqlite.Core" Version="9.0.8" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\ShareXMac.HelpersLib\ShareXMac.HelpersLib.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 4: Create ShareXMac.ImageEffectsLib.csproj**

```xml
<!-- ShareXMac.ImageEffectsLib/ShareXMac.ImageEffectsLib.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Library</OutputType>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
    <PackageReference Include="System.Drawing.Common" Version="9.0.0" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\ShareXMac.HelpersLib\ShareXMac.HelpersLib.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 5: Create ShareXMac.IndexerLib.csproj**

```xml
<!-- ShareXMac.IndexerLib/ShareXMac.IndexerLib.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Library</OutputType>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\ShareXMac.HelpersLib\ShareXMac.HelpersLib.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 6: Create ShareXMac.MediaLib.csproj**

```xml
<!-- ShareXMac.MediaLib/ShareXMac.MediaLib.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Library</OutputType>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\ShareXMac.HelpersLib\ShareXMac.HelpersLib.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 7: Create ShareXMac.UploadersLib.csproj**

```xml
<!-- ShareXMac.UploadersLib/ShareXMac.UploadersLib.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Library</OutputType>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="FluentFTP" Version="52.1.0" />
    <PackageReference Include="MegaApiClient" Version="1.10.4" />
    <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
    <PackageReference Include="SSH.NET" Version="2025.0.0" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\ShareXMac.HelpersLib\ShareXMac.HelpersLib.csproj" />
  </ItemGroup>
  <ItemGroup>
    <Content Include="Resources\OAuthCallbackPage.html" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>
</Project>
```

- [ ] **Step 8: Create ShareXMac.ScreenCaptureLib.csproj**

```xml
<!-- ShareXMac.ScreenCaptureLib/ShareXMac.ScreenCaptureLib.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Library</OutputType>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\ShareXMac.HelpersLib\ShareXMac.HelpersLib.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 9: Create ShareXMac.csproj (Avalonia app shell — content comes in Task 10)**

```xml
<!-- ShareXMac/ShareXMac.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Avalonia" Version="11.2.1" />
    <PackageReference Include="Avalonia.Desktop" Version="11.2.1" />
    <PackageReference Include="Avalonia.Themes.Fluent" Version="11.2.1" />
    <PackageReference Include="Avalonia.Fonts.Inter" Version="11.2.1" />
    <PackageReference Include="CommunityToolkit.Mvvm" Version="8.4.0" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\ShareXMac.HelpersLib\ShareXMac.HelpersLib.csproj" />
    <ProjectReference Include="..\ShareXMac.UploadersLib\ShareXMac.UploadersLib.csproj" />
    <ProjectReference Include="..\ShareXMac.ImageEffectsLib\ShareXMac.ImageEffectsLib.csproj" />
    <ProjectReference Include="..\ShareXMac.HistoryLib\ShareXMac.HistoryLib.csproj" />
    <ProjectReference Include="..\ShareXMac.IndexerLib\ShareXMac.IndexerLib.csproj" />
    <ProjectReference Include="..\ShareXMac.MediaLib\ShareXMac.MediaLib.csproj" />
    <ProjectReference Include="..\ShareXMac.ScreenCaptureLib\ShareXMac.ScreenCaptureLib.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 10: Create ShareXMac.Tests.csproj**

```xml
<!-- ShareXMac.Tests/ShareXMac.Tests.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Library</OutputType>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
    <PackageReference Include="xunit" Version="2.9.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\ShareXMac\ShareXMac.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 11: Add all projects to the solution**

```bash
cd ~/Documents/Dev/Projects/ShareX-Mac
dotnet sln add ShareXMac/ShareXMac.csproj
dotnet sln add ShareXMac.HelpersLib/ShareXMac.HelpersLib.csproj
dotnet sln add ShareXMac.HistoryLib/ShareXMac.HistoryLib.csproj
dotnet sln add ShareXMac.ImageEffectsLib/ShareXMac.ImageEffectsLib.csproj
dotnet sln add ShareXMac.IndexerLib/ShareXMac.IndexerLib.csproj
dotnet sln add ShareXMac.MediaLib/ShareXMac.MediaLib.csproj
dotnet sln add ShareXMac.UploadersLib/ShareXMac.UploadersLib.csproj
dotnet sln add ShareXMac.ScreenCaptureLib/ShareXMac.ScreenCaptureLib.csproj
dotnet sln add ShareXMac.Tests/ShareXMac.Tests.csproj
```

Expected: each line prints `Project ... added to the solution.`

- [ ] **Step 12: Commit**

```bash
git add .
git commit -m "chore: add solution and project stubs"
```

---

### Task 3: Port HelpersLib — bulk copy and delete Windows layers

**Files:**
- Create: `ShareXMac.HelpersLib/` (all non-UI, non-Windows files from `ShareX.HelpersLib/`)

- [ ] **Step 1: Copy all files from source**

```bash
SOURCE=~/Documents/Dev/Projects/ShareX-arm64/ShareX.HelpersLib
DEST=~/Documents/Dev/Projects/ShareX-Mac/ShareXMac.HelpersLib
cp -r $SOURCE/. $DEST/
# Remove the source csproj — we wrote our own in Task 2
rm $DEST/ShareX.HelpersLib.csproj
```

- [ ] **Step 2: Delete Windows UI directories entirely**

These directories contain WinForms Forms, Controls, UITypeEditors, and Windows P/Invoke bindings. All will be replaced by Avalonia equivalents in Plan 3.

```bash
cd ~/Documents/Dev/Projects/ShareX-Mac/ShareXMac.HelpersLib
rm -rf Forms Controls UITypeEditors Native Input
```

- [ ] **Step 3: Delete Windows-only standalone files**

```bash
cd ~/Documents/Dev/Projects/ShareX-Mac/ShareXMac.HelpersLib
rm -f DesktopIconManager.cs   # Windows DWM/Shell32
rm -f DWMManager.cs            # Windows DWM API
rm -f TimerResolutionManager.cs # Windows multimedia timer
rm -f ControlHider.cs          # WinForms control
rm -f ListViewColumnSorter.cs  # WinForms ListView
rm -f WindowState.cs           # Windows window state
rm -f TextBoxTraceListener.cs  # WinForms TextBox
rm -f SingleInstanceManager.cs # Windows named mutex
rm -f FontSafe.cs              # WinForms font wrapper
rm -f ShareXResources.cs       # WinForms resource manager
rm -f ShareXTheme.cs           # WinForms theme
rm -f Helpers/RegistryHelpers.cs # Windows registry
rm -f Helpers/ShortcutHelpers.cs # Windows .lnk shortcuts
rm -f Extensions/FormExtensions.cs # WinForms extensions
```

- [ ] **Step 4: Delete also the Printer subdirectory (Windows print spooler)**

```bash
rm -rf ~/Documents/Dev/Projects/ShareX-Mac/ShareXMac.HelpersLib/Printer
```

- [ ] **Step 5: Commit progress before fixing remaining errors**

```bash
cd ~/Documents/Dev/Projects/ShareX-Mac
git add ShareXMac.HelpersLib/
git commit -m "feat: copy HelpersLib and remove Windows UI/platform layers"
```

---

### Task 4: Port HelpersLib — fix remaining compiler errors

**Files:**
- Modify: `ShareXMac.HelpersLib/Helpers/ClipboardHelpers.cs`
- Modify: `ShareXMac.HelpersLib/Helpers/FileHelpers.cs`
- Modify: `ShareXMac.HelpersLib/Helpers/Helpers.cs`
- Modify: `ShareXMac.HelpersLib/Helpers/ImageHelpers.cs`
- Modify: `ShareXMac.HelpersLib/Helpers/CaptureHelpers.cs`
- Modify: `ShareXMac.HelpersLib/Extensions/GraphicsExtensions.cs`

- [ ] **Step 1: Run build to see all errors**

```bash
cd ~/Documents/Dev/Projects/ShareX-Mac
dotnet build ShareXMac.HelpersLib/ShareXMac.HelpersLib.csproj 2>&1 | grep "error CS"
```

Expected: multiple `error CS0246` (type not found) and `error CS0103` (name not found) for Windows types.

- [ ] **Step 2: Fix CaptureHelpers.cs — stub Windows capture methods**

`CaptureHelpers.cs` uses Windows GDI+ screen capture. On macOS these are replaced by ScreenCaptureKit (Plan 2). Replace the file body with macOS stubs:

Open `ShareXMac.HelpersLib/Helpers/CaptureHelpers.cs`. Remove all methods that call Windows APIs (`BitBlt`, `PrintWindow`, `GetDC`, etc.) and replace with:

```csharp
// CaptureHelpers.cs — Windows capture methods stubbed; real implementation in ShareXMac.ScreenCaptureLib
namespace ShareX.HelpersLib;

public static class CaptureHelpers
{
    public static Rectangle GetScreenBounds()
    {
        // Returns primary display bounds; refined in Plan 2 via ScreenCaptureKit
        return new Rectangle(0, 0, 2560, 1440);
    }

    public static Rectangle GetActiveScreenBounds()
    {
        return GetScreenBounds();
    }
}
```

- [ ] **Step 3: Fix ClipboardHelpers.cs — replace WinForms Clipboard with macOS stub**

Open `ShareXMac.HelpersLib/Helpers/ClipboardHelpers.cs`. Remove the `using System.Windows.Forms;` import. Replace all `Clipboard.*` calls with `MacClipboard.*` and create a companion file:

Create `ShareXMac.HelpersLib/Helpers/MacClipboard.cs`:

```csharp
namespace ShareX.HelpersLib;

// Thin facade over macOS NSPasteboard via P/Invoke — filled in Plan 2.
// For now, clipboard operations are no-ops so the library compiles.
public static class MacClipboard
{
    public static void SetText(string text) { /* Plan 2 */ }
    public static string? GetText() => null;
    public static void SetImage(System.Drawing.Image image) { /* Plan 2 */ }
    public static System.Drawing.Image? GetImage() => null;
    public static bool ContainsText() => false;
    public static bool ContainsImage() => false;
}
```

In `ClipboardHelpers.cs`, replace every `Clipboard.SetText(` with `MacClipboard.SetText(`, `Clipboard.GetText(` with `MacClipboard.GetText(`, and so on for the image and format methods.

- [ ] **Step 4: Fix FileHelpers.cs — remove Shell32 P/Invoke**

Open `ShareXMac.HelpersLib/Helpers/FileHelpers.cs`. Find any `[DllImport("shell32.dll"` blocks and `SHFileOperation` calls. Delete those methods entirely — they handle Windows recycle-bin moves. Replace with a simple `File.Delete` call:

```csharp
public static bool MoveToRecycleBin(string filePath)
{
    // macOS: move to trash via Process call to AppleScript (Plan 2 adds proper NSFileManager)
    try { File.Delete(filePath); return true; }
    catch { return false; }
}
```

Remove `using System.Runtime.InteropServices;` from FileHelpers.cs if it is only used for the removed P/Invoke block.

- [ ] **Step 5: Fix Helpers.cs — remove Windows-specific API calls**

Open `ShareXMac.HelpersLib/Helpers/Helpers.cs`. Search for and remove/stub:
- Any `Microsoft.Win32` references (registry)
- Any `System.Windows.Forms` references
- Any `Environment.SpecialFolder.ApplicationData` → replace with macOS path:

```csharp
public static string GetPersonalFolder()
{
    return Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.Personal),
        "Library", "Application Support", "ShareX-Mac");
}
```

- [ ] **Step 6: Fix ImageHelpers.cs — ensure System.Drawing imports resolve**

Open `ShareXMac.HelpersLib/Helpers/ImageHelpers.cs`. The file uses `System.Drawing` which is available via the `System.Drawing.Common` package added in the csproj. No code changes needed unless the file imports `System.Windows.Forms` — remove that import if present.

- [ ] **Step 7: Fix GraphicsExtensions.cs — remove WinForms imports**

Open `ShareXMac.HelpersLib/Extensions/GraphicsExtensions.cs`. Remove `using System.Windows.Forms;` if present. Keep all `System.Drawing` usage — it resolves via the package.

- [ ] **Step 8: Run build and fix any remaining errors**

```bash
cd ~/Documents/Dev/Projects/ShareX-Mac
dotnet build ShareXMac.HelpersLib/ShareXMac.HelpersLib.csproj 2>&1 | grep "error CS"
```

Expected: zero errors. If errors remain, each one identifies a file and line — remove the offending Windows type reference and replace with a stub or `// macOS: see Plan 2` comment.

- [ ] **Step 9: Commit**

```bash
git add ShareXMac.HelpersLib/
git commit -m "feat: fix HelpersLib Windows dependencies — library builds on macOS"
```

---

### Task 5: Port HistoryLib

**Files:**
- Create: `ShareXMac.HistoryLib/` (non-UI files from `ShareX.HistoryLib/`)

- [ ] **Step 1: Copy source files**

```bash
SOURCE=~/Documents/Dev/Projects/ShareX-arm64/ShareX.HistoryLib
DEST=~/Documents/Dev/Projects/ShareX-Mac/ShareXMac.HistoryLib
cp -r $SOURCE/. $DEST/
rm $DEST/ShareX.HistoryLib.csproj
```

- [ ] **Step 2: Delete Windows UI and ImageListView files**

```bash
cd ~/Documents/Dev/Projects/ShareX-Mac/ShareXMac.HistoryLib
rm -rf Forms
rm -f HistoryImageListViewRenderer.cs  # Windows ImageListView WinForms renderer
rm -f HistoryItemManager_ContextMenu.cs # WinForms context menu
```

- [ ] **Step 3: Fix HistoryItemManager.cs — remove WinForms references**

Open `ShareXMac.HistoryLib/HistoryItemManager.cs`. Remove `using System.Windows.Forms;`. Any method that creates a WinForms context menu should be deleted — the Avalonia UI will provide its own context menu in Plan 3.

- [ ] **Step 4: Update Properties references**

Open each remaining `.cs` file. Replace any `using ShareX.HelpersLib;` namespace reference that is unchanged (should still match since we kept the original namespace). Fix any `Environment.SpecialFolder` path that points to Windows locations to use:

```csharp
Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal),
    "Library", "Application Support", "ShareX-Mac")
```

- [ ] **Step 5: Build and fix**

```bash
dotnet build ~/Documents/Dev/Projects/ShareX-Mac/ShareXMac.HistoryLib/ShareXMac.HistoryLib.csproj 2>&1 | grep "error CS"
```

Expected: zero errors. Fix any remaining errors by removing the Windows type usage.

- [ ] **Step 6: Commit**

```bash
cd ~/Documents/Dev/Projects/ShareX-Mac
git add ShareXMac.HistoryLib/
git commit -m "feat: port HistoryLib — builds on macOS"
```

---

### Task 6: Port ImageEffectsLib

**Files:**
- Create: `ShareXMac.ImageEffectsLib/` (non-UI files from `ShareX.ImageEffectsLib/`)

- [ ] **Step 1: Copy and delete Windows UI layer**

```bash
SOURCE=~/Documents/Dev/Projects/ShareX-arm64/ShareX.ImageEffectsLib
DEST=~/Documents/Dev/Projects/ShareX-Mac/ShareXMac.ImageEffectsLib
cp -r $SOURCE/. $DEST/
rm $DEST/ShareX.ImageEffectsLib.csproj
rm -rf $DEST/Forms
```

- [ ] **Step 2: Fix ImageEffectPreset.cs — remove WinForms import**

Open `ShareXMac.ImageEffectsLib/ImageEffectPreset.cs`. Remove `using System.Windows.Forms;` if present.

- [ ] **Step 3: Fix Drawings/DrawText.cs — remove WinForms font dialog**

Open `ShareXMac.ImageEffectsLib/Drawings/DrawText.cs`. Remove any `FontDialog` usage — font selection is handled by the Avalonia UI in Plan 3. Keep all `System.Drawing.Font` usage.

- [ ] **Step 4: Build and fix**

```bash
dotnet build ~/Documents/Dev/Projects/ShareX-Mac/ShareXMac.ImageEffectsLib/ShareXMac.ImageEffectsLib.csproj 2>&1 | grep "error CS"
```

Expected: zero errors.

- [ ] **Step 5: Commit**

```bash
cd ~/Documents/Dev/Projects/ShareX-Mac
git add ShareXMac.ImageEffectsLib/
git commit -m "feat: port ImageEffectsLib — builds on macOS"
```

---

### Task 7: Port IndexerLib

**Files:**
- Create: `ShareXMac.IndexerLib/` (all non-UI files from `ShareX.IndexerLib/`)

- [ ] **Step 1: Copy and delete Windows UI layer**

```bash
SOURCE=~/Documents/Dev/Projects/ShareX-arm64/ShareX.IndexerLib
DEST=~/Documents/Dev/Projects/ShareX-Mac/ShareXMac.IndexerLib
cp -r $SOURCE/. $DEST/
rm $DEST/ShareX.IndexerLib.csproj
rm -rf $DEST/Forms
```

- [ ] **Step 2: Build — expect near-zero errors (only 2 Windows files)**

```bash
dotnet build ~/Documents/Dev/Projects/ShareX-Mac/ShareXMac.IndexerLib/ShareXMac.IndexerLib.csproj 2>&1 | grep "error CS"
```

Fix any errors by removing the Windows-specific import and replacing with a stub.

- [ ] **Step 3: Commit**

```bash
cd ~/Documents/Dev/Projects/ShareX-Mac
git add ShareXMac.IndexerLib/
git commit -m "feat: port IndexerLib — builds on macOS"
```

---

### Task 8: Port MediaLib

**Files:**
- Create: `ShareXMac.MediaLib/` (all non-UI files from `ShareX.MediaLib/`)

- [ ] **Step 1: Copy and delete Windows UI layer**

```bash
SOURCE=~/Documents/Dev/Projects/ShareX-arm64/ShareX.MediaLib
DEST=~/Documents/Dev/Projects/ShareX-Mac/ShareXMac.MediaLib
cp -r $SOURCE/. $DEST/
rm $DEST/ShareX.MediaLib.csproj
rm -rf $DEST/Forms
```

- [ ] **Step 2: Fix FFmpegCLIManager.cs — update FFmpeg path resolution**

Open `ShareXMac.MediaLib/FFmpegCLIManager.cs` (lives in MediaLib or HelpersLib — check both). Find where FFmpeg binary path is resolved. Replace the Windows default path:

```csharp
// Old: Path.Combine(Application.StartupPath, "ffmpeg.exe")
// New:
public static string GetFFmpegPath()
{
    // Check app bundle first, then fall back to system FFmpeg via Homebrew
    string bundled = Path.Combine(AppContext.BaseDirectory, "ffmpeg");
    if (File.Exists(bundled)) return bundled;
    return "/opt/homebrew/bin/ffmpeg"; // standard Homebrew arm64 location
}
```

- [ ] **Step 3: Build and fix**

```bash
dotnet build ~/Documents/Dev/Projects/ShareX-Mac/ShareXMac.MediaLib/ShareXMac.MediaLib.csproj 2>&1 | grep "error CS"
```

Expected: zero errors.

- [ ] **Step 4: Commit**

```bash
cd ~/Documents/Dev/Projects/ShareX-Mac
git add ShareXMac.MediaLib/
git commit -m "feat: port MediaLib — builds on macOS"
```

---

### Task 9: Port UploadersLib

**Files:**
- Create: `ShareXMac.UploadersLib/` (all non-UI files from `ShareX.UploadersLib/`)

- [ ] **Step 1: Copy and delete Windows UI layer**

```bash
SOURCE=~/Documents/Dev/Projects/ShareX-arm64/ShareX.UploadersLib
DEST=~/Documents/Dev/Projects/ShareX-Mac/ShareXMac.UploadersLib
cp -r $SOURCE/. $DEST/
rm $DEST/ShareX.UploadersLib.csproj
rm -rf $DEST/Forms
```

- [ ] **Step 2: Copy OAuth callback HTML resource**

```bash
mkdir -p ~/Documents/Dev/Projects/ShareX-Mac/ShareXMac.UploadersLib/Resources
cp $SOURCE/Resources/OAuthCallbackPage.html \
   ~/Documents/Dev/Projects/ShareX-Mac/ShareXMac.UploadersLib/Resources/
```

- [ ] **Step 3: Fix OAuthListenerForm — replace with non-WinForms HTTP listener**

`OAuth/OAuthListenerForm.cs` and its `.Designer.cs` use WinForms. Delete both:

```bash
rm ~/Documents/Dev/Projects/ShareX-Mac/ShareXMac.UploadersLib/OAuth/OAuthListenerForm.cs
rm ~/Documents/Dev/Projects/ShareX-Mac/ShareXMac.UploadersLib/OAuth/OAuthListenerForm.Designer.cs
```

Create a replacement `OAuth/OAuthListener.cs` that uses a plain `HttpListener`:

```csharp
namespace ShareX.UploadersLib.OAuthBase;

public class OAuthListener
{
    private readonly string _redirectUri;

    public OAuthListener(string redirectUri)
    {
        _redirectUri = redirectUri;
    }

    public async Task<string?> WaitForCodeAsync(CancellationToken ct = default)
    {
        using var listener = new System.Net.HttpListener();
        listener.Prefixes.Add(_redirectUri.TrimEnd('/') + "/");
        listener.Start();

        var context = await listener.GetContextAsync().WaitAsync(ct);
        var code = context.Request.QueryString["code"];

        var buffer = "<html><body>Authorization complete. You may close this tab.</body></html>"u8.ToArray();
        context.Response.ContentLength64 = buffer.Length;
        await context.Response.OutputStream.WriteAsync(buffer, ct);
        context.Response.Close();
        listener.Stop();

        return code;
    }
}
```

Find all callers of `OAuthListenerForm` in the UploadersLib and replace with `OAuthListener`. Search: `grep -rn "OAuthListenerForm" ~/Documents/Dev/Projects/ShareX-Mac/ShareXMac.UploadersLib --include="*.cs"`

- [ ] **Step 4: Build and fix**

```bash
dotnet build ~/Documents/Dev/Projects/ShareX-Mac/ShareXMac.UploadersLib/ShareXMac.UploadersLib.csproj 2>&1 | grep "error CS"
```

Fix remaining errors (remove WinForms imports, stub any dialog that opened a WinForms form).

- [ ] **Step 5: Commit**

```bash
cd ~/Documents/Dev/Projects/ShareX-Mac
git add ShareXMac.UploadersLib/
git commit -m "feat: port UploadersLib — builds on macOS"
```

---

### Task 10: Create Avalonia app shell

**Files:**
- Create: `ShareXMac/Program.cs`
- Create: `ShareXMac/App.axaml`
- Create: `ShareXMac/App.axaml.cs`
- Create: `ShareXMac/Assets/icon.png` (placeholder — a 32x32 PNG)

- [ ] **Step 1: Write failing test — app shell has an AppViewModel**

Create `ShareXMac.Tests/AppViewModelTests.cs`:

```csharp
using ShareXMac.ViewModels;
using Xunit;

namespace ShareXMac.Tests;

public class AppViewModelTests
{
    [Fact]
    public void AppViewModel_InitializesWithExpectedTitle()
    {
        var vm = new AppViewModel();
        Assert.Equal("ShareX Mac", vm.Title);
    }
}
```

- [ ] **Step 2: Run test — expect failure**

```bash
cd ~/Documents/Dev/Projects/ShareX-Mac
dotnet test ShareXMac.Tests/ShareXMac.Tests.csproj 2>&1 | tail -5
```

Expected: compile error — `AppViewModel` does not exist yet.

- [ ] **Step 3: Create ViewModels directory and AppViewModel**

```bash
mkdir -p ~/Documents/Dev/Projects/ShareX-Mac/ShareXMac/ViewModels
```

Create `ShareXMac/ViewModels/AppViewModel.cs`:

```csharp
using CommunityToolkit.Mvvm.ComponentModel;

namespace ShareXMac.ViewModels;

public partial class AppViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = "ShareX Mac";
}
```

- [ ] **Step 4: Create Program.cs**

```csharp
// ShareXMac/Program.cs
using Avalonia;

namespace ShareXMac;

class Program
{
    [STAThread]
    public static void Main(string[] args) =>
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
```

- [ ] **Step 5: Create App.axaml**

```xml
<!-- ShareXMac/App.axaml -->
<Application xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="ShareXMac.App">
    <Application.Styles>
        <FluentTheme />
    </Application.Styles>
</Application>
```

- [ ] **Step 6: Create App.axaml.cs**

```csharp
// ShareXMac/App.axaml.cs
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ShareXMac.ViewModels;

namespace ShareXMac;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = Avalonia.Controls.ShutdownMode.OnExplicitShutdown;
        }
        base.OnFrameworkInitializationCompleted();
    }
}
```

- [ ] **Step 7: Add placeholder app icon**

```bash
# Create a 32x32 placeholder PNG using sips (macOS built-in)
mkdir -p ~/Documents/Dev/Projects/ShareX-Mac/ShareXMac/Assets
# Copy any existing PNG as placeholder
cp ~/Documents/Dev/Projects/ShareX-arm64/ShareX/Resources/ShareX_Icon.ico \
   ~/Documents/Dev/Projects/ShareX-Mac/ShareXMac/Assets/icon.ico 2>/dev/null || \
   touch ~/Documents/Dev/Projects/ShareX-Mac/ShareXMac/Assets/icon.ico
```

- [ ] **Step 8: Run test — expect pass**

```bash
dotnet test ShareXMac.Tests/ShareXMac.Tests.csproj -v minimal
```

Expected: `Passed! - 1 test`

- [ ] **Step 9: Commit**

```bash
cd ~/Documents/Dev/Projects/ShareX-Mac
git add ShareXMac/ ShareXMac.Tests/
git commit -m "feat: add Avalonia app shell and AppViewModel"
```

---

### Task 11: Add macOS menu bar (TrayIcon)

**Files:**
- Modify: `ShareXMac/App.axaml`
- Create: `ShareXMac/ViewModels/TrayViewModel.cs`
- Test: `ShareXMac.Tests/TrayViewModelTests.cs`

- [ ] **Step 1: Write failing test — TrayViewModel exposes capture commands**

Create `ShareXMac.Tests/TrayViewModelTests.cs`:

```csharp
using ShareXMac.ViewModels;
using Xunit;

namespace ShareXMac.Tests;

public class TrayViewModelTests
{
    [Fact]
    public void TrayViewModel_CaptureRegionCommand_IsNotNull()
    {
        var vm = new TrayViewModel();
        Assert.NotNull(vm.CaptureRegionCommand);
    }

    [Fact]
    public void TrayViewModel_QuitCommand_IsNotNull()
    {
        var vm = new TrayViewModel();
        Assert.NotNull(vm.QuitCommand);
    }
}
```

- [ ] **Step 2: Run test — expect failure**

```bash
dotnet test ShareXMac.Tests/ShareXMac.Tests.csproj 2>&1 | tail -5
```

Expected: compile error — `TrayViewModel` does not exist.

- [ ] **Step 3: Create TrayViewModel**

```csharp
// ShareXMac/ViewModels/TrayViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia;

namespace ShareXMac.ViewModels;

public partial class TrayViewModel : ObservableObject
{
    [RelayCommand]
    private void CaptureRegion() { /* wired to IScreenCapture in Plan 2 */ }

    [RelayCommand]
    private void CaptureWindow() { /* wired to IScreenCapture in Plan 2 */ }

    [RelayCommand]
    private void CaptureFullscreen() { /* wired to IScreenCapture in Plan 2 */ }

    [RelayCommand]
    private void RecordVideo() { /* wired to IScreenCapture in Plan 2 */ }

    [RelayCommand]
    private void RecordGif() { /* wired to IScreenCapture in Plan 2 */ }

    [RelayCommand]
    private void OpenSettings() { /* opens SettingsWindow in Plan 3 */ }

    [RelayCommand]
    private void OpenHistory() { /* opens HistoryWindow in Plan 3 */ }

    [RelayCommand]
    private static void Quit()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime app)
            app.Shutdown();
    }
}
```

- [ ] **Step 4: Add TrayIcon to App.axaml**

```xml
<!-- ShareXMac/App.axaml -->
<Application xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:ShareXMac.ViewModels"
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
                        <NativeMenuItem Header="Record Video"
                                        Command="{Binding RecordVideoCommand}" />
                        <NativeMenuItem Header="Record GIF"
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

- [ ] **Step 5: Wire TrayViewModel as DataContext in App.axaml.cs**

```csharp
// ShareXMac/App.axaml.cs
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ShareXMac.ViewModels;

namespace ShareXMac;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        DataContext = new TrayViewModel();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = Avalonia.Controls.ShutdownMode.OnExplicitShutdown;
        }
        base.OnFrameworkInitializationCompleted();
    }
}
```

- [ ] **Step 6: Run tests — expect pass**

```bash
dotnet test ShareXMac.Tests/ShareXMac.Tests.csproj -v minimal
```

Expected: `Passed! - 3 tests`

- [ ] **Step 7: Commit**

```bash
cd ~/Documents/Dev/Projects/ShareX-Mac
git add ShareXMac/ ShareXMac.Tests/
git commit -m "feat: add menu bar TrayIcon with capture and quit commands"
```

---

### Task 12: Platform interfaces and stubs

**Files:**
- Create: `ShareXMac/Platform/IScreenCapture.cs`
- Create: `ShareXMac/Platform/IHotkeyManager.cs`
- Create: `ShareXMac/Platform/INotificationService.cs`
- Create: `ShareXMac/Platform/StubScreenCapture.cs`
- Create: `ShareXMac/Platform/StubHotkeyManager.cs`
- Create: `ShareXMac/Platform/StubNotificationService.cs`
- Test: `ShareXMac.Tests/PlatformStubTests.cs`

- [ ] **Step 1: Write failing tests**

Create `ShareXMac.Tests/PlatformStubTests.cs`:

```csharp
using ShareXMac.Platform;
using Xunit;

namespace ShareXMac.Tests;

public class PlatformStubTests
{
    [Fact]
    public async Task StubScreenCapture_CaptureRegion_ReturnsNull()
    {
        IScreenCapture capture = new StubScreenCapture();
        var result = await capture.CaptureRegionAsync();
        Assert.Null(result);
    }

    [Fact]
    public async Task StubScreenCapture_RecognizeText_ReturnsNull()
    {
        IScreenCapture capture = new StubScreenCapture();
        var result = await capture.RecognizeTextAsync(Array.Empty<byte>());
        Assert.Null(result);
    }

    [Fact]
    public void StubHotkeyManager_IsAvailable_ReturnsFalse()
    {
        IHotkeyManager hotkeys = new StubHotkeyManager();
        Assert.False(hotkeys.IsAvailable);
    }

    [Fact]
    public async Task StubNotificationService_Show_DoesNotThrow()
    {
        INotificationService notifications = new StubNotificationService();
        await notifications.ShowAsync("Test", "Body");
    }
}
```

- [ ] **Step 2: Run tests — expect compile failure**

```bash
dotnet test ShareXMac.Tests/ShareXMac.Tests.csproj 2>&1 | tail -5
```

Expected: compile errors — `IScreenCapture`, `IHotkeyManager`, `INotificationService` not found.

- [ ] **Step 3: Create interface files**

```bash
mkdir -p ~/Documents/Dev/Projects/ShareX-Mac/ShareXMac/Platform
```

Create `ShareXMac/Platform/IScreenCapture.cs`:

```csharp
namespace ShareXMac.Platform;

public interface IScreenCapture
{
    Task<byte[]?> CaptureRegionAsync();
    Task<byte[]?> CaptureWindowAsync();
    Task<byte[]?> CaptureFullscreenAsync();
    Task StartRecordingAsync(string outputPath, RecordingFormat format);
    Task StopRecordingAsync();
    Task<string?> RecognizeTextAsync(byte[] imageData);
}

public enum RecordingFormat { MP4, GIF }
```

Create `ShareXMac/Platform/IHotkeyManager.cs`:

```csharp
namespace ShareXMac.Platform;

public interface IHotkeyManager
{
    bool IsAvailable { get; }
    void Register(string id, KeyCombo combo, Action callback);
    void Unregister(string id);
    void UnregisterAll();
}

public record KeyCombo(string Modifiers, string Key);
```

Create `ShareXMac/Platform/INotificationService.cs`:

```csharp
namespace ShareXMac.Platform;

public interface INotificationService
{
    Task ShowAsync(string title, string body);
}
```

- [ ] **Step 4: Create stub implementations**

Create `ShareXMac/Platform/StubScreenCapture.cs`:

```csharp
namespace ShareXMac.Platform;

public class StubScreenCapture : IScreenCapture
{
    public Task<byte[]?> CaptureRegionAsync() => Task.FromResult<byte[]?>(null);
    public Task<byte[]?> CaptureWindowAsync() => Task.FromResult<byte[]?>(null);
    public Task<byte[]?> CaptureFullscreenAsync() => Task.FromResult<byte[]?>(null);
    public Task StartRecordingAsync(string outputPath, RecordingFormat format) => Task.CompletedTask;
    public Task StopRecordingAsync() => Task.CompletedTask;
    public Task<string?> RecognizeTextAsync(byte[] imageData) => Task.FromResult<string?>(null);
}
```

Create `ShareXMac/Platform/StubHotkeyManager.cs`:

```csharp
namespace ShareXMac.Platform;

public class StubHotkeyManager : IHotkeyManager
{
    public bool IsAvailable => false;
    public void Register(string id, KeyCombo combo, Action callback) { }
    public void Unregister(string id) { }
    public void UnregisterAll() { }
}
```

Create `ShareXMac/Platform/StubNotificationService.cs`:

```csharp
namespace ShareXMac.Platform;

public class StubNotificationService : INotificationService
{
    public Task ShowAsync(string title, string body) => Task.CompletedTask;
}
```

- [ ] **Step 5: Run tests — expect pass**

```bash
dotnet test ShareXMac.Tests/ShareXMac.Tests.csproj -v minimal
```

Expected: `Passed! - 7 tests`

- [ ] **Step 6: Commit**

```bash
cd ~/Documents/Dev/Projects/ShareX-Mac
git add ShareXMac/Platform/ ShareXMac.Tests/
git commit -m "feat: add platform interfaces and stub implementations"
```

---

### Task 13: Full solution build verification

- [ ] **Step 1: Build entire solution**

```bash
cd ~/Documents/Dev/Projects/ShareX-Mac
dotnet build ShareX-Mac.sln 2>&1 | tail -10
```

Expected: `Build succeeded.` with 0 errors. Warnings are acceptable.

- [ ] **Step 2: Run all tests**

```bash
dotnet test ShareX-Mac.sln -v minimal
```

Expected: `Passed! - 7 tests`

- [ ] **Step 3: Verify app launches (menu bar icon appears)**

```bash
dotnet run --project ShareXMac/ShareXMac.csproj
```

Expected: app starts with no crash, menu bar icon appears in the macOS status bar. Click it — all 9 menu items are visible. Quit terminates the process cleanly.

- [ ] **Step 4: Final commit**

```bash
cd ~/Documents/Dev/Projects/ShareX-Mac
git add .
git commit -m "feat: Plan 1 complete — all libraries build, Avalonia shell launches on macOS"
```

---

## What comes next

- **Plan 2 — Screen Capture Layer:** Implement `MacScreenCapture` (ScreenCaptureKit P/Invoke), `MacHotkeyManager` (CGEvent), `MacNotificationService` (UserNotifications), wire into `TrayViewModel` commands.
- **Plan 3 — UI:** Post-capture toolbar, annotation canvas, Settings window, History window.
- **Plan 4 — Distribution:** GitHub Actions CI, code signing, notarization, `.dmg` packaging, Homebrew cask.
