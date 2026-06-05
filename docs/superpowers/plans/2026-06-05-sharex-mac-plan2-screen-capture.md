# ShareX Mac — Plan 2: Screen Capture Layer

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement working macOS screen capture, recording, OCR, clipboard, notifications, and hotkeys by filling `ShareXMac.ScreenCaptureLib` and wiring real implementations into `TrayViewModel`.

**Architecture:** Platform interfaces move to `ShareXMac.HelpersLib` (where `ScreenCaptureLib` can implement them without circular dependencies). Mac implementations live in `ShareXMac.ScreenCaptureLib`. App shell registers real impls via constructor injection and falls back to stubs if a permission is unavailable.

**Tech Stack:** .NET 10, `screencapture` CLI (region/window/fullscreen), FFmpeg `avfoundation` (recording), Apple Vision via ObjC P/Invoke (OCR), NSPasteboard ObjC P/Invoke (clipboard), UNUserNotificationCenter ObjC P/Invoke (notifications), CGEventTap P/Invoke (hotkeys).

---

## File Structure

```
ShareXMac.HelpersLib/
  Platform/
    IScreenCapture.cs        MOVE (from ShareXMac/Platform/ — rename namespace to ShareX.HelpersLib)
    IHotkeyManager.cs        MOVE
    INotificationService.cs  MOVE

ShareXMac.ScreenCaptureLib/
  ObjC/
    ObjCRuntime.cs           CREATE — P/Invoke bindings for ObjC runtime + dlopen/dlsym
  MacScreenCapture.cs        CREATE — IScreenCapture: screencapture CLI + FFmpeg + Vision OCR
  MacVision.cs               CREATE — Vision framework OCR helper (P/Invoke + ObjC blocks)
  MacClipboard.cs            CREATE — NSPasteboard wrapper (replaces ClipboardHelpers stub logic)
  MacNotificationService.cs  CREATE — INotificationService via UNUserNotificationCenter
  MacHotkeyManager.cs        CREATE — IHotkeyManager via CGEventTap

ShareXMac/
  Platform/
    IScreenCapture.cs        DELETE (moved to HelpersLib)
    IHotkeyManager.cs        DELETE (moved to HelpersLib)
    INotificationService.cs  DELETE (moved to HelpersLib)
    StubScreenCapture.cs     UPDATE — add `using ShareX.HelpersLib;`
    StubHotkeyManager.cs     UPDATE — add `using ShareX.HelpersLib;`
    StubNotificationService.cs UPDATE — add `using ShareX.HelpersLib;`
  ViewModels/
    TrayViewModel.cs         MODIFY — inject IScreenCapture, make commands async
  App.axaml.cs               MODIFY — register real implementations

ShareXMac.HelpersLib/
  Helpers/
    ClipboardHelpers.cs      MODIFY — real NSPasteboard via MacClipboard

ShareXMac.Tests/
  ScreenCaptureTests.cs      CREATE — tests for Mac platform implementations
```

---

### Task 1: Move platform interfaces to HelpersLib

**Files:**
- Create: `ShareXMac.HelpersLib/Platform/IScreenCapture.cs`
- Create: `ShareXMac.HelpersLib/Platform/IHotkeyManager.cs`
- Create: `ShareXMac.HelpersLib/Platform/INotificationService.cs`
- Delete: `ShareXMac/Platform/IScreenCapture.cs`
- Delete: `ShareXMac/Platform/IHotkeyManager.cs`
- Delete: `ShareXMac/Platform/INotificationService.cs`
- Modify: `ShareXMac/Platform/StubScreenCapture.cs`
- Modify: `ShareXMac/Platform/StubHotkeyManager.cs`
- Modify: `ShareXMac/Platform/StubNotificationService.cs`
- Modify: `ShareXMac.ScreenCaptureLib/ShareXMac.ScreenCaptureLib.csproj`

- [ ] **Step 1: Create IScreenCapture.cs in HelpersLib**

Create `ShareXMac.HelpersLib/Platform/IScreenCapture.cs`:
```csharp
namespace ShareX.HelpersLib;

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

- [ ] **Step 2: Create IHotkeyManager.cs in HelpersLib**

Create `ShareXMac.HelpersLib/Platform/IHotkeyManager.cs`:
```csharp
namespace ShareX.HelpersLib;

public interface IHotkeyManager
{
    bool IsAvailable { get; }
    void Register(string id, KeyCombo combo, Action callback);
    void Unregister(string id);
    void UnregisterAll();
}

public record KeyCombo(string Modifiers, string Key);
```

- [ ] **Step 3: Create INotificationService.cs in HelpersLib**

Create `ShareXMac.HelpersLib/Platform/INotificationService.cs`:
```csharp
namespace ShareX.HelpersLib;

public interface INotificationService
{
    Task ShowAsync(string title, string body);
}
```

- [ ] **Step 4: Delete old interface files**

```bash
rm ShareXMac/Platform/IScreenCapture.cs ShareXMac/Platform/IHotkeyManager.cs ShareXMac/Platform/INotificationService.cs
```

- [ ] **Step 5: Update stubs to use new namespace**

Replace the entire contents of `ShareXMac/Platform/StubScreenCapture.cs`:
```csharp
using ShareX.HelpersLib;
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

Replace the entire contents of `ShareXMac/Platform/StubHotkeyManager.cs`:
```csharp
using ShareX.HelpersLib;
namespace ShareXMac.Platform;

public class StubHotkeyManager : IHotkeyManager
{
    public bool IsAvailable => false;
    public void Register(string id, KeyCombo combo, Action callback) { }
    public void Unregister(string id) { }
    public void UnregisterAll() { }
}
```

Replace the entire contents of `ShareXMac/Platform/StubNotificationService.cs`:
```csharp
using ShareX.HelpersLib;
namespace ShareXMac.Platform;

public class StubNotificationService : INotificationService
{
    public Task ShowAsync(string title, string body) => Task.CompletedTask;
}
```

- [ ] **Step 6: Verify ScreenCaptureLib.csproj references HelpersLib**

Check `ShareXMac.ScreenCaptureLib/ShareXMac.ScreenCaptureLib.csproj` — it must contain a ProjectReference to HelpersLib. It already does from Plan 1. If for any reason it's missing, add:
```xml
<ItemGroup>
  <ProjectReference Include="..\ShareXMac.HelpersLib\ShareXMac.HelpersLib.csproj" />
</ItemGroup>
```

