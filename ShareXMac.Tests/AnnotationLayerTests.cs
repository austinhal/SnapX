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
