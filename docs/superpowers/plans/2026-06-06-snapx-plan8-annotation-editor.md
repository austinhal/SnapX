# SnapX Plan 8: Image Annotation Editor

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add an annotation editor that opens from the PostCaptureWindow, lets the user draw arrows, rectangles, and text on a screenshot, then replaces the captured image with the flattened result.

**Architecture:** Four layers — (1) model: `Annotation` record hierarchy (`ArrowAnnotation`, `RectAnnotation`, `TextAnnotation`) and `AnnotationLayer` with undo history; (2) rendering: `AnnotationCanvas`, a custom Avalonia `Control` that overrides `Render()` using `DrawingContext`; (3) view-model: `AnnotationEditorViewModel` with injectable tool/color state, drag lifecycle (`StartDrag/UpdateDrag/EndDrag`), and event-based completion; (4) UI: `AnnotationEditorWindow` with toolbar, canvas, inline text input overlay, and `RenderTargetBitmap` flattening on Done. `PostCaptureViewModel` grows an `AnnotateCommand` that opens the editor and updates `_imageData`/`Thumbnail` on completion.

**Tech Stack:** .NET 10, Avalonia 11.2.1 (`Control.Render`, `DrawingContext`, `RenderTargetBitmap`, `StyledProperty`, `AffectsRender`/`AffectsMeasure`), CommunityToolkit.Mvvm 8.4.0, xUnit.

> **Scope note:** Plan 7 (Color Picker) is complete. This plan covers the annotation editor only.

---

## File Structure

```
ShareXMac/
  Models/
    Annotation.cs               CREATE — AnnotationTool enum + Annotation abstract record + Arrow/Rect/Text subtypes
    AnnotationLayer.cs          CREATE — mutable list with Add/Undo/Clear + Changed event
  Views/
    AnnotationCanvas.cs         CREATE — custom Control: StyledProperties (Source, Layer, InProgress), Render() override
    AnnotationEditorWindow.axaml     CREATE — toolbar + ScrollViewer/AnnotationCanvas + text overlay
    AnnotationEditorWindow.axaml.cs  CREATE — pointer events, text input, RenderTargetBitmap flatten on Done
  ViewModels/
    AnnotationEditorViewModel.cs     CREATE — tool/color state, drag lifecycle, PlaceText, Undo/Cancel/Done
    PostCaptureViewModel.cs          MODIFY — make Thumbnail [ObservableProperty], add AnnotateCommand
  Views/
    PostCaptureWindow.axaml          MODIFY — add "Annotate" button next to existing action buttons

ShareXMac.Tests/
  AnnotationLayerTests.cs            CREATE — 9 tests
  AnnotationEditorViewModelTests.cs  CREATE — 8 tests
```

---

### Task 1: Annotation model + AnnotationLayer

Pure managed code — no Avalonia, no P/Invoke.

**Files:**
- Create: `ShareXMac/Models/Annotation.cs`
- Create: `ShareXMac/Models/AnnotationLayer.cs`
- Create: `ShareXMac.Tests/AnnotationLayerTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `ShareXMac.Tests/AnnotationLayerTests.cs`:

```csharp
using ShareXMac.Models;
using Xunit;

namespace ShareXMac.Tests;

public class AnnotationLayerTests
{
    [Fact]
    public void Add_IncreasesCount()
    {
        var layer = new AnnotationLayer();
        layer.Add(new ArrowAnnotation(0, 0, 10, 10, "#FF0000", 3));
        Assert.Equal(1, layer.Count);
    }

    [Fact]
    public void Add_Multiple_PreservesOrder()
    {
        var layer = new AnnotationLayer();
        var a = new ArrowAnnotation(0, 0, 1, 1, "#FF0000", 3);
        var b = new RectAnnotation(5, 5, 20, 20, "#0000FF", 2);
        layer.Add(a);
        layer.Add(b);
        Assert.Equal(a, layer.Items[0]);
        Assert.Equal(b, layer.Items[1]);
    }

    [Fact]
    public void Undo_RemovesLastItem()
    {
        var layer = new AnnotationLayer();
        layer.Add(new ArrowAnnotation(0, 0, 10, 10, "#FF0000", 3));
        layer.Add(new RectAnnotation(0, 0, 5, 5, "#0000FF", 2));
        layer.Undo();
        Assert.Equal(1, layer.Count);
    }

    [Fact]
    public void Undo_OnEmpty_ReturnsFalse()
    {
        var layer = new AnnotationLayer();
        Assert.False(layer.Undo());
    }

    [Fact]
    public void Undo_OnNonEmpty_ReturnsTrue()
    {
        var layer = new AnnotationLayer();
        layer.Add(new ArrowAnnotation(0, 0, 10, 10, "#FF0000", 3));
        Assert.True(layer.Undo());
    }

    [Fact]
    public void Clear_RemovesAllItems()
    {
        var layer = new AnnotationLayer();
        layer.Add(new ArrowAnnotation(0, 0, 10, 10, "#FF0000", 3));
        layer.Add(new ArrowAnnotation(1, 1, 11, 11, "#FF0000", 3));
        layer.Clear();
        Assert.Equal(0, layer.Count);
    }

    [Fact]
    public void Changed_FiresOnAdd()
    {
        var layer = new AnnotationLayer();
        bool fired = false;
        layer.Changed += () => fired = true;
        layer.Add(new ArrowAnnotation(0, 0, 1, 1, "#FF0000", 3));
        Assert.True(fired);
    }

