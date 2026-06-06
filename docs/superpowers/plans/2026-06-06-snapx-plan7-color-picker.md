# SnapX Plan 7: Screen Color Picker

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a screen color picker tool: a floating always-on-top window that magnifies the area under the cursor in real time, shows the sampled pixel's Hex/RGB/HSV values, and copies the hex string to clipboard on click or Enter.

**Architecture:** Four layers — (1) model: `SampledColor` record with color math (Hex, ToHsv); (2) platform: `MacColorSampler` using `CGDisplayCreateImageForRect` + `NSEvent.mouseLocation` P/Invokes to grab a 15×15 logical-pixel region around the cursor; (3) view-model: `ColorPickerViewModel` with injectable sampler, `DispatcherTimer`-driven `Refresh()`, and `CopyHexCommand`; (4) UI: a 162×270 dark floating `ColorPickerWindow` with a 150×150 magnified view, a color swatch, Hex/RGB/HSV labels, and a center-pixel border overlay.

**Tech Stack:** .NET 10, Avalonia 11.2.1, CommunityToolkit.Mvvm 8.4.0, CoreGraphics P/Invoke (`CGDisplayCreateImageForRect`, `CGMainDisplayID`, `CGDisplayPixelsHigh`, `CGImageGetDataProvider`, `CGImageGetBytesPerRow`, `CGImageGetBitsPerPixel`, `CGDataProviderCopyData`), CoreFoundation P/Invoke (`CFDataGetBytePtr`, `CFDataGetLength`, `CFRelease`), ObjC runtime (`NSEvent.mouseLocation`), xUnit.

> **Scope note:** Plan 6 mentioned color picker + annotation editor. These are independent subsystems. Plan 7 covers the color picker only. Annotation editor is Plan 8.

---

## File Structure

```
ShareXMac/
  Models/
    SampledColor.cs               CREATE — record(R, G, B) + Hex property + ToHsv() method
  ViewModels/
    ColorPickerViewModel.cs       CREATE — observable color state, injectable sampler, Refresh(), CopyHexCommand
  Views/
    ColorPickerWindow.axaml       CREATE — floating dark window: magnified view + overlay + swatch + labels
    ColorPickerWindow.axaml.cs    CREATE — DispatcherTimer, keyboard (Enter/Esc) and click handling
  ViewModels/
    TrayViewModel.cs              MODIFY — add OpenColorPickerCommand
  App.axaml                       MODIFY — add "Color Picker" menu item above the Settings separator

ShareXMac.ScreenCaptureLib/
  MacColorSampler.cs              CREATE — CGDisplayCreateImageForRect + NSEvent.mouseLocation P/Invokes

ShareXMac.Tests/
  SampledColorTests.cs            CREATE — 9 tests for Hex formatting and ToHsv conversion
  ColorPickerViewModelTests.cs    CREATE — 5 tests for Refresh() state updates
```

---

### Task 1: SampledColor model

Create the color math record. No UI, no P/Invoke — pure managed code and tests.

**Files:**
- Create: `ShareXMac/Models/SampledColor.cs`
- Create: `ShareXMac.Tests/SampledColorTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `ShareXMac.Tests/SampledColorTests.cs`:

```csharp
using ShareXMac.Models;
using Xunit;

namespace ShareXMac.Tests;

public class SampledColorTests
{
    [Fact]
    public void Hex_FormatsBlack()
        => Assert.Equal("#000000", new SampledColor(0, 0, 0).Hex);

    [Fact]
    public void Hex_FormatsWhite()
        => Assert.Equal("#FFFFFF", new SampledColor(255, 255, 255).Hex);

    [Fact]
    public void Hex_FormatsRed()
        => Assert.Equal("#FF0000", new SampledColor(255, 0, 0).Hex);

    [Fact]
    public void Hex_FormatsMidGray()
        => Assert.Equal("#808080", new SampledColor(128, 128, 128).Hex);