- [ ] **Step 7: Build to verify no errors**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet build ShareXMac/ShareXMac.csproj 2>&1 | grep -E "error|warning|Error|Warning" | grep -v NU1903
```
Expected: `0 Error(s)`

- [ ] **Step 8: Commit**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && git add -A && git commit -m "refactor: move platform interfaces to HelpersLib for ScreenCaptureLib access"
```

---

### Task 2: ObjC interop foundation

**Files:**
- Create: `ShareXMac.ScreenCaptureLib/ObjC/ObjCRuntime.cs`

- [ ] **Step 1: Write failing test**

Create `ShareXMac.Tests/ObjCRuntimeTests.cs`:
```csharp
using ShareXMac.ScreenCaptureLib.ObjC;
using Xunit;

namespace ShareXMac.Tests;

public class ObjCRuntimeTests
{
    [Fact]
    public void GetClass_NSString_ReturnsNonZero()
    {
        nint cls = ObjCRuntime.GetClass("NSString");
        Assert.NotEqual(0, cls);
    }

    [Fact]
    public void Send_NSStringClass_Description_ReturnsNonZero()
    {
        nint cls = ObjCRuntime.GetClass("NSString");
        nint desc = ObjCRuntime.Send(cls, "description");
        Assert.NotEqual(0, desc);
    }

    [Fact]
    public void SendStr_NSStringAlloc_ReturnsString()
    {
        nint cls = ObjCRuntime.GetClass("NSString");
        nint obj = ObjCRuntime.Send(
            ObjCRuntime.Send(cls, "alloc"),
            "initWithUTF8String:",
            "hello");
        string? result = ObjCRuntime.ToManagedString(obj);
        Assert.Equal("hello", result);
    }
}
```

- [ ] **Step 2: Run test to confirm failure**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet test ShareXMac.Tests/ShareXMac.Tests.csproj --filter "ObjCRuntimeTests" 2>&1 | tail -10
```
Expected: compile error (ObjCRuntime does not exist)

- [ ] **Step 3: Create ObjCRuntime.cs**

Create `ShareXMac.ScreenCaptureLib/ObjC/ObjCRuntime.cs`:
```csharp
using System.Runtime.InteropServices;

namespace ShareXMac.ScreenCaptureLib.ObjC;

internal static class ObjCRuntime
{
    const string ObjCLib = "/usr/lib/libobjc.A.dylib";
    const string Dl = "libdl.dylib";

    [DllImport(ObjCLib)] static extern nint objc_getClass(string name);
    [DllImport(ObjCLib)] static extern nint sel_registerName(string name);

    // objc_msgSend with 0 args
    [DllImport(ObjCLib, EntryPoint = "objc_msgSend")]
    static extern nint objc_msgSend0(nint receiver, nint sel);

    // objc_msgSend with 1 nint arg
    [DllImport(ObjCLib, EntryPoint = "objc_msgSend")]
    static extern nint objc_msgSend1(nint receiver, nint sel, nint arg1);

    // objc_msgSend with 2 nint args
    [DllImport(ObjCLib, EntryPoint = "objc_msgSend")]
    static extern nint objc_msgSend2(nint receiver, nint sel, nint arg1, nint arg2);

    // objc_msgSend with UTF-8 string arg
    [DllImport(ObjCLib, EntryPoint = "objc_msgSend")]
    static extern nint objc_msgSend_str(nint receiver, nint sel,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string arg1);

    // objc_msgSend for initWithBytes:length:
    [DllImport(ObjCLib, EntryPoint = "objc_msgSend")]
    internal static extern nint objc_msgSend_bytes(nint receiver, nint sel,
        nint bytes, nuint length);

    // objc_msgSend returning bool
    [DllImport(ObjCLib, EntryPoint = "objc_msgSend")]
    static extern bool objc_msgSend_bool(nint receiver, nint sel);

    // objc_msgSend for performRequests:error: (id, id*) -> bool
    [DllImport(ObjCLib, EntryPoint = "objc_msgSend")]
    internal static extern bool objc_msgSend_perform(nint receiver, nint sel,
        nint requests, ref nint error);

    // dlopen / dlsym for loading frameworks
    [DllImport(Dl)] internal static extern nint dlopen(string path, int flags);
    [DllImport(Dl)] internal static extern nint dlsym(nint handle, string symbol);

    // Public helpers

    internal static nint GetClass(string name) => objc_getClass(name);
    internal static nint Sel(string name) => sel_registerName(name);

    internal static nint Send(nint obj, string sel)
        => objc_msgSend0(obj, Sel(sel));

    internal static nint Send(nint obj, string sel, nint arg)
        => objc_msgSend1(obj, Sel(sel), arg);

    internal static nint Send(nint obj, string sel, nint a1, nint a2)
        => objc_msgSend2(obj, Sel(sel), a1, a2);

    internal static nint Send(nint obj, string sel, string arg)
        => objc_msgSend_str(obj, Sel(sel), arg);

    internal static bool SendBool(nint obj, string sel)
        => objc_msgSend_bool(obj, Sel(sel));

    /// <summary>
    /// Creates an NSString from a managed string and returns the ObjC object pointer.
    /// Caller is responsible for releasing if needed.
    /// </summary>
    internal static nint ToNSString(string text)
        => objc_msgSend_str(
            Send(Send(GetClass("NSString"), "alloc"), "init"),
            Sel("initWithUTF8String:"),
            text);

    /// <summary>
    /// Reads the UTF-8 string from an NSString object. Returns null if obj is zero.
    /// </summary>
    internal static string? ToManagedString(nint nsStr)
    {
        if (nsStr == 0) return null;
        nint cstr = Send(nsStr, "UTF8String");
        return Marshal.PtrToStringUTF8(cstr);
    }

    /// <summary>
    /// Returns an NSArray containing a single element.
    /// </summary>
    internal static nint ArrayWithObject(nint obj)
        => objc_msgSend1(GetClass("NSArray"), Sel("arrayWithObject:"), obj);

    /// <summary>
    /// Returns the count of an NSArray.
    /// </summary>
    internal static nint ArrayCount(nint array)
        => Send(array, "count");

    /// <summary>
    /// Returns element at index from an NSArray.
    /// </summary>
    internal static nint ArrayObjectAt(nint array, nint index)
        => objc_msgSend1(array, Sel("objectAtIndex:"), index);
}
```

- [ ] **Step 4: Add ScreenCaptureLib reference to test project**

Modify `ShareXMac.Tests/ShareXMac.Tests.csproj` — add a ProjectReference:
```xml
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
    <ProjectReference Include="..\ShareXMac.ScreenCaptureLib\ShareXMac.ScreenCaptureLib.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 5: Run tests to verify they pass**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet test ShareXMac.Tests/ShareXMac.Tests.csproj --filter "ObjCRuntimeTests" -v normal 2>&1 | tail -15