    [Fact]
    public void Changed_FiresOnUndo()
    {
        var layer = new AnnotationLayer();
        layer.Add(new ArrowAnnotation(0, 0, 1, 1, "#FF0000", 3));
        bool fired = false;
        layer.Changed += () => fired = true;
        layer.Undo();
        Assert.True(fired);
    }

    [Fact]
    public void Changed_FiresOnClear()
    {
        var layer = new AnnotationLayer();
        layer.Add(new ArrowAnnotation(0, 0, 1, 1, "#FF0000", 3));
        bool fired = false;
        layer.Changed += () => fired = true;
        layer.Clear();
        Assert.True(fired);
    }
}
```

- [ ] **Step 2: Run to confirm compile failure**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet test ShareXMac.Tests/ShareXMac.Tests.csproj --filter "AnnotationLayerTests" 2>&1 | tail -5
```

Expected: compile error (`AnnotationLayer`, `ArrowAnnotation` not found)

- [ ] **Step 3: Create Annotation.cs**

Create `ShareXMac/Models/Annotation.cs`:

```csharp
namespace ShareXMac.Models;

public enum AnnotationTool { Arrow, Rectangle, Text }

public abstract record Annotation(string Color, double StrokeWidth);

public record ArrowAnnotation(
    double X1, double Y1, double X2, double Y2,
    string Color, double StrokeWidth = 3)
    : Annotation(Color, StrokeWidth);

public record RectAnnotation(
    double X, double Y, double Width, double Height,
    string Color, double StrokeWidth = 3)
    : Annotation(Color, StrokeWidth);

public record TextAnnotation(
    double X, double Y, string Text,
    string Color, double FontSize = 18)
    : Annotation(Color, 0);
```

- [ ] **Step 4: Create AnnotationLayer.cs**

Create `ShareXMac/Models/AnnotationLayer.cs`:

```csharp
namespace ShareXMac.Models;

public class AnnotationLayer
{
    private readonly List<Annotation> _items = new();

    public IReadOnlyList<Annotation> Items => _items;
    public int Count => _items.Count;

    public event Action? Changed;

    public void Add(Annotation a)
    {
        _items.Add(a);
        Changed?.Invoke();
    }

    public bool Undo()
    {
        if (_items.Count == 0) return false;
        _items.RemoveAt(_items.Count - 1);
        Changed?.Invoke();
        return true;
    }

    public void Clear()
    {
        _items.Clear();
        Changed?.Invoke();
    }
}
```

- [ ] **Step 5: Run the new tests**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet test ShareXMac.Tests/ShareXMac.Tests.csproj --filter "AnnotationLayerTests" -v normal 2>&1 | tail -10
```

Expected: 9 passed

- [ ] **Step 6: Run all tests**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet test ShareXMac.Tests/ShareXMac.Tests.csproj 2>&1 | tail -3
```

Expected: 105 passed (96 + 9)

- [ ] **Step 7: Commit**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && git add ShareXMac/Models/Annotation.cs ShareXMac/Models/AnnotationLayer.cs ShareXMac.Tests/AnnotationLayerTests.cs && git commit -m "feat: add Annotation model and AnnotationLayer"
```

---

### Task 2: AnnotationCanvas custom control

Custom Avalonia `Control` that renders a bitmap and a list of annotations. No unit tests — build success is verification.

**Files:**
- Create: `ShareXMac/Views/AnnotationCanvas.cs`

- [ ] **Step 1: Create AnnotationCanvas.cs**

Create `ShareXMac/Views/AnnotationCanvas.cs`:

```csharp
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using ShareXMac.Models;

namespace ShareXMac.Views;

public class AnnotationCanvas : Control
{
    public static readonly StyledProperty<Bitmap?> SourceProperty =
        AvaloniaProperty.Register<AnnotationCanvas, Bitmap?>(nameof(Source));

    public static readonly StyledProperty<AnnotationLayer?> LayerProperty =
        AvaloniaProperty.Register<AnnotationCanvas, AnnotationLayer?>(nameof(Layer));

    public static readonly StyledProperty<Annotation?> InProgressProperty =
        AvaloniaProperty.Register<AnnotationCanvas, Annotation?>(nameof(InProgress));

    static AnnotationCanvas()
    {
        AffectsRender<AnnotationCanvas>(SourceProperty, InProgressProperty);
        AffectsMeasure<AnnotationCanvas>(SourceProperty);
        // Layer mutations fire Changed event; the window code-behind calls InvalidateVisual() there.
    }

