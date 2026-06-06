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

    [Fact]
    public void ToHsv_Yellow_Hue60()
    {
        var (h, s, v) = new SampledColor(255, 255, 0).ToHsv();
        Assert.Equal(60, h); Assert.Equal(100, s); Assert.Equal(100, v);
    }

    [Fact]
    public void ToHsv_Cyan_Hue180()
    {
        var (h, s, v) = new SampledColor(0, 255, 255).ToHsv();
        Assert.Equal(180, h); Assert.Equal(100, s); Assert.Equal(100, v);
    }

    [Fact]
    public void ToHsv_Magenta_Hue300()
    {
        var (h, s, v) = new SampledColor(255, 0, 255).ToHsv();
        Assert.Equal(300, h); Assert.Equal(100, s); Assert.Equal(100, v);
    }

    [Fact]
    public void ToHsv_NearBoundaryRed_HueNotWrapsTo360()
    {
        // (255, 1, 0) is red-dominant — hue should round to 0, not 360
        var (h, s, v) = new SampledColor(255, 1, 0).ToHsv();
        Assert.True(h < 360, $"Expected h < 360 but got {h}");
        Assert.Equal(100, s);
        Assert.Equal(100, v);
    }
}
