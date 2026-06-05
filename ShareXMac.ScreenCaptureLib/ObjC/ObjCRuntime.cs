using System.Runtime.InteropServices;

namespace ShareXMac.ScreenCaptureLib.ObjC;

internal static class ObjCRuntime
{
    const string ObjCLib = "libobjc.dylib";
    const string Dl = "libdl.dylib";

    [DllImport(ObjCLib)] static extern nint objc_getClass(string name);
    [DllImport(ObjCLib)] static extern nint sel_registerName(string name);

    [DllImport(ObjCLib, EntryPoint = "objc_msgSend")]
    static extern nint objc_msgSend0(nint receiver, nint sel);

    [DllImport(ObjCLib, EntryPoint = "objc_msgSend")]
    static extern nint objc_msgSend1(nint receiver, nint sel, nint arg1);

    [DllImport(ObjCLib, EntryPoint = "objc_msgSend")]
    static extern nint objc_msgSend2(nint receiver, nint sel, nint arg1, nint arg2);

    [DllImport(ObjCLib, EntryPoint = "objc_msgSend")]
    static extern nint objc_msgSend3(nint receiver, nint sel, nint a1, nint a2, nint a3);

    [DllImport(ObjCLib, EntryPoint = "objc_msgSend")]
    static extern nint objc_msgSend_str(nint receiver, nint sel,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string arg1);

    [DllImport(ObjCLib, EntryPoint = "objc_msgSend")]
    internal static extern nint objc_msgSend_bytes(nint receiver, nint sel,
        nint bytes, nuint length);

    [DllImport(ObjCLib, EntryPoint = "objc_msgSend")]
    static extern bool objc_msgSend_bool(nint receiver, nint sel);

    [DllImport(ObjCLib, EntryPoint = "objc_msgSend")]
    internal static extern bool objc_msgSend_perform(nint receiver, nint sel,
        nint requests, ref nint error);

    [DllImport(ObjCLib, EntryPoint = "objc_msgSend")]
    internal static extern nint objc_msgSend_nint(nint receiver, nint sel, nint arg);

    [DllImport(Dl)] internal static extern nint dlopen(string path, int flags);
    [DllImport(Dl)] internal static extern nint dlsym(nint handle, string symbol);

    internal static nint GetClass(string name) => objc_getClass(name);
    internal static nint Sel(string name) => sel_registerName(name);

    internal static nint Send(nint obj, string sel)
        => objc_msgSend0(obj, Sel(sel));

    internal static nint Send(nint obj, string sel, nint arg)
        => objc_msgSend1(obj, Sel(sel), arg);

    internal static nint Send(nint obj, string sel, nint a1, nint a2)
        => objc_msgSend2(obj, Sel(sel), a1, a2);

    internal static nint Send(nint obj, string sel, nint a1, nint a2, nint a3)
        => objc_msgSend3(obj, Sel(sel), a1, a2, a3);

    internal static nint Send(nint obj, string sel, string arg)
        => objc_msgSend_str(obj, Sel(sel), arg);

    internal static bool SendBool(nint obj, string sel)
        => objc_msgSend_bool(obj, Sel(sel));

    internal static nint SendInt(nint obj, string sel, nint arg)
        => objc_msgSend_nint(obj, Sel(sel), arg);

    internal static nint ToNSString(string text)
    {
        nint alloc = Send(GetClass("NSString"), "alloc");
        return objc_msgSend_str(alloc, Sel("initWithUTF8String:"), text);
    }

    internal static string? ToManagedString(nint nsStr)
    {
        if (nsStr == 0) return null;
        nint cstr = Send(nsStr, "UTF8String");
        return Marshal.PtrToStringUTF8(cstr);
    }

    internal static nint ArrayWithObject(nint obj)
        => objc_msgSend1(GetClass("NSArray"), Sel("arrayWithObject:"), obj);

    internal static nint ArrayCount(nint array)
        => Send(array, "count");

    internal static nint ArrayObjectAt(nint array, nint index)
        => objc_msgSend1(array, Sel("objectAtIndex:"), index);
}