    public Bitmap? Source
    {
        get => GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public AnnotationLayer? Layer
    {
        get => GetValue(LayerProperty);
        set => SetValue(LayerProperty, value);
    }

    public Annotation? InProgress
    {
        get => GetValue(InProgressProperty);
        set => SetValue(InProgressProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
        => Source?.Size ?? availableSize;

    protected override Size ArrangeOverride(Size finalSize)
        => Source?.Size ?? finalSize;

    public override void Render(DrawingContext ctx)
    {
        if (Source != null)
        {
            var rect = new Rect(Source.Size);
            ctx.DrawImage(Source, rect);
        }

        if (Layer != null)
            foreach (var a in Layer.Items)
                DrawAnnotation(ctx, a);

        if (InProgress != null)
            DrawAnnotation(ctx, InProgress);
    }

    private static void DrawAnnotation(DrawingContext ctx, Annotation a)
    {
        switch (a)
        {
            case ArrowAnnotation arrow:
                DrawArrow(ctx, arrow);
                break;

            case RectAnnotation rect:
                var pen = MakePen(a);
                var r = new Rect(
                    new Point(Math.Min(rect.X, rect.X + rect.Width),
                              Math.Min(rect.Y, rect.Y + rect.Height)),
                    new Size(Math.Abs(rect.Width), Math.Abs(rect.Height)));
                ctx.DrawRectangle(null, pen, r);
                break;

            case TextAnnotation text:
                var brush = new SolidColorBrush(Color.Parse(text.Color));
                var ft = new FormattedText(
                    text.Text,
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    Typeface.Default,
                    text.FontSize,
                    brush);
                ctx.DrawText(ft, new Point(text.X, text.Y));
                break;
        }
    }

    private static void DrawArrow(DrawingContext ctx, ArrowAnnotation a)
    {
        var pen   = MakePen(a);
        var start = new Point(a.X1, a.Y1);
        var end   = new Point(a.X2, a.Y2);
        ctx.DrawLine(pen, start, end);

        double dx = a.X2 - a.X1;
        double dy = a.Y2 - a.Y1;
        if (Math.Abs(dx) < 0.5 && Math.Abs(dy) < 0.5) return; // too short for arrowhead

        double angle     = Math.Atan2(dy, dx);
        const double len = 14;
        const double spread = Math.PI / 6; // 30°

        ctx.DrawLine(pen, end,
            new Point(a.X2 - len * Math.Cos(angle - spread),
                      a.Y2 - len * Math.Sin(angle - spread)));
        ctx.DrawLine(pen, end,
            new Point(a.X2 - len * Math.Cos(angle + spread),
                      a.Y2 - len * Math.Sin(angle + spread)));
    }

    private static Pen MakePen(Annotation a)
        => new(new SolidColorBrush(Color.Parse(a.Color)), a.StrokeWidth);
}
```

- [ ] **Step 2: Build to verify it compiles**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet build ShareX-Mac.sln 2>&1 | tail -5
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 3: Run all tests to confirm no regressions**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet test ShareXMac.Tests/ShareXMac.Tests.csproj 2>&1 | tail -3
```

Expected: 105 passed

- [ ] **Step 4: Commit**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && git add ShareXMac/Views/AnnotationCanvas.cs && git commit -m "feat: add AnnotationCanvas custom control"
```

---

### Task 3: AnnotationEditorViewModel

Observable view-model with injectable state for testability, drag lifecycle, and event-driven completion.

**Files:**
- Create: `ShareXMac/ViewModels/AnnotationEditorViewModel.cs`
- Create: `ShareXMac.Tests/AnnotationEditorViewModelTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `ShareXMac.Tests/AnnotationEditorViewModelTests.cs`:

```csharp
using Avalonia;
using ShareXMac.Models;
using ShareXMac.ViewModels;
using Xunit;

namespace ShareXMac.Tests;

// HeadlessAvaloniaFixture (defined in TestAppBuilder.cs) initialises Avalonia before tests,
// which is required to construct Bitmap objects in AnnotationEditorViewModel.
public class AnnotationEditorViewModelTests : IClassFixture<HeadlessAvaloniaFixture>
{
    // Minimal 1×1 white PNG (same bytes as PostCaptureViewModelTests)
    private static readonly byte[] MinimalPng =
    {
        0x89,0x50,0x4E,0x47,0x0D,0x0A,0x1A,0x0A,0x00,0x00,0x00,0x0D,
        0x49,0x48,0x44,0x52,0x00,0x00,0x00,0x01,0x00,0x00,0x00,0x01,
        0x08,0x02,0x00,0x00,0x00,0x90,0x77,0x53,0xDE,0x00,0x00,0x00,
        0x0C,0x49,0x44,0x41,0x54,0x78,0x9C,0x63,0xF8,0xFF,0xFF,0x3F,
        0x00,0x05,0xFE,0x02,0xFE,0x0D,0xEF,0x46,0xB8,0x00,0x00,0x00,
        0x00,0x49,0x45,0x4E,0x44,0xAE,0x42,0x60,0x82
    };

    private static AnnotationEditorViewModel MakeVm() => new(MinimalPng);

    [Fact]
    public void DefaultTool_IsArrow()
    {
        var vm = MakeVm();
        Assert.Equal(AnnotationTool.Arrow, vm.CurrentTool);
    }

    [Fact]
    public void StartUpdateDrag_SetsInProgressArrow()
    {
        var vm = MakeVm();
        vm.StartDrag(new Point(0, 0));
        vm.UpdateDrag(new Point(50, 50));
        var arrow = Assert.IsType<ArrowAnnotation>(vm.InProgressAnnotation);
        Assert.Equal(50, arrow.X2);
        Assert.Equal(50, arrow.Y2);
    }

    [Fact]
    public void EndDrag_CommitsArrowToLayer()
    {
        var vm = MakeVm();
        vm.StartDrag(new Point(0, 0));
        vm.EndDrag(new Point(100, 80));
        Assert.Equal(1, vm.Layer.Count);
        var arrow = Assert.IsType<ArrowAnnotation>(vm.Layer.Items[0]);
        Assert.Equal(100, arrow.X2);
    }

    [Fact]
    public void EndDrag_ClearsInProgressAnnotation()
    {
        var vm = MakeVm();
        vm.StartDrag(new Point(0, 0));
        vm.EndDrag(new Point(10, 10));
        Assert.Null(vm.InProgressAnnotation);
    }

    [Fact]
    public void UndoCommand_RemovesLastAnnotation()
    {
        var vm = MakeVm();
        vm.StartDrag(new Point(0, 0));
        vm.EndDrag(new Point(10, 10));
        Assert.Equal(1, vm.Layer.Count);
        vm.UndoCommand.Execute(null);
        Assert.Equal(0, vm.Layer.Count);
    }

    [Fact]
    public void RectangleTool_EndDrag_AddsRectToLayer()
    {
        var vm = MakeVm();
        vm.CurrentTool = AnnotationTool.Rectangle;
        vm.StartDrag(new Point(10, 20));
        vm.EndDrag(new Point(80, 60));
        var rect = Assert.IsType<RectAnnotation>(vm.Layer.Items[0]);
        Assert.Equal(10, rect.X);
        Assert.Equal(20, rect.Y);
        Assert.Equal(70, rect.Width);
        Assert.Equal(40, rect.Height);
    }

    [Fact]
    public void PlaceText_AddsTextAnnotation()
    {
        var vm = MakeVm();
        vm.PlaceText(new Point(30, 40), "Hello");
        var text = Assert.IsType<TextAnnotation>(vm.Layer.Items[0]);
        Assert.Equal("Hello", text.Text);
        Assert.Equal(30, text.X);
    }

    [Fact]
    public void PlaceText_EmptyString_DoesNotAdd()
    {
        var vm = MakeVm();
        vm.PlaceText(new Point(0, 0), "");
        Assert.Equal(0, vm.Layer.Count);
    }
}
```

- [ ] **Step 2: Run to confirm compile failure**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet test ShareXMac.Tests/ShareXMac.Tests.csproj --filter "AnnotationEditorViewModelTests" 2>&1 | tail -5
```

Expected: compile error (`AnnotationEditorViewModel` not found)

- [ ] **Step 3: Create AnnotationEditorViewModel.cs**

Create `ShareXMac/ViewModels/AnnotationEditorViewModel.cs`:

```csharp
using Avalonia;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShareXMac.Models;

namespace ShareXMac.ViewModels;

public partial class AnnotationEditorViewModel : ObservableObject
{
    private readonly Bitmap _originalBitmap;
    private Point? _dragStart;

    [ObservableProperty] private AnnotationTool _currentTool = AnnotationTool.Arrow;
    [ObservableProperty] private string         _currentColor = "#FF0000";
    [ObservableProperty] private double         _strokeWidth  = 3;
    [ObservableProperty] private Annotation?    _inProgressAnnotation;

    public AnnotationLayer Layer { get; } = new();
    public Bitmap OriginalBitmap => _originalBitmap;

    // Fires when the user clicks Done and the code-behind should flatten+close.
    public event Action? DoneRequested;
    // Fires when the user cancels; code-behind should close the window.
    public event Action? CancelRequested;
    // Fires with the flattened PNG bytes after the code-behind calls Complete().
    public event Action<byte[]>? Completed;

    public AnnotationEditorViewModel(byte[] imageData)
    {
        using var ms = new MemoryStream(imageData);
        _originalBitmap = new Bitmap(ms);
    }

    public void StartDrag(Point p) => _dragStart = p;

    public void UpdateDrag(Point p)
    {
        if (_dragStart == null) return;
        InProgressAnnotation = MakeAnnotation(_dragStart.Value, p);
    }

    public void EndDrag(Point p)
    {
        if (_dragStart == null) return;
        var a = MakeAnnotation(_dragStart.Value, p);
        if (a != null) Layer.Add(a);
        _dragStart = null;
        InProgressAnnotation = null;
    }

    public void PlaceText(Point p, string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        Layer.Add(new TextAnnotation(p.X, p.Y, text, CurrentColor));
    }

    // Called by the code-behind after RenderTargetBitmap flattening.
    public void Complete(byte[] flattenedBytes) => Completed?.Invoke(flattenedBytes);

    [RelayCommand]
    public void Undo()
    {
        InProgressAnnotation = null;
        _dragStart = null;
        Layer.Undo();
    }

    [RelayCommand]
    public void Done() => DoneRequested?.Invoke();

    [RelayCommand]
    public void Cancel() => CancelRequested?.Invoke();

    [RelayCommand]
    public void SetColor(string color) => CurrentColor = color;

    private Annotation? MakeAnnotation(Point start, Point end) => CurrentTool switch
    {
        AnnotationTool.Arrow     => new ArrowAnnotation(start.X, start.Y, end.X, end.Y, CurrentColor, StrokeWidth),
        AnnotationTool.Rectangle => new RectAnnotation(start.X, start.Y, end.X - start.X, end.Y - start.Y, CurrentColor, StrokeWidth),
        _                        => null // Text is placed via PlaceText, not drag
    };
}
```

- [ ] **Step 4: Run AnnotationEditorViewModelTests**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet test ShareXMac.Tests/ShareXMac.Tests.csproj --filter "AnnotationEditorViewModelTests" -v normal 2>&1 | tail -15
```

Expected: 8 passed

- [ ] **Step 5: Run all tests**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet test ShareXMac.Tests/ShareXMac.Tests.csproj 2>&1 | tail -3
```

Expected: 113 passed (105 + 8)

- [ ] **Step 6: Commit**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && git add ShareXMac/ViewModels/AnnotationEditorViewModel.cs ShareXMac.Tests/AnnotationEditorViewModelTests.cs && git commit -m "feat: add AnnotationEditorViewModel with drag lifecycle"
```

---

### Task 4: AnnotationEditorWindow + PostCapture integration

The editor window AXAML and code-behind, flattening via `RenderTargetBitmap`, and the "Annotate" button wired into `PostCaptureViewModel`/`PostCaptureWindow`.

**Files:**
- Create: `ShareXMac/Views/AnnotationEditorWindow.axaml`
- Create: `ShareXMac/Views/AnnotationEditorWindow.axaml.cs`
- Modify: `ShareXMac/ViewModels/PostCaptureViewModel.cs`
- Modify: `ShareXMac/Views/PostCaptureWindow.axaml`

- [ ] **Step 1: Create AnnotationEditorWindow.axaml**

Create `ShareXMac/Views/AnnotationEditorWindow.axaml`:

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="using:ShareXMac.ViewModels"
        xmlns:views="using:ShareXMac.Views"
        x:Class="ShareXMac.Views.AnnotationEditorWindow"
        x:DataType="vm:AnnotationEditorViewModel"
        Title="Annotate Screenshot"
        Width="900" Height="680"
        MinWidth="600" MinHeight="400"
        WindowStartupLocation="CenterScreen">

    <DockPanel>

        <!-- Toolbar -->
        <Border DockPanel.Dock="Top" Background="#FF2D2D2D" Padding="6,4">
            <StackPanel Orientation="Horizontal" Spacing="6" VerticalAlignment="Center">

                <!-- Tool buttons -->
                <ToggleButton Content="→ Arrow"
                              IsChecked="{Binding IsArrowTool}"
                              Padding="8,4" />
                <ToggleButton Content="□ Rect"
                              IsChecked="{Binding IsRectTool}"
                              Padding="8,4" />
                <ToggleButton Content="T Text"
                              IsChecked="{Binding IsTextTool}"
                              Padding="8,4" />

                <Separator Width="1" Height="24" Background="#FF555555" Margin="4,0" />

                <!-- Color presets — CommandParameter is #AARRGGBB -->
                <Button Width="24" Height="24" Background="#FFFF0000"
                        Command="{Binding SetColorCommand}" CommandParameter="#FFFF0000"
                        ToolTip.Tip="Red" />
                <Button Width="24" Height="24" Background="#FF0000FF"
                        Command="{Binding SetColorCommand}" CommandParameter="#FF0000FF"
                        ToolTip.Tip="Blue" />
                <Button Width="24" Height="24" Background="#FF00CC00"
                        Command="{Binding SetColorCommand}" CommandParameter="#FF00CC00"
                        ToolTip.Tip="Green" />
                <Button Width="24" Height="24" Background="#FFFFFF00"
                        Command="{Binding SetColorCommand}" CommandParameter="#FFFFFF00"
                        ToolTip.Tip="Yellow" />
                <Button Width="24" Height="24" Background="#FFFF8800"
                        Command="{Binding SetColorCommand}" CommandParameter="#FFFF8800"
                        ToolTip.Tip="Orange" />
                <Button Width="24" Height="24" Background="#FFFFFFFF" BorderBrush="#FF888888" BorderThickness="1"
                        Command="{Binding SetColorCommand}" CommandParameter="#FFFFFFFF"
                        ToolTip.Tip="White" />
                <Button Width="24" Height="24" Background="#FF000000"
                        Command="{Binding SetColorCommand}" CommandParameter="#FF000000"
                        ToolTip.Tip="Black" />

                <!-- Current color swatch -->
                <Border Width="24" Height="24" CornerRadius="3"
                        Background="{Binding CurrentColor}"
                        BorderBrush="#FF888888" BorderThickness="1"
                        ToolTip.Tip="Current color" />

                <Separator Width="1" Height="24" Background="#FF555555" Margin="4,0" />

                <!-- Stroke width -->
                <TextBlock Text="Width:" Foreground="White" VerticalAlignment="Center" />
                <NumericUpDown Value="{Binding StrokeWidth}"
                               Minimum="1" Maximum="20" Increment="1"
                               Width="70" />

                <Separator Width="1" Height="24" Background="#FF555555" Margin="4,0" />

                <Button Content="Undo"
                        Command="{Binding UndoCommand}"
                        Padding="8,4" />
            </StackPanel>
        </Border>

        <!-- Bottom action bar -->
        <Border DockPanel.Dock="Bottom" Background="#FF2D2D2D" Padding="8,6">
            <StackPanel Orientation="Horizontal" HorizontalAlignment="Right" Spacing="8">
                <Button Content="Cancel"
                        Command="{Binding CancelCommand}"
                        Padding="12,5" />
                <Button x:Name="DoneButton"
                        Content="Done"
                        Background="#FF0078D4" Foreground="White"
                        Padding="16,5" />
            </StackPanel>
        </Border>

        <!-- Canvas area with text overlay -->
        <ScrollViewer HorizontalScrollBarVisibility="Auto"
                      VerticalScrollBarVisibility="Auto"
                      Background="#FF1A1A1A">
            <Panel>
                <!-- The annotation canvas — pointer events wired in code-behind -->
                <views:AnnotationCanvas x:Name="AnnotationCanvasControl"
                    Source="{Binding OriginalBitmap}"
                    Layer="{Binding Layer}"
                    InProgress="{Binding InProgressAnnotation}"
                    Cursor="Cross" />

                <!-- Inline text input overlay (shown on text-tool click) -->
                <Canvas x:Name="TextOverlay" IsHitTestVisible="False">
                    <Border x:Name="TextInputBorder"
                            IsVisible="False"
                            Background="#FFEEEEEE"
                            Padding="2"
                            CornerRadius="2">
                        <TextBox x:Name="TextInputBox"
                                 Width="200"
                                 Background="White"
                                 Foreground="Black"
                                 AcceptsReturn="False"
                                 IsHitTestVisible="True" />
                    </Border>
                </Canvas>
            </Panel>
        </ScrollViewer>

    </DockPanel>
</Window>
```

- [ ] **Step 2: Create AnnotationEditorWindow.axaml.cs**

Create `ShareXMac/Views/AnnotationEditorWindow.axaml.cs`:

```csharp
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using ShareXMac.Models;
using ShareXMac.ViewModels;

namespace ShareXMac.Views;

public partial class AnnotationEditorWindow : Window
{
    private readonly AnnotationEditorViewModel _vm;
    private AnnotationCanvas _canvas = null!;
    private Border           _textBorder = null!;
    private TextBox          _textBox    = null!;
    private Point?           _pendingTextPoint;
    private bool             _dragging;

    public AnnotationEditorWindow(AnnotationEditorViewModel vm)
    {
        _vm = vm;
        InitializeComponent();
        DataContext = vm;

        _canvas     = this.FindControl<AnnotationCanvas>("AnnotationCanvasControl")!;
        _textBorder = this.FindControl<Border>("TextInputBorder")!;
        _textBox    = this.FindControl<TextBox>("TextInputBox")!;

        // Wire canvas pointer events
        _canvas.PointerPressed  += OnCanvasPointerPressed;
        _canvas.PointerMoved    += OnCanvasPointerMoved;
        _canvas.PointerReleased += OnCanvasPointerReleased;

        // Text input: commit on Enter, cancel on Escape
        _textBox.KeyDown += OnTextBoxKeyDown;

        // Invalidate canvas when layer changes
        vm.Layer.Changed += () => _canvas.InvalidateVisual();

        // Done: code-behind flattens via RenderTargetBitmap, then calls vm.Complete()
        vm.DoneRequested   += FlattenAndClose;
        vm.CancelRequested += Close;

        // Wire Done button (not a RelayCommand — needs access to visual tree)
        this.FindControl<Button>("DoneButton")!.Click += (_, _) => vm.DoneCommand.Execute(null);

        // IsArrowTool / IsRectTool / IsTextTool derived properties
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(vm.CurrentTool))
            {
                vm.OnCurrentToolChanged();
            }
        };
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    // --- Pointer events ---

    private void OnCanvasPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(_canvas).Properties.IsLeftButtonPressed) return;
        var p = e.GetPosition(_canvas);

        if (_vm.CurrentTool == AnnotationTool.Text)
        {
            ShowTextInput(p);
        }
        else
        {
            _dragging = true;
            _vm.StartDrag(p);
        }
        e.Handled = true;
    }

