using ShareXMac.ScreenCaptureLib.ObjC;

namespace ShareXMac.ScreenCaptureLib;

public static class MacClipboard
{
    private const string NSPasteboardTypeString = "public.utf8-plain-text";
    private const string NSPasteboardTypePNG    = "public.png";

    private static bool s_frameworkLoaded;

    private static void EnsureFrameworkLoaded()
    {
        if (s_frameworkLoaded) return;
        ObjCRuntime.dlopen("/System/Library/Frameworks/AppKit.framework/AppKit", 1);
        s_frameworkLoaded = true;
    }

    private static nint GetGeneralPasteboard() =>
        ObjCRuntime.Send(ObjCRuntime.GetClass("NSPasteboard"), "generalPasteboard");

    private static nint CreatePool() =>
        ObjCRuntime.Send(ObjCRuntime.Send(ObjCRuntime.GetClass("NSAutoreleasePool"), "alloc"), "init");

    private static void DrainPool(nint pool) =>
        ObjCRuntime.Send(pool, "drain");

    public static void SetText(string text)
    {
        EnsureFrameworkLoaded();
        nint pool = CreatePool();
        try
        {
            nint pb = GetGeneralPasteboard();
            ObjCRuntime.Send(pb, "clearContents");
            nint nsStr = ObjCRuntime.ToNSString(text);
            nint array = ObjCRuntime.ArrayWithObject(nsStr);
            ObjCRuntime.Send(pb, "writeObjects:", array);
            ObjCRuntime.Send(nsStr, "release");
        }
        finally { DrainPool(pool); }
    }

    public static string? GetText()
    {
        EnsureFrameworkLoaded();
        nint pool = CreatePool();
        try
        {
            nint pb = GetGeneralPasteboard();
            nint typeStr = ObjCRuntime.ToNSString(NSPasteboardTypeString);
            nint result = ObjCRuntime.Send(pb, "stringForType:", typeStr);
            return ObjCRuntime.ToManagedString(result);
        }
        finally { DrainPool(pool); }
    }

    public static bool ContainsText()
    {
        EnsureFrameworkLoaded();
        nint pool = CreatePool();
        try
        {
            nint pb = GetGeneralPasteboard();
            nint typeStr = ObjCRuntime.ToNSString(NSPasteboardTypeString);
            nint types = ObjCRuntime.ArrayWithObject(typeStr);
            nint available = ObjCRuntime.Send(pb, "availableTypeFromArray:", types);
            return available != 0;
        }
        finally { DrainPool(pool); }
    }

    public static bool ContainsImage()
    {
        EnsureFrameworkLoaded();
        nint pool = CreatePool();
        try
        {
            nint pb = GetGeneralPasteboard();
            nint typeStr = ObjCRuntime.ToNSString(NSPasteboardTypePNG);
            nint types = ObjCRuntime.ArrayWithObject(typeStr);
            nint available = ObjCRuntime.Send(pb, "availableTypeFromArray:", types);
            return available != 0;
        }
        finally { DrainPool(pool); }
    }

    public static void Clear()
    {
        EnsureFrameworkLoaded();
        nint pool = CreatePool();
        try { ObjCRuntime.Send(GetGeneralPasteboard(), "clearContents"); }
        finally { DrainPool(pool); }
    }

    public static unsafe void SetImage(byte[] png)
    {
        if (png.Length == 0) return;
        EnsureFrameworkLoaded();
        nint pool = CreatePool();
        try
        {
            nint nsData;
            fixed (byte* ptr = png)
            {
                nint alloc = ObjCRuntime.Send(ObjCRuntime.GetClass("NSData"), "alloc");
                nsData = ObjCRuntime.objc_msgSend_bytes(alloc,
                    ObjCRuntime.Sel("initWithBytes:length:"),
                    (nint)ptr, (nuint)png.Length);
            }
            nint nsImage = ObjCRuntime.Send(
                ObjCRuntime.Send(ObjCRuntime.GetClass("NSImage"), "alloc"),
                "initWithData:", nsData);
            ObjCRuntime.Send(nsData, "release");
            if (nsImage == 0) return;
            nint pb = GetGeneralPasteboard();
            ObjCRuntime.Send(pb, "clearContents");
            ObjCRuntime.Send(pb, "writeObjects:", ObjCRuntime.ArrayWithObject(nsImage));
            ObjCRuntime.Send(nsImage, "release");
        }
        finally { DrainPool(pool); }
    }
}
