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

    [ObservableProperty] private Bitmap?         _magnifiedView;
    [ObservableProperty] private string          _hex          = "#000000";
    [ObservableProperty] private byte            _r, _g, _b;
    [ObservableProperty] private double          _hue, _saturation, _value;
    [ObservableProperty] private SolidColorBrush _swatchBrush  = new(Colors.Black);

    // Production constructor: samples from the real screen
    public ColorPickerViewModel() : this(() => MacColorSampler.SampleRegion(7)) { }

    // Test constructor: inject a fixed pixel array
    public ColorPickerViewModel(Func<SampledColor[]> sample) => _sample = sample;

    public void Refresh()
    {
        SampledColor[] pixels = _sample();
        int size   = 15;
        int center = size / 2 * size + size / 2;
        SampledColor c = center < pixels.Length ? pixels[center] : new SampledColor(0, 0, 0);

        R = c.R; G = c.G; B = c.B;
        Hex = c.Hex;
        var (h, s, v) = c.ToHsv();
        Hue = h; Saturation = s; Value = v;
        SwatchBrush = new SolidColorBrush(new Color(255, c.R, c.G, c.B));

        try
        {
            MagnifiedView = BuildMagnifiedBitmap(pixels, size);
        }
        catch
        {
            // Avalonia platform may not be initialized in unit test environments
            MagnifiedView = null;
        }
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