    private void OnCanvasPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_dragging) return;
        _vm.UpdateDrag(e.GetPosition(_canvas));
    }

    private void OnCanvasPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_dragging) return;
        _dragging = false;
        _vm.EndDrag(e.GetPosition(_canvas));
    }

    // --- Inline text input ---

    private void ShowTextInput(Point canvasPoint)
    {
        _pendingTextPoint = canvasPoint;
        Canvas.SetLeft(_textBorder, canvasPoint.X);
        Canvas.SetTop(_textBorder,  canvasPoint.Y);
        _textBorder.IsVisible   = true;
        _textBorder.IsHitTestVisible = true;
        var overlay = this.FindControl<Canvas>("TextOverlay")!;
        overlay.IsHitTestVisible = true;
        _textBox.Text = "";
        Dispatcher.UIThread.Post(() => _textBox.Focus());
    }

    private void CommitText()
    {
        if (_pendingTextPoint.HasValue)
            _vm.PlaceText(_pendingTextPoint.Value, _textBox.Text ?? "");
        HideTextInput();
    }

    private void HideTextInput()
    {
        _textBorder.IsVisible = false;
        _textBorder.IsHitTestVisible = false;
        var overlay = this.FindControl<Canvas>("TextOverlay")!;
        overlay.IsHitTestVisible = false;
        _pendingTextPoint = null;
        _canvas.InvalidateVisual();
    }

    private void OnTextBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)  { CommitText();    e.Handled = true; }
        if (e.Key == Key.Escape) { HideTextInput(); e.Handled = true; }
    }

    // --- Flatten and close ---

    private void FlattenAndClose()
    {
        // Render the annotation canvas to a bitmap at the original image's DIP size.
        var size = _vm.OriginalBitmap.Size;
        var pixelSize = new PixelSize(Math.Max(1, (int)size.Width), Math.Max(1, (int)size.Height));
        using var rtb = new RenderTargetBitmap(pixelSize, new Vector(96, 96));
        rtb.Render(_canvas);
        using var ms = new MemoryStream();
        rtb.Save(ms);
        _vm.Complete(ms.ToArray());
        Close();
    }
}
```

- [ ] **Step 3: Add tool toggle properties to AnnotationEditorViewModel**

Read `ShareXMac/ViewModels/AnnotationEditorViewModel.cs`. Add after the `SetColor` command:

```csharp
// Derived tool toggle properties — bound to ToggleButton.IsChecked in AXAML
public bool IsArrowTool
{
    get => CurrentTool == AnnotationTool.Arrow;
    set { if (value) CurrentTool = AnnotationTool.Arrow; }
}