    [Fact]
    public void ToHsv_Black_ReturnsZero()
    {
        var (h, s, v) = new SampledColor(0, 0, 0).ToHsv();
        Assert.Equal(0, h); Assert.Equal(0, s); Assert.Equal(0, v);
    }

    [Fact]
    public void ToHsv_White_ReturnsFullV()
    {
        var (h, s, v) = new SampledColor(255, 255, 255).ToHsv();
        Assert.Equal(0, h); Assert.Equal(0, s); Assert.Equal(100, v);
    }

    [Fact]
    public void ToHsv_Red_HueZero()
    {
        var (h, s, v) = new SampledColor(255, 0, 0).ToHsv();
        Assert.Equal(0, h); Assert.Equal(100, s); Assert.Equal(100, v);
    }

    [Fact]
    public void ToHsv_Green_Hue120()
    {
        var (h, s, v) = new SampledColor(0, 255, 0).ToHsv();
        Assert.Equal(120, h); Assert.Equal(100, s); Assert.Equal(100, v);
    }

    [Fact]
    public void ToHsv_Blue_Hue240()
    {
        var (h, s, v) = new SampledColor(0, 0, 255).ToHsv();
        Assert.Equal(240, h); Assert.Equal(100, s); Assert.Equal(100, v);
    }
}
```

- [ ] **Step 2: Run to confirm compile failure**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet test ShareXMac.Tests/ShareXMac.Tests.csproj --filter "SampledColorTests" 2>&1 | tail -5
```

Expected: compile error (`SampledColor` not found)

- [ ] **Step 3: Create SampledColor.cs**

Create `ShareXMac/Models/SampledColor.cs`:

```csharp
namespace ShareXMac.Models;

public record SampledColor(byte R, byte G, byte B)
{
    public string Hex => $"#{R:X2}{G:X2}{B:X2}";

    // Returns (Hue 0-360, Saturation 0-100, Value 0-100), rounded to nearest integer.
    public (double H, double S, double V) ToHsv()
    {
        double rf = R / 255.0, gf = G / 255.0, bf = B / 255.0;
        double max = Math.Max(rf, Math.Max(gf, bf));
        double min = Math.Min(rf, Math.Min(gf, bf));
        double delta = max - min;

        double h = 0;
        if (delta > 0)
        {
            if (max == rf)       h = 60 * ((gf - bf) / delta % 6);
            else if (max == gf)  h = 60 * ((bf - rf) / delta + 2);
            else                 h = 60 * ((rf - gf) / delta + 4);
            if (h < 0) h += 360;
        }
        double s = max == 0 ? 0 : delta / max;
        return (Math.Round(h), Math.Round(s * 100), Math.Round(max * 100));
    }
}
```

- [ ] **Step 4: Run the new tests**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet test ShareXMac.Tests/ShareXMac.Tests.csproj --filter "SampledColorTests" -v normal 2>&1 | tail -10
```

Expected: 9 passed

- [ ] **Step 5: Run all tests**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet test ShareXMac.Tests/ShareXMac.Tests.csproj 2>&1 | tail -3
```

Expected: 87 passed (78 + 9)

- [ ] **Step 6: Commit**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && git add ShareXMac/Models/SampledColor.cs ShareXMac.Tests/SampledColorTests.cs && git commit -m "feat: add SampledColor model with Hex and ToHsv"
```

---

### Task 2: MacColorSampler

Platform layer: capture a rectangular screen region around the cursor via CoreGraphics and return an array of `SampledColor`.

No unit tests — this is pure P/Invoke code that requires a live macOS display. Build success is the verification.

**Files:**
- Create: `ShareXMac.ScreenCaptureLib/MacColorSampler.cs`

- [ ] **Step 1: Create MacColorSampler.cs**

Create `ShareXMac.ScreenCaptureLib/MacColorSampler.cs`:

```csharp
using System.Runtime.InteropServices;
using ShareXMac.Models;
using ShareXMac.ScreenCaptureLib.ObjC;

namespace ShareXMac.ScreenCaptureLib;

public static class MacColorSampler
{
    [StructLayout(LayoutKind.Sequential)]
    private struct CGRect { public double X, Y, Width, Height; }

