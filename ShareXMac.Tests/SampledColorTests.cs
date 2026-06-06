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