public bool IsRectTool
{
    get => CurrentTool == AnnotationTool.Rectangle;
    set { if (value) CurrentTool = AnnotationTool.Rectangle; }
}

public bool IsTextTool
{
    get => CurrentTool == AnnotationTool.Text;
    set { if (value) CurrentTool = AnnotationTool.Text; }
}

// Called by the code-behind when CurrentTool changes, to update IsArrowTool/IsRectTool/IsTextTool
public void OnCurrentToolChanged()
{
    OnPropertyChanged(nameof(IsArrowTool));
    OnPropertyChanged(nameof(IsRectTool));
    OnPropertyChanged(nameof(IsTextTool));
}
```

Also add `partial void OnCurrentToolChanged(AnnotationTool value)` to trigger automatically:

```csharp
partial void OnCurrentToolChanged(AnnotationTool value)
{
    OnPropertyChanged(nameof(IsArrowTool));
    OnPropertyChanged(nameof(IsRectTool));
    OnPropertyChanged(nameof(IsTextTool));
}
```

If you add `partial void OnCurrentToolChanged`, remove the `public void OnCurrentToolChanged()` method and the `PropertyChanged` subscription in the code-behind (they'd duplicate each other). Use the `partial void` approach — it's called automatically by the CommunityToolkit source generator when `CurrentTool` changes.

The final `AnnotationEditorViewModel.cs` with these additions (complete file):

```csharp
using Avalonia;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShareXMac.Models;

