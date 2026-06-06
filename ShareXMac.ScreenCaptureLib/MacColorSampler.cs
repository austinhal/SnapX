using System.Runtime.InteropServices;
using ShareXMac.Models;
using ShareXMac.ScreenCaptureLib.ObjC;

namespace ShareXMac.ScreenCaptureLib;

public static class MacColorSampler
{
    [StructLayout(LayoutKind.Sequential)]
    private struct CGRect { public double X, Y, Width, Height; }

    // NSEvent.mouseLocation — Y=0 at bottom-left, Y increases upward (same as CoreGraphics)
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

    // ObjCRuntime doesn't have an NSPoint-returning variant, so we add one here.
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