    // NSPoint: NSEvent.mouseLocation — Y=0 at bottom-left, Y increases upward (same as CoreGraphics)
    [StructLayout(LayoutKind.Sequential)]
    private struct NSPoint { public double X, Y; }

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern uint CGMainDisplayID();

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern double CGDisplayPixelsHigh(uint display);

    // rect is in display points (logical pixels); on Retina returns 2x physical pixels
    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern nint CGDisplayCreateImageForRect(uint display, CGRect rect);

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern nint CGImageGetDataProvider(nint image);

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern nuint CGImageGetBytesPerRow(nint image);

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern nuint CGImageGetBitsPerPixel(nint image);

    // CGDataProviderCopyData returns a CFDataRef — caller must CFRelease
    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern nint CGDataProviderCopyData(nint provider);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern nint CFDataGetBytePtr(nint cfData);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern nint CFDataGetLength(nint cfData);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern void CFRelease(nint cf);

    // NSEvent.mouseLocation via ObjC — returns NSPoint with Y=0 at bottom-left
    [DllImport("libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static extern NSPoint objc_msgSend_NSPoint(nint receiver, nint sel);

    private static NSPoint GetCursorPosition()
        => objc_msgSend_NSPoint(ObjCRuntime.GetClass("NSEvent"), ObjCRuntime.Sel("mouseLocation"));

    /// <summary>
    /// Returns (2*radius+1)^2 colors in row-major order, centered on the current cursor position.
    /// Returns an all-black array on failure.
    /// </summary>
    public static SampledColor[] SampleRegion(int radius = 7)
    {
        int size = 2 * radius + 1;
        var fallback = new SampledColor[size * size];
        for (int i = 0; i < fallback.Length; i++) fallback[i] = new SampledColor(0, 0, 0);

        NSPoint cursor = GetCursorPosition();
        uint display   = CGMainDisplayID();

        // CoreGraphics Y-axis matches NSEvent (both 0 at bottom-left), no flip needed
        var rect = new CGRect
        {
            X = cursor.X - radius,
            Y = cursor.Y - radius,
            Width  = size,
            Height = size
        };

        nint image = CGDisplayCreateImageForRect(display, rect);
        if (image == 0) return fallback;

        try
        {
            nint provider = CGImageGetDataProvider(image);
            nint cfData   = CGDataProviderCopyData(provider);
            if (cfData == 0) return fallback;

            try
            {
                nint bytesPtr  = CFDataGetBytePtr(cfData);
                int  byteCount = (int)CFDataGetLength(cfData);
                int  stride    = (int)CGImageGetBytesPerRow(image);
                int  bpp       = (int)CGImageGetBitsPerPixel(image) / 8; // 4 for BGRA8888

                if (bpp == 0 || stride == 0) return fallback;

                byte[] raw = new byte[byteCount];
                Marshal.Copy(bytesPtr, raw, 0, byteCount);

                // Physical image dimensions (may be 2x logical on Retina)
                int imgW = stride / bpp;
                int imgH = byteCount / stride;
                double sx = (double)imgW / size;
                double sy = (double)imgH / size;

                var result = new SampledColor[size * size];
                for (int ly = 0; ly < size; ly++)
                {
                    for (int lx = 0; lx < size; lx++)
                    {
                        int px     = (int)(lx * sx);
                        int py     = (int)(ly * sy);
                        int offset = py * stride + px * bpp;
                        if (offset + 2 < raw.Length)
                        {
                            // CGDisplayCreateImageForRect returns BGRA8888 on macOS
                            byte b = raw[offset], g = raw[offset + 1], r = raw[offset + 2];
                            result[ly * size + lx] = new SampledColor(r, g, b);
                        }
                        else
                        {
                            result[ly * size + lx] = new SampledColor(0, 0, 0);
                        }
                    }
                }
                return result;
            }
            finally { CFRelease(cfData); }
        }
        finally { CFRelease(image); }
    }
}
```

- [ ] **Step 2: Build to verify it compiles**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet build ShareX-Mac.sln 2>&1 | tail -5
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 3: Run all tests to confirm nothing broken**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet test ShareXMac.Tests/ShareXMac.Tests.csproj 2>&1 | tail -3
```

Expected: 87 passed

- [ ] **Step 4: Commit**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && git add ShareXMac.ScreenCaptureLib/MacColorSampler.cs && git commit -m "feat: add MacColorSampler with CGDisplayCreateImageForRect"
```

---

### Task 3: ColorPickerViewModel

Observable view-model with injectable sampler (for testability), timer-driven `Refresh()`, and `CopyHexCommand`.

**Files:**
- Create: `ShareXMac/ViewModels/ColorPickerViewModel.cs`
- Create: `ShareXMac.Tests/ColorPickerViewModelTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `ShareXMac.Tests/ColorPickerViewModelTests.cs`:

```csharp
using Avalonia;
using Avalonia.Headless;
using ShareXMac.Models;
using ShareXMac.ViewModels;
using Xunit;

namespace ShareXMac.Tests;

// Avalonia requires an application instance to create WriteableBitmap
[assembly: AvaloniaApp(typeof(ShareXMac.App))]

public class ColorPickerViewModelTests
{
    private static SampledColor[] AllColor(byte r, byte g, byte b)
        => Enumerable.Repeat(new SampledColor(r, g, b), 15 * 15).ToArray();

    [Fact]
    public void Refresh_SetsHex_FromCenterPixel()
    {
        var vm = new ColorPickerViewModel(() => AllColor(255, 0, 0));
        vm.Refresh();
        Assert.Equal("#FF0000", vm.Hex);
    }

    [Fact]
    public void Refresh_SetsRGB_FromCenterPixel()
    {
        var vm = new ColorPickerViewModel(() => AllColor(0, 0, 255));
        vm.Refresh();
        Assert.Equal(0, vm.R); Assert.Equal(0, vm.G); Assert.Equal(255, vm.B);
    }

    [Fact]
    public void Refresh_SetsHsv_ForGreen()
    {
        var vm = new ColorPickerViewModel(() => AllColor(0, 255, 0));
        vm.Refresh();
        Assert.Equal(120, vm.Hue);
        Assert.Equal(100, vm.Saturation);
        Assert.Equal(100, vm.Value);
    }

    [Fact]
    public void Refresh_SetsMagnifiedView_NonNull()
    {
        var vm = new ColorPickerViewModel(() => AllColor(128, 64, 32));
        vm.Refresh();
        Assert.NotNull(vm.MagnifiedView);
    }

    [Fact]
    public void Refresh_WithEmptyArray_DoesNotThrow()
    {
        var vm = new ColorPickerViewModel(() => Array.Empty<SampledColor>());
        var ex = Record.Exception(() => vm.Refresh());
        Assert.Null(ex);
    }
}
```

- [ ] **Step 2: Run to confirm compile failure**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet test ShareXMac.Tests/ShareXMac.Tests.csproj --filter "ColorPickerViewModelTests" 2>&1 | tail -5
```

Expected: compile error (`ColorPickerViewModel` not found)

- [ ] **Step 3: Check how Avalonia headless tests are configured in this project**

Read `ShareXMac.Tests/AppViewModelTests.cs` to see whether `[assembly: AvaloniaApp]` is already present somewhere, and whether there's an existing headless setup:

```bash
grep -r "AvaloniaApp\|UseHeadless\|AppBuilder\|AvaloniaHeadless" /Users/austin/Documents/Dev/Projects/ShareX-Mac/ShareXMac.Tests/ 2>/dev/null
grep -r "AvaloniaApp\|UseHeadless" /Users/austin/Documents/Dev/Projects/ShareX-Mac/ShareXMac.Tests/ShareXMac.Tests.csproj 2>/dev/null
```

If `[assembly: AvaloniaApp(typeof(ShareXMac.App))]` already appears in the test project (e.g. in `AppViewModelTests.cs`), remove the duplicate `[assembly: ...]` line from `ColorPickerViewModelTests.cs` — assembly attributes can only appear once.

If the project uses `UseHeadless` in some other setup file (like an `AssemblyFixture`), follow the same pattern.

- [ ] **Step 4: Create ColorPickerViewModel.cs**

Create `ShareXMac/ViewModels/ColorPickerViewModel.cs`:

```csharp
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShareXMac.Models;
using ShareXMac.ScreenCaptureLib;

namespace ShareXMac.ViewModels;

public partial class ColorPickerViewModel : ObservableObject
{
    private readonly Func<SampledColor[]> _sample;

    [ObservableProperty] private Bitmap?       _magnifiedView;
    [ObservableProperty] private string        _hex          = "#000000";
    [ObservableProperty] private byte          _r, _g, _b;
    [ObservableProperty] private double        _hue, _saturation, _value;
    [ObservableProperty] private SolidColorBrush _swatchBrush = new(Colors.Black);

    // Production: samples from the real screen
    public ColorPickerViewModel() : this(() => MacColorSampler.SampleRegion(7)) { }

    // Tests: inject a fixed pixel array
    public ColorPickerViewModel(Func<SampledColor[]> sample) => _sample = sample;

    public void Refresh()
    {
        SampledColor[] pixels = _sample();
        int size = 15;
        int center = size / 2 * size + size / 2;
        SampledColor c = center < pixels.Length ? pixels[center] : new SampledColor(0, 0, 0);

        R = c.R; G = c.G; B = c.B;
        Hex = c.Hex;
        var (h, s, v) = c.ToHsv();
        Hue = h; Saturation = s; Value = v;
        SwatchBrush = new SolidColorBrush(new Color(255, c.R, c.G, c.B));

        MagnifiedView = BuildMagnifiedBitmap(pixels, size);
    }

    [RelayCommand]
    public void CopyHex() => MacClipboard.SetText(Hex);

    private static unsafe Bitmap BuildMagnifiedBitmap(SampledColor[] pixels, int size)
    {
        const int mag = 10;
        if (pixels.Length == 0)
            return new WriteableBitmap(new PixelSize(1, 1), new Vector(96, 96),
                PixelFormat.Bgra8888, AlphaFormat.Unpremul);

        var bmp = new WriteableBitmap(
            new PixelSize(size * mag, size * mag),
            new Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Unpremul);

        using var fb = bmp.Lock();
        uint* dst    = (uint*)fb.Address;
        int   stride = fb.RowBytes / 4;

        for (int py = 0; py < size; py++)
        {
            for (int px = 0; px < size; px++)
            {
                int idx = py * size + px;
                SampledColor p = idx < pixels.Length ? pixels[idx] : new SampledColor(0, 0, 0);
                // Bgra8888 as uint (LE): 0xAARRGGBB
                uint color = 0xFF000000u | ((uint)p.R << 16) | ((uint)p.G << 8) | p.B;
                for (int dy = 0; dy < mag; dy++)
                    for (int dx = 0; dx < mag; dx++)
                        dst[(py * mag + dy) * stride + px * mag + dx] = color;
            }
        }
        return bmp;
    }
}
```

- [ ] **Step 5: Run ColorPickerViewModelTests**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet test ShareXMac.Tests/ShareXMac.Tests.csproj --filter "ColorPickerViewModelTests" -v normal 2>&1 | tail -10
```

Expected: 5 passed

- [ ] **Step 6: Run all tests**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet test ShareXMac.Tests/ShareXMac.Tests.csproj 2>&1 | tail -3
```

Expected: 92 passed (87 + 5)

- [ ] **Step 7: Commit**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && git add ShareXMac/ViewModels/ColorPickerViewModel.cs ShareXMac.Tests/ColorPickerViewModelTests.cs && git commit -m "feat: add ColorPickerViewModel with injectable sampler"
```

---

### Task 4: ColorPickerWindow + tray wiring

The floating color picker window (AXAML + code-behind), a new tray command, and the menu item in App.axaml.

**Files:**
- Create: `ShareXMac/Views/ColorPickerWindow.axaml`
- Create: `ShareXMac/Views/ColorPickerWindow.axaml.cs`
- Modify: `ShareXMac/ViewModels/TrayViewModel.cs`
- Modify: `ShareXMac/App.axaml`

- [ ] **Step 1: Create ColorPickerWindow.axaml**

Create `ShareXMac/Views/ColorPickerWindow.axaml`:

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="using:ShareXMac.ViewModels"
        x:Class="ShareXMac.Views.ColorPickerWindow"
        x:DataType="vm:ColorPickerViewModel"
        Width="162" Height="270"
        CanResize="False"
        SystemDecorations="None"
        Topmost="True"
        WindowStartupLocation="CenterScreen"
        Background="#FF1E1E1E"
        Title="Color Picker">
    <StackPanel Margin="6" Spacing="4">

        <!-- Magnified pixel view with center-pixel border overlay -->
        <Grid Width="150" Height="150" HorizontalAlignment="Center">
            <Image Source="{Binding MagnifiedView}"
                   Width="150" Height="150"
                   RenderOptions.BitmapInterpolationMode="None"
                   Stretch="Fill" />
            <!-- Center pixel highlight: the sampled pixel is at position 7,7 (0-based)
                 Each logical pixel is 10×10 — so the center square starts at (70,70) -->
            <Canvas Width="150" Height="150" IsHitTestVisible="False">
                <Rectangle Canvas.Left="70" Canvas.Top="70"
                           Width="10" Height="10"
                           Stroke="White" StrokeThickness="1"
                           Fill="Transparent" Opacity="0.85" />
            </Canvas>
        </Grid>

        <!-- Color swatch -->
        <Border Height="22" CornerRadius="3"
                Background="{Binding SwatchBrush}"
                HorizontalAlignment="Stretch" />

        <!-- Hex value (large, monospaced) -->
        <TextBlock Text="{Binding Hex}"
                   Foreground="White"
                   FontSize="16"
                   FontFamily="Menlo,Courier New,monospace"
                   HorizontalAlignment="Center" />

        <!-- RGB row -->
        <StackPanel Orientation="Horizontal"
                    HorizontalAlignment="Center" Spacing="10">
            <TextBlock Text="{Binding R, StringFormat='R {0}'}"
                       Foreground="#FFCC4444" FontSize="11" />
            <TextBlock Text="{Binding G, StringFormat='G {0}'}"
                       Foreground="#FF44CC44" FontSize="11" />
            <TextBlock Text="{Binding B, StringFormat='B {0}'}"
                       Foreground="#FF4488FF" FontSize="11" />
        </StackPanel>

        <!-- HSV row -->
        <StackPanel Orientation="Horizontal"
                    HorizontalAlignment="Center" Spacing="10">
            <TextBlock Text="{Binding Hue,        StringFormat='H {0:F0}°'}"
                       Foreground="#FFAAAAAA" FontSize="11" />
            <TextBlock Text="{Binding Saturation, StringFormat='S {0:F0}%'}"
                       Foreground="#FFAAAAAA" FontSize="11" />
            <TextBlock Text="{Binding Value,      StringFormat='V {0:F0}%'}"
                       Foreground="#FFAAAAAA" FontSize="11" />
        </StackPanel>

        <!-- Hint -->
        <TextBlock Text="Click or Enter to copy  ·  Esc to cancel"
                   Foreground="#FF666666" FontSize="10"
                   HorizontalAlignment="Center" />

    </StackPanel>
</Window>
```

- [ ] **Step 2: Create ColorPickerWindow.axaml.cs**

Create `ShareXMac/Views/ColorPickerWindow.axaml.cs`:

```csharp
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using ShareXMac.ViewModels;

namespace ShareXMac.Views;

public partial class ColorPickerWindow : Window
{
    private readonly DispatcherTimer _timer;

    public ColorPickerWindow(ColorPickerViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(80) };
        _timer.Tick += (_, _) => vm.Refresh();

        Opened  += (_, _) => { vm.Refresh(); _timer.Start(); };
        Closed  += (_, _) => _timer.Stop();

        KeyDown        += OnKeyDown;
        PointerPressed += (_, _) => PickAndClose(vm);
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape) { Close(); e.Handled = true; }
        else if (e.Key == Key.Enter) { PickAndClose((ColorPickerViewModel)DataContext!); e.Handled = true; }
    }