namespace ShareXMac.ViewModels;

public partial class AnnotationEditorViewModel : ObservableObject
{
    private readonly Bitmap _originalBitmap;
    private Point? _dragStart;

    [ObservableProperty] private AnnotationTool _currentTool = AnnotationTool.Arrow;
    [ObservableProperty] private string         _currentColor = "#FF0000";
    [ObservableProperty] private double         _strokeWidth  = 3;
    [ObservableProperty] private Annotation?    _inProgressAnnotation;

    public AnnotationLayer Layer { get; } = new();
    public Bitmap OriginalBitmap => _originalBitmap;

    public event Action? DoneRequested;
    public event Action? CancelRequested;
    public event Action<byte[]>? Completed;

    public AnnotationEditorViewModel(byte[] imageData)
    {
        using var ms = new MemoryStream(imageData);
        _originalBitmap = new Bitmap(ms);
    }

    partial void OnCurrentToolChanged(AnnotationTool value)
    {
        OnPropertyChanged(nameof(IsArrowTool));
        OnPropertyChanged(nameof(IsRectTool));
        OnPropertyChanged(nameof(IsTextTool));
    }

    public bool IsArrowTool
    {
        get => CurrentTool == AnnotationTool.Arrow;
        set { if (value) CurrentTool = AnnotationTool.Arrow; }
    }

