using Avalonia;
using ShareXMac.Models;
using ShareXMac.ViewModels;
using Xunit;

namespace ShareXMac.Tests;

// HeadlessAvaloniaFixture (defined in TestAppBuilder.cs) initialises Avalonia before tests,
// which is required to construct Bitmap objects in AnnotationEditorViewModel.
public class AnnotationEditorViewModelTests : IClassFixture<HeadlessAvaloniaFixture>
{
    // Minimal 1×1 white PNG
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