    private void PickAndClose(ColorPickerViewModel vm)
    {
        vm.CopyHexCommand.Execute(null);
        Close();
    }
}
```

- [ ] **Step 3: Add OpenColorPickerCommand to TrayViewModel**

Read `ShareXMac/ViewModels/TrayViewModel.cs`. Locate the `OpenHistory` and `Quit` commands. Add the new command after `OpenHistory`:

```csharp
[RelayCommand]
private void OpenColorPicker()
{
    Dispatcher.UIThread.Post(() =>
    {
        var vm = new ColorPickerViewModel();
        new ColorPickerWindow(vm).Show();
    });
}
```

- [ ] **Step 4: Add Color Picker menu item to App.axaml**

Read `ShareXMac/App.axaml`. Add a `Color Picker` item in the second separator block (between the record items and Settings), immediately before `<NativeMenuItemSeparator />` that precedes Settings:

```xml
<NativeMenuItemSeparator />
<NativeMenuItem Header="Color Picker"
                Command="{Binding OpenColorPickerCommand}" />
<NativeMenuItemSeparator />
<NativeMenuItem Header="Settings..."
                Command="{Binding OpenSettingsCommand}" />
```

The full updated App.axaml NativeMenu block should be:

```xml
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
    <NativeMenuItem Header="Color Picker"
                    Command="{Binding OpenColorPickerCommand}" />
    <NativeMenuItemSeparator />
    <NativeMenuItem Header="Settings..."
                    Command="{Binding OpenSettingsCommand}" />
    <NativeMenuItem Header="History..."
                    Command="{Binding OpenHistoryCommand}" />
    <NativeMenuItemSeparator />
    <NativeMenuItem Header="Quit"
                    Command="{Binding QuitCommand}" />