    public bool IsRectTool
    {
        get => CurrentTool == AnnotationTool.Rectangle;
        set { if (value) CurrentTool = AnnotationTool.Rectangle; }
    }

    public bool IsTextTool
    {
        get => CurrentTool == AnnotationTool.Text;
        set { if (value) CurrentTool = AnnotationTool.Text; }
    }

    public void StartDrag(Point p) => _dragStart = p;

    public void UpdateDrag(Point p)
    {
        if (_dragStart == null) return;
        InProgressAnnotation = MakeAnnotation(_dragStart.Value, p);
    }

    public void EndDrag(Point p)
    {
        if (_dragStart == null) return;
        var a = MakeAnnotation(_dragStart.Value, p);
        if (a != null) Layer.Add(a);
        _dragStart = null;
        InProgressAnnotation = null;
    }

    public void PlaceText(Point p, string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        Layer.Add(new TextAnnotation(p.X, p.Y, text, CurrentColor));
    }

    public void Complete(byte[] flattenedBytes) => Completed?.Invoke(flattenedBytes);

    [RelayCommand]
    public void Undo()
    {
        InProgressAnnotation = null;
        _dragStart = null;
        Layer.Undo();
    }

    [RelayCommand]
    public void Done() => DoneRequested?.Invoke();

    [RelayCommand]
    public void Cancel() => CancelRequested?.Invoke();

    [RelayCommand]
    public void SetColor(string color) => CurrentColor = color;

    private Annotation? MakeAnnotation(Point start, Point end) => CurrentTool switch
    {
        AnnotationTool.Arrow     => new ArrowAnnotation(start.X, start.Y, end.X, end.Y, CurrentColor, StrokeWidth),
        AnnotationTool.Rectangle => new RectAnnotation(start.X, start.Y, end.X - start.X, end.Y - start.Y, CurrentColor, StrokeWidth),
        _                        => null
    };
}
```

Also simplify the code-behind to remove the `PropertyChanged` subscription since `partial void OnCurrentToolChanged` now handles it:

In `AnnotationEditorWindow.axaml.cs`, remove these lines from the constructor:
```csharp
// Remove:
vm.PropertyChanged += (_, e) =>
{
    if (e.PropertyName == nameof(vm.CurrentTool))
        vm.OnCurrentToolChanged();
};
```

- [ ] **Step 4: Modify PostCaptureViewModel — make Thumbnail observable, add AnnotateCommand**

Read `ShareXMac/ViewModels/PostCaptureViewModel.cs` in full first.

The complete modified file (changes: `Thumbnail` becomes `[ObservableProperty]`, `Dispose` updated, `AnnotateCommand` added, `using Avalonia.Threading` added):

```csharp
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShareXMac.Models;
using ShareXMac.ScreenCaptureLib;
using ShareXMac.Services;
using ShareXMac.Views;
using System.Diagnostics;