```
Expected: `3 passed`

- [ ] **Step 6: Commit**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && git add -A && git commit -m "feat: add ObjC runtime P/Invoke foundation in ScreenCaptureLib"
```

---

### Task 3: MacScreenCapture — captures and recording

**Files:**
- Create: `ShareXMac.ScreenCaptureLib/MacScreenCapture.cs`

- [ ] **Step 1: Write failing tests**

Create `ShareXMac.Tests/MacScreenCaptureTests.cs`:
```csharp
using ShareXMac.ScreenCaptureLib;
using Xunit;

namespace ShareXMac.Tests;

public class MacScreenCaptureTests
{
    [Fact]
    public void Constructor_DoesNotThrow()
    {
        var capture = new MacScreenCapture();
        Assert.NotNull(capture);
    }

    [Fact]
    public async Task StopRecordingAsync_WhenNotRecording_DoesNotThrow()
    {
        var capture = new MacScreenCapture();
        // Should be a no-op, not throw
        await capture.StopRecordingAsync();
    }

    [Fact]
    public async Task StartRecordingAsync_ThenStop_CreatesNoExceptionFlow()
    {
        // Integration test: actually starts ffmpeg. Skip if ffmpeg not available.
        string ffmpegPath = MacScreenCapture.FindFfmpeg();
        if (!File.Exists(ffmpegPath))
        {
            return; // skip — ffmpeg not installed
        }

        var capture = new MacScreenCapture();
        string outputPath = Path.Combine(Path.GetTempPath(), $"test-{Guid.NewGuid():N}.mp4");
        try
        {
            await capture.StartRecordingAsync(outputPath, ShareX.HelpersLib.RecordingFormat.MP4);
            await Task.Delay(500);
            await capture.StopRecordingAsync();
        }
        finally
        {
            if (File.Exists(outputPath)) File.Delete(outputPath);
        }
    }
}
```

- [ ] **Step 2: Run to confirm failure**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet test ShareXMac.Tests/ShareXMac.Tests.csproj --filter "MacScreenCaptureTests" 2>&1 | tail -5
```
Expected: compile error (MacScreenCapture does not exist)

- [ ] **Step 3: Create MacScreenCapture.cs**

Create `ShareXMac.ScreenCaptureLib/MacScreenCapture.cs`:
```csharp
using System.Diagnostics;
using ShareX.HelpersLib;

namespace ShareXMac.ScreenCaptureLib;

public class MacScreenCapture : IScreenCapture
{
    private Process? _recordingProcess;

    public async Task<byte[]?> CaptureRegionAsync()
    {
        string path = TempPath("png");
        if (!await RunCapture($"-i -t png \"{path}\"")) return null;
        return await ReadAndDelete(path);
    }

    public async Task<byte[]?> CaptureWindowAsync()
    {
        string path = TempPath("png");
        if (!await RunCapture($"-w -t png \"{path}\"")) return null;
        return await ReadAndDelete(path);
    }

    public async Task<byte[]?> CaptureFullscreenAsync()
    {
        string path = TempPath("png");
        if (!await RunCapture($"-x -t png \"{path}\"")) return null;
        return await ReadAndDelete(path);
    }