</NativeMenu>
```

- [ ] **Step 5: Build**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet build ShareX-Mac.sln 2>&1 | tail -5
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 6: Run all tests**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet test ShareXMac.Tests/ShareXMac.Tests.csproj 2>&1 | tail -3
```

Expected: 92 passed

- [ ] **Step 7: Manual smoke test**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet run --project ShareXMac/ShareXMac.csproj
```

1. Click tray icon → "Color Picker" should appear in the menu
2. Click "Color Picker" → a small dark window appears at screen center
3. Move the cursor around the screen — the magnified view should update ~12 times/second
4. Hover over something red → swatch turns red, Hex shows `#FF...`, H ≈ 0
5. Press Enter → hex is copied to clipboard (verify with Cmd+V), window closes
6. Open Color Picker again → move to a blue area → click anywhere in the window → copies hex, closes
7. Open again → press Escape → window closes without copying

- [ ] **Step 8: Commit and push**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && git add ShareXMac/Views/ColorPickerWindow.axaml ShareXMac/Views/ColorPickerWindow.axaml.cs ShareXMac/ViewModels/TrayViewModel.cs ShareXMac/App.axaml && git commit -m "feat: add ColorPickerWindow with tray menu integration" && git push origin master
```

---

## Plan Complete

After all 4 tasks:

- **Color Picker** is accessible from the tray menu
- Moving the cursor updates the magnified 15×15 view at ~12fps
- The center pixel's color is shown as Hex/RGB/HSV with a live color swatch
- Click anywhere in the window or press Enter → copies hex to clipboard, closes
- Esc closes without copying
- 92 tests passing (78 baseline + 9 SampledColor + 5 ColorPickerViewModel)

**Next plan:** Plan 8 — Image annotation editor (draw arrows, boxes, and text on screenshots before saving/uploading).