namespace ShareXMac.ViewModels;

public partial class PostCaptureViewModel : ObservableObject, IDisposable
{
    public string FilePath { get; }
    public int AutoDismissSeconds { get; init; } = 8;

    [ObservableProperty] private Bitmap   _thumbnail = null!;
    [ObservableProperty] private bool     _isUploading;
    [ObservableProperty] private string?  _uploadedUrl;

    private byte[] _imageData;
    private readonly UploadService _uploadService;
    private readonly AppSettings _settings;

    public event Action? CloseRequested;

    public PostCaptureViewModel(CaptureResult result, UploadService uploadService, AppSettings settings)
    {
        FilePath       = result.FilePath;
        _imageData     = result.ImageData;
        _uploadService = uploadService;
        _settings      = settings;
        using var ms = new MemoryStream(result.ImageData);
        _thumbnail = Bitmap.DecodeToWidth(ms, 360);
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
            IsUploading = false;
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
    private void Annotate()
    {
        Dispatcher.UIThread.Post(() =>
        {
            var editorVm = new AnnotationEditorViewModel(_imageData);
            var editorWin = new AnnotationEditorWindow(editorVm);
            editorVm.Completed += bytes =>
            {
                _imageData = bytes;
                var old = _thumbnail;
                using var ms = new MemoryStream(bytes);
                Thumbnail = Bitmap.DecodeToWidth(ms, 360);
                old?.Dispose();
            };
            editorVm.CancelRequested += editorWin.Close;
            editorWin.Show();
        });
    }

    [RelayCommand]
    private void Dismiss() => CloseRequested?.Invoke();

    public void Dispose() => _thumbnail?.Dispose();
}
```

- [ ] **Step 5: Add Annotate button to PostCaptureWindow.axaml**

Read `ShareXMac/Views/PostCaptureWindow.axaml`. In the action buttons `StackPanel` (Grid Row="1"), add the "Annotate" button after "Open in Finder" and before "Upload":

```xml
<Button Content="Annotate"
        Command="{Binding AnnotateCommand}"
        Background="#FF6B2FBE" Foreground="White"
        Padding="10,5" />
```

The full updated StackPanel:

```xml
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
    <Button Content="Annotate"
            Command="{Binding AnnotateCommand}"
            Background="#FF6B2FBE" Foreground="White"
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
```

- [ ] **Step 6: Build**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet build ShareX-Mac.sln 2>&1 | tail -8
```

Expected: `Build succeeded. 0 Error(s)`

Common build errors to fix:
- If AXAML binding `{Binding CurrentColor}` on a `Border.Background` fails at compile time, change it to `{Binding CurrentColor, Converter={x:Static SolidColorBrushConverter.Instance}}`. But try the direct binding first — Avalonia can coerce a hex string to a `Brush`.
- If `views:AnnotationCanvas` is unknown in AXAML, ensure the namespace `xmlns:views="using:ShareXMac.Views"` is at the top of the Window element.
- If `NumericUpDown` is not found, it is in `Avalonia.Controls` — no additional package needed for Avalonia 11.

- [ ] **Step 7: Run all tests**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet test ShareXMac.Tests/ShareXMac.Tests.csproj 2>&1 | tail -3
```

Expected: 113 passed (no regressions — Task 4 adds no new tests)

- [ ] **Step 8: Manual smoke test**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && dotnet run --project ShareXMac/ShareXMac.csproj
```

1. Trigger a capture (tray → Capture Region, draw a selection)
2. PostCaptureWindow appears — verify "Annotate" button is visible (purple)
3. Click "Annotate" — AnnotationEditorWindow opens with the screenshot
4. Select Arrow tool → drag across the image → red arrow appears
5. Select Rect tool → drag a rectangle → outlined rectangle appears
6. Select Text tool → click on the image → text input box appears → type "Hello" → Enter → text appears on image
7. Click Undo — last annotation removed
8. Click Done — editor closes, PostCaptureWindow thumbnail updates with the annotated image
9. Reopen editor via Annotate again — starts fresh from the annotated bytes (cumulative)
10. Click Cancel — editor closes, PostCaptureWindow unchanged

- [ ] **Step 9: Commit and push**

```bash
cd /Users/austin/Documents/Dev/Projects/ShareX-Mac && git add \
  ShareXMac/Views/AnnotationEditorWindow.axaml \
  ShareXMac/Views/AnnotationEditorWindow.axaml.cs \
  ShareXMac/ViewModels/AnnotationEditorViewModel.cs \
  ShareXMac/ViewModels/PostCaptureViewModel.cs \
  ShareXMac/Views/PostCaptureWindow.axaml \
  && git commit -m "feat: add AnnotationEditorWindow with PostCapture integration" \
  && git push origin master
```

---

## Plan Complete

After all 4 tasks:

- **Annotation editor** opens from PostCaptureWindow via "Annotate" (purple button)
- Arrow tool: drag to draw a line with an arrowhead at the endpoint
- Rectangle tool: drag to draw an unfilled rectangle
- Text tool: click to place inline text input, Enter to commit
- Color picker: 7 preset color buttons in the toolbar
- Stroke width: numeric spinner
- Undo: removes the most recent annotation
- Done: flattens annotations onto the image via `RenderTargetBitmap`, updates PostCaptureWindow thumbnail
- Cancel: closes editor without changes
- **113 tests passing** (96 baseline + 9 AnnotationLayer + 8 AnnotationEditorViewModel)

**Next plan:** Plan 9 — to be determined.
