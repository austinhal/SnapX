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

    private static nint GetGeneralPasteboard()
    {
        EnsureFrameworkLoaded();
        return ObjCRuntime.Send(ObjCRuntime.GetClass("NSPasteboard"), "generalPasteboard");
    }

    public static void SetText(string text)
    {
        nint pb = GetGeneralPasteboard();
        ObjCRuntime.Send(pb, "clearContents");
        nint nsStr = ObjCRuntime.ToNSString(text);
        nint array = ObjCRuntime.ArrayWithObject(nsStr);
        ObjCRuntime.Send(pb, "writeObjects:", array);
    }

    public static string? GetText()
    {
        nint pb = GetGeneralPasteboard();
        nint typeStr = ObjCRuntime.ToNSString(NSPasteboardTypeString);
        nint result = ObjCRuntime.Send(pb, "stringForType:", typeStr);
        return ObjCRuntime.ToManagedString(result);
    }

    public static bool ContainsText()
    {
        nint pb = GetGeneralPasteboard();
        nint typeStr = ObjCRuntime.ToNSString(NSPasteboardTypeString);
        nint types = ObjCRuntime.ArrayWithObject(typeStr);
        nint available = ObjCRuntime.Send(pb, "availableTypeFromArray:", types);
        return available != 0;
    }

    public static bool ContainsImage()
    {
        nint pb = GetGeneralPasteboard();
        nint typeStr = ObjCRuntime.ToNSString(NSPasteboardTypePNG);
        nint types = ObjCRuntime.ArrayWithObject(typeStr);
        nint available = ObjCRuntime.Send(pb, "availableTypeFromArray:", types);
        return available != 0;
    }

    public static void Clear()
    {
        nint pb = GetGeneralPasteboard();
        ObjCRuntime.Send(pb, "clearContents");
    }
}