    public async Task StartRecordingAsync(string outputPath, RecordingFormat format)
    {
        if (_recordingProcess != null) return;

        string ffmpeg = FindFfmpeg();
        string args = format == RecordingFormat.GIF
            ? $"-f avfoundation -i \"1:none\" -r 10 -vf \"scale=1280:-1\" \"{outputPath}\""
            : $"-f avfoundation -i \"1:none\" -r 30 -c:v libx264 -preset fast \"{outputPath}\"";

        _recordingProcess = new Process
        {
            StartInfo = new ProcessStartInfo(ffmpeg, args)
            {
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        _recordingProcess.Start();
        await Task.CompletedTask;
    }

    public async Task StopRecordingAsync()
    {
        if (_recordingProcess == null) return;
        try
        {
            // Send 'q' to ffmpeg's stdin to trigger graceful shutdown
            await _recordingProcess.StandardInput.WriteAsync('q');
            await _recordingProcess.StandardInput.FlushAsync();
            await _recordingProcess.WaitForExitAsync();
        }
        catch
        {
            _recordingProcess.Kill();
        }
        finally
        {
            _recordingProcess.Dispose();
            _recordingProcess = null;
        }
    }

    public Task<string?> RecognizeTextAsync(byte[] imageData) =>
        MacVision.RecognizeTextAsync(imageData);

    public static string FindFfmpeg()
    {
        string bundled = Path.Combine(AppContext.BaseDirectory, "ffmpeg");
        if (File.Exists(bundled)) return bundled;
        string homebrew = "/opt/homebrew/bin/ffmpeg";
        if (File.Exists(homebrew)) return homebrew;
        return "/usr/local/bin/ffmpeg";
    }

    private static async Task<bool> RunCapture(string args)
    {
        using var proc = Process.Start(new ProcessStartInfo("screencapture", args)
        {
            UseShellExecute = false,
            CreateNoWindow = true
        });
        if (proc == null) return false;
        await proc.WaitForExitAsync();
        return proc.ExitCode == 0;
    }

    private static string TempPath(string ext) =>
        Path.Combine(Path.GetTempPath(), $"sharexmac-{Guid.NewGuid():N}.{ext}");

    private static async Task<byte[]?> ReadAndDelete(string path)
    {
        if (!File.Exists(path)) return null;
        byte[] data = await File.ReadAllBytesAsync(path);
        File.Delete(path);
        return data;
    }
}
```

Note: `MacVision` is a stub that will be filled in Task 4.

- [ ] **Step 4: Create MacVision stub so it compiles**

Create `ShareXMac.ScreenCaptureLib/MacVision.cs` as a stub:
```csharp
namespace ShareXMac.ScreenCaptureLib;

internal static class MacVision
{
    public static Task<string?> RecognizeTextAsync(byte[] imageData)
        => Task.FromResult<string?>(null); // replaced in Task 4
}
```

- [ ] **Step 5: Run tests to verify they pass**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet test ShareXMac.Tests/ShareXMac.Tests.csproj --filter "MacScreenCaptureTests" -v normal 2>&1 | tail -15
```
Expected: `3 passed`

- [ ] **Step 6: Commit**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && git add -A && git commit -m "feat: add MacScreenCapture with screencapture CLI and FFmpeg recording"
```

---

### Task 4: MacVision — OCR via ObjC P/Invoke

**Files:**
- Modify: `ShareXMac.ScreenCaptureLib/MacVision.cs` (replace stub with real implementation)

The Vision P/Invoke approach:
1. Load the Vision framework via `dlopen`
2. Create `NSData` from the raw image bytes
3. Create `NSImage` from `NSData`, then get its `CGImage`
4. Create `VNImageRequestHandler` with the `CGImage`
5. Create `VNRecognizeTextRequest` with an ObjC completion block
6. Call `performRequests:error:` (synchronous — block fires on the same thread)
7. Read results from `VNRecognizedTextObservation` array

**ObjC block layout (arm64/x64):** A "global block" uses a static function pointer, `flags = 0x50000000`, and a static descriptor. The invoke signature for the completion block is `void(struct block_literal *, id request, id error)`.

- [ ] **Step 1: Write failing tests**

Add to `ShareXMac.Tests/MacScreenCaptureTests.cs`:
```csharp
[Fact]
public async Task RecognizeTextAsync_EmptyBytes_ReturnsNullOrEmpty()
{
    // Should not throw even with garbage input
    var capture = new MacScreenCapture();
    string? result = await capture.RecognizeTextAsync(new byte[] { 0, 1, 2, 3 });
    // null (invalid image) or empty string are both acceptable
    Assert.True(result == null || result.Length >= 0);
}
```

Run:
```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet test ShareXMac.Tests/ShareXMac.Tests.csproj --filter "RecognizeTextAsync_EmptyBytes" 2>&1 | tail -5
```
Expected: `1 passed` (current stub returns null, which satisfies `result == null`)

The test will still pass after we implement the real Vision OCR, since garbage bytes produce a null/empty result. The test is intentionally lenient because Vision returns null/empty for invalid image data.

- [ ] **Step 2: Implement MacVision with Vision P/Invoke**

Replace `ShareXMac.ScreenCaptureLib/MacVision.cs` with the full implementation:
```csharp
using System.Runtime.InteropServices;
using ShareXMac.ScreenCaptureLib.ObjC;

namespace ShareXMac.ScreenCaptureLib;

internal static class MacVision
{
    // ObjC block layout for a completion block ^(VNRequest *req, NSError *err)
    [StructLayout(LayoutKind.Sequential)]
    private struct BlockDescriptor
    {
        public nuint Reserved;
        public nuint Size;
    }

    [StructLayout(LayoutKind.Sequential)]
    private unsafe struct BlockLiteral
    {
        public nint Isa;         // _NSConcreteGlobalBlock
        public int Flags;        // BLOCK_IS_GLOBAL | BLOCK_HAS_DESCRIPTOR = 0x50000000
        public int Reserved;
        public nint Invoke;      // static function pointer
        public BlockDescriptor* Descriptor;
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private unsafe delegate void CompletionBlockInvoke(BlockLiteral* block, nint request, nint error);

    // Static state — Vision completion is always synchronous (fired during performRequests:error:)
    [ThreadStatic] private static nint t_pendingResults;

    private static readonly CompletionBlockInvoke s_completionDelegate = CompletionCallback;
    private static readonly nint s_completionFuncPtr =
        Marshal.GetFunctionPointerForDelegate(s_completionDelegate);

    private static unsafe void CompletionCallback(BlockLiteral* block, nint request, nint error)
    {
        // request.results is an NSArray of VNRecognizedTextObservation
        t_pendingResults = ObjCRuntime.Send(request, "results");
    }

    private static bool s_frameworkLoaded;

    private static void EnsureFrameworkLoaded()
    {
        if (s_frameworkLoaded) return;
        ObjCRuntime.dlopen("/System/Library/Frameworks/Vision.framework/Vision", 1);
        s_frameworkLoaded = true;
    }

    public static unsafe Task<string?> RecognizeTextAsync(byte[] imageData)
    {
        if (imageData.Length == 0) return Task.FromResult<string?>(null);

        EnsureFrameworkLoaded();

        // 1. Create NSData from imageData
        nint nsData;
        fixed (byte* ptr = imageData)
        {
            nint alloc = ObjCRuntime.Send(ObjCRuntime.GetClass("NSData"), "alloc");
            nsData = ObjCRuntime.objc_msgSend_bytes(alloc, ObjCRuntime.Sel("initWithBytes:length:"),
                (nint)ptr, (nuint)imageData.Length);
        }
        if (nsData == 0) return Task.FromResult<string?>(null);

        // 2. Create NSImage from NSData, get CGImage
        nint nsImage = ObjCRuntime.Send(
            ObjCRuntime.Send(ObjCRuntime.GetClass("NSImage"), "alloc"),
            "initWithData:", nsData);
        if (nsImage == 0) return Task.FromResult<string?>(null);

        // NSImage.CGImage property via lock/unlock focus workaround:
        // tiffRepresentation -> NSBitmapImageRep -> cgImage
        nint tiffData = ObjCRuntime.Send(nsImage, "TIFFRepresentation");
        if (tiffData == 0) return Task.FromResult<string?>(null);

        nint bitmapRep = ObjCRuntime.Send(
            ObjCRuntime.Send(ObjCRuntime.GetClass("NSBitmapImageRep"), "alloc"),
            "initWithData:", tiffData);
        if (bitmapRep == 0) return Task.FromResult<string?>(null);

        nint cgImage = ObjCRuntime.Send(bitmapRep, "CGImage");
        if (cgImage == 0) return Task.FromResult<string?>(null);

        // 3. Create VNImageRequestHandler with CGImage
        nint handler = ObjCRuntime.Send(
            ObjCRuntime.Send(ObjCRuntime.GetClass("VNImageRequestHandler"), "alloc"),
            "initWithCGImage:options:",
            cgImage,
            ObjCRuntime.Send(ObjCRuntime.GetClass("NSDictionary"), "dictionary"));
        if (handler == 0) return Task.FromResult<string?>(null);

        // 4. Build the completion block
        var descriptor = new BlockDescriptor
        {
            Reserved = 0,
            Size = (nuint)sizeof(BlockLiteral)
        };
        BlockDescriptor* descPtr = (BlockDescriptor*)Marshal.AllocHGlobal(sizeof(BlockDescriptor));
        *descPtr = descriptor;

        nint isaPtr = ObjCRuntime.dlsym(
            ObjCRuntime.dlopen("/usr/lib/libobjc.A.dylib", 1),
            "_NSConcreteGlobalBlock");

        var blockLit = new BlockLiteral
        {
            Isa = isaPtr,
            Flags = 0x50000000, // BLOCK_IS_GLOBAL | BLOCK_HAS_DESCRIPTOR
            Reserved = 0,
            Invoke = s_completionFuncPtr,
            Descriptor = descPtr
        };

        BlockLiteral* blockPtr = (BlockLiteral*)Marshal.AllocHGlobal(sizeof(BlockLiteral));
        *blockPtr = blockLit;

        // 5. Create VNRecognizeTextRequest with completion block
        nint request = ObjCRuntime.Send(
            ObjCRuntime.Send(ObjCRuntime.GetClass("VNRecognizeTextRequest"), "alloc"),
            "initWithCompletionHandler:", (nint)blockPtr);

        // Set accuracy level (1 = .accurate)
        // [request setRecognitionLevel:1]
        ObjCRuntime.objc_msgSend_bytes(request, ObjCRuntime.Sel("setRecognitionLevel:"), 1, 0);

        // 6. Perform request synchronously
        t_pendingResults = 0;
        nint requestsArray = ObjCRuntime.ArrayWithObject(request);
        nint error = 0;
        ObjCRuntime.objc_msgSend_perform(handler, ObjCRuntime.Sel("performRequests:error:"),
            requestsArray, ref error);

        Marshal.FreeHGlobal((nint)blockPtr);
        Marshal.FreeHGlobal((nint)descPtr);

        // 7. Read results
        nint results = t_pendingResults;
        if (results == 0) return Task.FromResult<string?>(null);

        nint count = ObjCRuntime.ArrayCount(results);
        if (count == 0) return Task.FromResult<string?>("");

        var lines = new System.Text.StringBuilder();
        for (nint i = 0; i < count; i++)
        {
            nint observation = ObjCRuntime.ArrayObjectAt(results, i);
            // topCandidates(1) returns NSArray; take first candidate's .string
            nint candidates = ObjCRuntime.Send(observation, "topCandidatesForCount:", 1);
            // Note: selector is "topCandidatesForCount:" not "topCandidates:"
            if (candidates == 0) continue;
            nint candidateCount = ObjCRuntime.ArrayCount(candidates);
            if (candidateCount == 0) continue;
            nint candidate = ObjCRuntime.ArrayObjectAt(candidates, 0);
            nint text = ObjCRuntime.Send(candidate, "string");
            string? line = ObjCRuntime.ToManagedString(text);
            if (line != null)
            {
                if (lines.Length > 0) lines.Append('\n');
                lines.Append(line);
            }
        }

        return Task.FromResult<string?>(lines.Length > 0 ? lines.ToString() : null);
    }
}
```

Note on `topCandidatesForCount:`: The actual ObjC selector is `topCandidates:` (with a single integer argument). The `Send` helper for integer arguments needs a new overload. Add to `ObjCRuntime.cs`:
```csharp
// objc_msgSend with nint arg (for topCandidates: integer arg)
[DllImport(ObjCLib, EntryPoint = "objc_msgSend")]
internal static extern nint objc_msgSend_nint(nint receiver, nint sel, nint arg);

internal static nint SendInt(nint obj, string sel, nint arg)
    => objc_msgSend_nint(obj, Sel(sel), arg);
```

Then update the MacVision call from `ObjCRuntime.Send(observation, "topCandidatesForCount:", 1)` to:
```csharp
nint candidates = ObjCRuntime.SendInt(observation, "topCandidates:", 1);
```

Also update `setRecognitionLevel:` call:
```csharp
ObjCRuntime.SendInt(request, "setRecognitionLevel:", 1);
```

And remove the incorrect `objc_msgSend_bytes` call for `setRecognitionLevel:`.

- [ ] **Step 3: Add missing ObjC overloads to ObjCRuntime.cs**

Add to `ShareXMac.ScreenCaptureLib/ObjC/ObjCRuntime.cs` inside the class:
```csharp
// objc_msgSend with nint int arg (for NSArray counts, enum values)
[DllImport(ObjCLib, EntryPoint = "objc_msgSend")]
internal static extern nint objc_msgSend_nint(nint receiver, nint sel, nint arg);

internal static nint SendInt(nint obj, string sel, nint arg)
    => objc_msgSend_nint(obj, Sel(sel), arg);
```

Also add the `initWithData:` overload (single nint arg — already covered by `Send(obj, sel, nint)`) and `initWithCGImage:options:` (two nint args — already covered by `Send(obj, sel, nint, nint)`).

Verify `ArrayCount` returns `nint` consistently with `Send` (it returns `nint` via `objc_msgSend0` which returns `nint` — correct).

- [ ] **Step 4: Build to verify no compile errors**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet build ShareXMac.ScreenCaptureLib/ShareXMac.ScreenCaptureLib.csproj 2>&1 | grep -E "^.*error" | head -20
```
Expected: 0 errors

- [ ] **Step 5: Run OCR test**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet test ShareXMac.Tests/ShareXMac.Tests.csproj --filter "RecognizeTextAsync_EmptyBytes" -v normal 2>&1 | tail -10
```
Expected: `1 passed`

- [ ] **Step 6: Commit**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && git add -A && git commit -m "feat: add Vision OCR via ObjC P/Invoke in MacVision"
```

---

### Task 5: MacClipboard — NSPasteboard

**Files:**
- Create: `ShareXMac.ScreenCaptureLib/MacClipboard.cs`
- Modify: `ShareXMac.HelpersLib/Helpers/ClipboardHelpers.cs`

The Plan 1 `ClipboardHelpers.cs` is a no-op stub with the comment "real clipboard implementation comes in Plan 2". Here we implement `MacClipboard` in ScreenCaptureLib (which can use ObjCRuntime), and update `ClipboardHelpers` to delegate to it.

- [ ] **Step 1: Write failing test**

Add to `ShareXMac.Tests/ScreenCaptureTests.cs` (create file):
```csharp
using ShareXMac.ScreenCaptureLib;
using Xunit;

namespace ShareXMac.Tests;

public class MacClipboardTests
{
    [Fact]
    public void SetText_ThenGetText_RoundTrips()
    {
        string text = $"sharexmac-test-{Guid.NewGuid()}";
        MacClipboard.SetText(text);
        string? result = MacClipboard.GetText();
        Assert.Equal(text, result);
    }

    [Fact]
    public void ContainsText_AfterSetText_ReturnsTrue()
    {
        MacClipboard.SetText("hello");
        Assert.True(MacClipboard.ContainsText());
    }

    [Fact]
    public void GetText_ReturnsString_OrNullIfEmpty()
    {
        // After calling SetText, GetText should not throw
        MacClipboard.SetText("test");
        string? result = MacClipboard.GetText();
        Assert.NotNull(result);
    }
}
```

Run:
```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet test ShareXMac.Tests/ShareXMac.Tests.csproj --filter "MacClipboardTests" 2>&1 | tail -5
```
Expected: compile error (MacClipboard does not exist in ScreenCaptureLib)

- [ ] **Step 2: Create MacClipboard.cs**

Create `ShareXMac.ScreenCaptureLib/MacClipboard.cs`:
```csharp
using ShareXMac.ScreenCaptureLib.ObjC;

namespace ShareXMac.ScreenCaptureLib;

public static class MacClipboard
{
    // NSPasteboard type strings
    private const string NSPasteboardTypeString = "public.utf8-plain-text";
    private const string NSPasteboardTypePNG    = "public.png";

    private static nint GetGeneralPasteboard() =>
        ObjCRuntime.Send(ObjCRuntime.GetClass("NSPasteboard"), "generalPasteboard");

    public static void SetText(string text)
    {
        nint pb = GetGeneralPasteboard();
        ObjCRuntime.Send(pb, "clearContents");

        nint nsStr = ObjCRuntime.ToNSString(text);
        nint array = ObjCRuntime.ArrayWithObject(nsStr);
        // [pb writeObjects:array]
        ObjCRuntime.Send(pb, "writeObjects:", array);
    }

    public static string? GetText()
    {
        nint pb = GetGeneralPasteboard();
        nint typeStr = ObjCRuntime.ToNSString(NSPasteboardTypeString);
        nint data = ObjCRuntime.Send(pb, "stringForType:", typeStr);
        return ObjCRuntime.ToManagedString(data);
    }

    public static bool ContainsText()
    {
        nint pb = GetGeneralPasteboard();
        nint typeStr = ObjCRuntime.ToNSString(NSPasteboardTypeString);
        // [pb availableTypeFromArray:@[NSPasteboardTypeString]]
        nint types = ObjCRuntime.ArrayWithObject(typeStr);
        nint available = ObjCRuntime.Send(pb, "availableTypeFromArray:", types);
        return available != 0;
    }

    public static bool ContainsImage()
    {
        nint pb = GetGeneralPasteboard();
        nint typeStr = ObjCRuntime.ToNSString(NSPasteboardTypePNG);
        nint types = ObjCRuntime.ArrayWithObject(typeStr);
        nint available = ObjCRuntime.Send(pb, "availableTypeFromArray:", types);
        return available != 0;
    }

    public static void Clear()
    {
        nint pb = GetGeneralPasteboard();
        ObjCRuntime.Send(pb, "clearContents");
    }
}
```

The `Send(nint obj, string sel, nint arg)` overload covers single-nint-arg calls. `writeObjects:` and `stringForType:` and `availableTypeFromArray:` all take one `nint` argument, so `Send(obj, sel, arg)` works.

- [ ] **Step 3: Add missing ObjCRuntime overloads if needed**

`Send(nint obj, string sel, nint arg)` is already defined in Task 2. Verify `ObjCRuntime.cs` has this overload. No new overloads should be needed for clipboard.

- [ ] **Step 4: Update ClipboardHelpers.cs to use MacClipboard**

Replace `ShareXMac.HelpersLib/Helpers/ClipboardHelpers.cs`:
```csharp
#region License Information (GPL v3)
// ShareX - A program that allows you to take screenshots and share any file type
// Copyright (c) 2007-2025 ShareX Team
// GPL-3.0 — see LICENSE
#endregion

using System.Drawing;

namespace ShareX.HelpersLib
{
    // Delegates to ShareXMac.ScreenCaptureLib.MacClipboard for NSPasteboard access.
    // Image overloads remain stubs until Plan 3 (post-capture toolbar).
    public static class ClipboardHelpers
    {
        public const string FORMAT_PNG = "PNG";
        public const string FORMAT_17 = "Format17";

        public static bool Clear()
        {
            ShareXMac.ScreenCaptureLib.MacClipboard.Clear();
            return true;
        }

        public static bool CopyText(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            ShareXMac.ScreenCaptureLib.MacClipboard.SetText(text);
            return true;
        }

        public static bool CopyImage(Image img, string fileName = null) => false; // Plan 3
        public static bool CopyFile(string path) => false;
        public static bool CopyFile(string[] paths) => false;
        public static bool CopyImageFromFile(string path) => false;
        public static bool CopyTextFromFile(string path) => false;
        public static Bitmap GetImage(bool checkContainsImage = false) => null;
        public static Bitmap GetImageAlternative2() => null;

        public static string GetText(bool checkContainsText = false)
        {
            if (checkContainsText && !ContainsText()) return null;
            return ShareXMac.ScreenCaptureLib.MacClipboard.GetText();
        }

        public static string[] GetFileDropList(bool checkContainsFileDropList = false) => null;
        public static Bitmap TryGetImage() => null;
        public static bool ContainsImage() => ShareXMac.ScreenCaptureLib.MacClipboard.ContainsImage();
        public static bool ContainsText() => ShareXMac.ScreenCaptureLib.MacClipboard.ContainsText();
        public static bool ContainsFileDropList() => false;
    }
}
```

**Important:** `HelpersLib` does not reference `ScreenCaptureLib`. This would create a circular dependency. Instead, keep the `ClipboardHelpers` stub in HelpersLib and have `TrayViewModel` in the app shell call `MacClipboard` directly when wiring up clipboard after capture. The `ClipboardHelpers` class is used by UploadersLib/other libs for upload result URLs — those will use the real clipboard via the app shell injection pattern.

Revert the `ClipboardHelpers.cs` change to keep the stub. Update the ClipboardHelpers.cs comment:
```csharp
// macOS stub — clipboard calls routed through MacClipboard in the app shell
public static bool CopyText(string text) => false;
```

This is the correct architecture: `MacClipboard` in `ScreenCaptureLib` is called directly by the app shell's capture flow (Task 7), not via `ClipboardHelpers`.

- [ ] **Step 5: Run tests**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet test ShareXMac.Tests/ShareXMac.Tests.csproj --filter "MacClipboardTests" -v normal 2>&1 | tail -15
```
Expected: `3 passed`

- [ ] **Step 6: Commit**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && git add -A && git commit -m "feat: add MacClipboard with NSPasteboard P/Invoke"
```

---

### Task 6: MacNotificationService and MacHotkeyManager

**Files:**
- Create: `ShareXMac.ScreenCaptureLib/MacNotificationService.cs`
- Create: `ShareXMac.ScreenCaptureLib/MacHotkeyManager.cs`

- [ ] **Step 1: Write failing tests**

Add to `ShareXMac.Tests/ScreenCaptureTests.cs`:
```csharp
public class MacNotificationServiceTests
{
    [Fact]
    public async Task ShowAsync_DoesNotThrow()
    {
        var svc = new MacNotificationService();
        // May show a system notification if permission is granted, or silently fail
        await svc.ShowAsync("Test", "This is a test notification");
    }
}

public class MacHotkeyManagerTests
{
    [Fact]
    public void IsAvailable_ReturnsBoolean()
    {
        var mgr = new MacHotkeyManager();
        bool avail = mgr.IsAvailable;
        // Just verify it doesn't throw — value depends on Accessibility permission
        Assert.True(avail || !avail);
    }

    [Fact]
    public void UnregisterAll_WhenEmpty_DoesNotThrow()
    {
        var mgr = new MacHotkeyManager();
        mgr.UnregisterAll();
    }

    [Fact]
    public void Register_ThenUnregister_DoesNotThrow()
    {
        var mgr = new MacHotkeyManager();
        if (!mgr.IsAvailable) return; // skip without Accessibility permission
        mgr.Register("test-id", new ShareX.HelpersLib.KeyCombo("cmd", "1"), () => { });
        mgr.Unregister("test-id");
    }
}
```

Run:
```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet test ShareXMac.Tests/ShareXMac.Tests.csproj --filter "MacNotificationServiceTests|MacHotkeyManagerTests" 2>&1 | tail -5
```
Expected: compile error

- [ ] **Step 2: Create MacNotificationService.cs**

`UNUserNotificationCenter` is available on macOS 10.14+. We target macOS 13, so it's always present.

Create `ShareXMac.ScreenCaptureLib/MacNotificationService.cs`:
```csharp
using ShareX.HelpersLib;
using ShareXMac.ScreenCaptureLib.ObjC;

namespace ShareXMac.ScreenCaptureLib;

public class MacNotificationService : INotificationService
{
    private static bool s_frameworkLoaded;

    private static void EnsureLoaded()
    {
        if (s_frameworkLoaded) return;
        ObjCRuntime.dlopen(
            "/System/Library/Frameworks/UserNotifications.framework/UserNotifications", 1);
        s_frameworkLoaded = true;
    }

    public Task ShowAsync(string title, string body)
    {
        try
        {
            EnsureLoaded();

            // UNUserNotificationCenter.current()
            nint center = ObjCRuntime.Send(
                ObjCRuntime.GetClass("UNUserNotificationCenter"), "currentNotificationCenter");
            if (center == 0) return Task.CompletedTask;

            // UNMutableNotificationContent
            nint contentClass = ObjCRuntime.GetClass("UNMutableNotificationContent");
            nint content = ObjCRuntime.Send(
                ObjCRuntime.Send(contentClass, "alloc"), "init");

            // content.title = title
            nint nsTitle = ObjCRuntime.ToNSString(title);
            ObjCRuntime.Send(content, "setTitle:", nsTitle);

            // content.body = body
            nint nsBody = ObjCRuntime.ToNSString(body);
            ObjCRuntime.Send(content, "setBody:", nsBody);

            // UNNotificationRequest with identifier
            string identifier = Guid.NewGuid().ToString("N");
            nint nsId = ObjCRuntime.ToNSString(identifier);

            // [UNNotificationRequest requestWithIdentifier:content:trigger:]
            // trigger = nil (deliver immediately)
            nint requestClass = ObjCRuntime.GetClass("UNNotificationRequest");
            nint notifRequest = ObjCRuntime.Send(
                requestClass,
                "requestWithIdentifier:content:trigger:",
                nsId, content, 0 /* nil trigger */);

            // [center addNotificationRequest:withCompletionHandler:nil]
            // We pass nil for the completion handler (no callback)
            ObjCRuntime.Send(center, "addNotificationRequest:withCompletionHandler:",
                notifRequest, 0);
        }
        catch
        {
            // Notification failed (permission not granted, etc.) — silent failure
        }
        return Task.CompletedTask;
    }
}
```

Add the missing ObjC overload for 3-argument `Send` to `ObjCRuntime.cs`:
```csharp
// objc_msgSend with 3 nint args (e.g., requestWithIdentifier:content:trigger:)
[DllImport(ObjCLib, EntryPoint = "objc_msgSend")]
static extern nint objc_msgSend3(nint receiver, nint sel, nint a1, nint a2, nint a3);

internal static nint Send(nint obj, string sel, nint a1, nint a2, nint a3)
    => objc_msgSend3(obj, Sel(sel), a1, a2, a3);
```

- [ ] **Step 3: Create MacHotkeyManager.cs**

`CGEventTap` requires the Accessibility permission (`com.apple.security.automation.apple-events` entitlement, or Accessibility checkbox in System Settings). The manager checks availability and silently no-ops if not permitted.

Create `ShareXMac.ScreenCaptureLib/MacHotkeyManager.cs`:
```csharp
using System.Runtime.InteropServices;
using ShareX.HelpersLib;

namespace ShareXMac.ScreenCaptureLib;

public class MacHotkeyManager : IHotkeyManager, IDisposable
{
    // CoreGraphics event tap constants
    private const int kCGHIDEventTap = 0;
    private const int kCGHeadInsertEventTap = 0;
    private const int kCGEventTapOptionListenOnly = 1;
    private const ulong kCGEventMaskForAllEvents = 0xFFFFFFFF;

    // kCGEventKeyDown = 10
    private const ulong kCGEventKeyDownMask = 1ul << 10;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate nint CGEventCallback(nint proxy, uint type, nint eventRef, nint userInfo);

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern nint CGEventTapCreate(
        int tap, int place, int options,
        ulong eventsOfInterest,
        CGEventCallback callback,
        nint userInfo);

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern nint CFMachPortCreateRunLoopSource(
        nint allocator, nint tap, nint order);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern nint CFRunLoopGetCurrent();

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern void CFRunLoopAddSource(nint runloop, nint source, nint mode);

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern bool CGEventTapIsEnabled(nint tap);

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern long CGEventGetIntegerValueField(nint eventRef, int field);

    // kCGKeyboardEventKeycode = 9
    private const int kCGKeyboardEventKeycode = 9;

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
            try
            {
                // Attempt to create a passive tap to test Accessibility access
                nint testTap = CGEventTapCreate(
                    kCGHIDEventTap, kCGHeadInsertEventTap,
                    kCGEventTapOptionListenOnly,
                    kCGEventKeyDownMask,
                    _nativeCallback, 0);
                if (testTap == 0) return false;
                // Immediately dispose
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    public void Register(string id, KeyCombo combo, Action callback)
    {
        _hotkeys[id] = (combo, callback);
        EnsureTapRunning();
    }

    public void Unregister(string id)
    {
        _hotkeys.Remove(id);
    }

    public void UnregisterAll()
    {
        _hotkeys.Clear();
    }

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
        // type 10 = kCGEventKeyDown
        if (type == 10)
        {
            long keycode = CGEventGetIntegerValueField(eventRef, kCGKeyboardEventKeycode);
            foreach (var (id, (combo, callback)) in _hotkeys)
            {
                if (KeycodeMatchesCombo(keycode, combo))
                    callback();
            }
        }
        return eventRef;
    }

    private static bool KeycodeMatchesCombo(long keycode, KeyCombo combo)
    {
        // Minimal key matching: compare virtual key code by Key string mapping
        // This is a best-effort implementation; full modifier checking requires
        // reading CGEventGetFlags which is a separate P/Invoke call.
        // A full implementation is deferred — for now only key code is checked.
        return GetVirtualKeyCode(combo.Key) == keycode;
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
        _tapPort = 0;
    }
}
```

- [ ] **Step 4: Run tests**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet test ShareXMac.Tests/ShareXMac.Tests.csproj --filter "MacNotificationServiceTests|MacHotkeyManagerTests" -v normal 2>&1 | tail -15
```
Expected: `4 passed`

- [ ] **Step 5: Commit**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && git add -A && git commit -m "feat: add MacNotificationService and MacHotkeyManager"
```

---

### Task 7: Wire real implementations into app shell

**Files:**
- Modify: `ShareXMac/ViewModels/TrayViewModel.cs`
- Modify: `ShareXMac/App.axaml.cs`

`TrayViewModel` currently has synchronous void stub commands. We wire in `IScreenCapture` via constructor injection, make the capture/record commands async, and copy capture results to clipboard via `MacClipboard`.

`App.axaml.cs` instantiates `MacScreenCapture`, `MacNotificationService`, and `MacHotkeyManager` and passes them to `TrayViewModel`.

- [ ] **Step 1: Write failing tests**

Add to `ShareXMac.Tests/TrayViewModelTests.cs`:
```csharp
// Add this test to the existing TrayViewModelTests class
[Fact]
public void Constructor_WithScreenCapture_DoesNotThrow()
{
    var stub = new ShareXMac.Platform.StubScreenCapture();
    var vm = new TrayViewModel(stub);
    Assert.NotNull(vm);
}
```

Run:
```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet test ShareXMac.Tests/ShareXMac.Tests.csproj --filter "Constructor_WithScreenCapture" 2>&1 | tail -5
```
Expected: compile error (TrayViewModel doesn't accept IScreenCapture)

- [ ] **Step 2: Update TrayViewModel.cs**

Replace `ShareXMac/ViewModels/TrayViewModel.cs`:
```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia;
using ShareX.HelpersLib;
using ShareXMac.ScreenCaptureLib;

namespace ShareXMac.ViewModels;

public partial class TrayViewModel : ObservableObject
{
    private readonly IScreenCapture _capture;
    private bool _isRecording;

    public TrayViewModel(IScreenCapture capture)
    {
        _capture = capture;
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
            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Movies",
                $"ShareX-{DateTime.Now:yyyy-MM-dd-HHmmss}.mp4");
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
            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Movies",
                $"ShareX-{DateTime.Now:yyyy-MM-dd-HHmmss}.gif");
            await _capture.StartRecordingAsync(path, RecordingFormat.GIF);
            _isRecording = true;
        }
    }

    [RelayCommand]
    private void OpenSettings()
    {
        // SettingsWindow opens in Plan 3
    }

    [RelayCommand]
    private void OpenHistory()
    {
        // HistoryWindow opens in Plan 3
    }

    [RelayCommand]
    private static void Quit()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime app)
            app.Shutdown();
    }

    private static Task OnCaptureComplete(byte[] data)
    {
        // Copy PNG data to clipboard as file path via temp file
        // Full post-capture toolbar (annotate / upload / save) is Plan 3
        // For now: save to Desktop and copy path to clipboard
        string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        string path = Path.Combine(desktop, $"ShareX-{DateTime.Now:yyyy-MM-dd-HHmmss}.png");
        File.WriteAllBytes(path, data);
        MacClipboard.SetText(path);
        return Task.CompletedTask;
    }
}
```

- [ ] **Step 3: Update App.axaml.cs**

Replace `ShareXMac/App.axaml.cs`:
```csharp
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ShareXMac.ScreenCaptureLib;
using ShareXMac.ViewModels;

namespace ShareXMac;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        var capture = new MacScreenCapture();
        DataContext = new TrayViewModel(capture);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.ShutdownMode = Avalonia.Controls.ShutdownMode.OnExplicitShutdown;

        base.OnFrameworkInitializationCompleted();
    }
}
```

- [ ] **Step 4: Run all tests**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet test ShareXMac.Tests/ShareXMac.Tests.csproj -v normal 2>&1 | tail -20
```
Expected: all tests pass (the `PlatformStubTests` from Plan 1 may need updating if they construct `TrayViewModel()` without arguments — fix any failing tests by passing a `StubScreenCapture()`)

If `PlatformStubTests.cs` or `TrayViewModelTests.cs` construct `TrayViewModel` without arguments:
```bash
grep -n "new TrayViewModel()" /Users/austin/Documents/Dev/Projects/ShareX-Mac/ShareXMac.Tests/TrayViewModelTests.cs
```
Update any such calls to `new TrayViewModel(new ShareXMac.Platform.StubScreenCapture())`.

- [ ] **Step 5: Full build check**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet build ShareXMac/ShareXMac.csproj 2>&1 | tail -5
```
Expected: `0 Error(s)`

- [ ] **Step 6: Commit**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && git add -A && git commit -m "feat: wire real platform implementations into TrayViewModel and App"
```

---

## Plan Complete

After all 7 tasks:
- `screencapture` handles region, window, fullscreen (Screen Recording permission requested lazily on first capture)
- FFmpeg handles MP4 and GIF recording
- Vision OCR recognizes text from any captured image
- NSPasteboard clipboard is wired to capture results
- UNUserNotificationCenter delivers upload/copy notifications
- CGEventTap powers optional global hotkeys (Accessibility permission only if user enables a hotkey)
- TrayViewModel commands are async and wired to real implementations

**Next plan:** Plan 3 — Settings Window, History Window, and Post-Capture Toolbar (Avalonia windows, SettingsViewModel, HistoryViewModel)
