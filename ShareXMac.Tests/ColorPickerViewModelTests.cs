using Avalonia;
using Avalonia.Headless;
using ShareXMac.Models;
using ShareXMac.ViewModels;
using Xunit;

namespace ShareXMac.Tests;

public class ColorPickerViewModelTests
{
    private static readonly Lazy<bool> AvaloniaInitialized = new Lazy<bool>(() =>
    {
        try
        {
            AppBuilder.Configure<Application>()
                .UseHeadless(new AvaloniaHeadlessPlatformOptions())
                .SetupWithoutStarting();
            return true;
        }
        catch
        {
            return false;
        }
    });

    private static SampledColor[] AllColor(byte r, byte g, byte b)
        => Enumerable.Repeat(new SampledColor(r, g, b), 15 * 15).ToArray();

    public ColorPickerViewModelTests()
    {
        _ = AvaloniaInitialized.Value;
    }

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
