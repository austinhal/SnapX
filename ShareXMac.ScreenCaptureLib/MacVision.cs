using System.Runtime.InteropServices;
using ShareXMac.ScreenCaptureLib.ObjC;

namespace ShareXMac.ScreenCaptureLib;

internal static class MacVision
{
    [StructLayout(LayoutKind.Sequential)]
    private struct BlockDescriptor
    {
        public nuint Reserved;
        public nuint Size;
    }

    [StructLayout(LayoutKind.Sequential)]
    private unsafe struct BlockLiteral
    {
        public nint Isa;
        public int Flags;
        public int Reserved;
        public nint Invoke;
        public BlockDescriptor* Descriptor;
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private unsafe delegate void CompletionBlockInvoke(BlockLiteral* block, nint request, nint error);

    // ThreadStatic so concurrent calls don't clobber each other
    [ThreadStatic] private static nint t_pendingResults;

    // Static delegate + function pointer — must outlive any block instance
    private static readonly CompletionBlockInvoke s_completionDelegate;
    private static readonly nint s_completionFuncPtr;

    static unsafe MacVision()
    {
        s_completionDelegate = CompletionCallback;
        s_completionFuncPtr = Marshal.GetFunctionPointerForDelegate(s_completionDelegate);
    }

    private static unsafe void CompletionCallback(BlockLiteral* block, nint request, nint error)
    {
        t_pendingResults = ObjCRuntime.Send(request, "results");
    }

    private static bool s_frameworkLoaded;

    private static void EnsureFrameworkLoaded()
    {
        if (s_frameworkLoaded) return;
        ObjCRuntime.dlopen("/System/Library/Frameworks/Vision.framework/Vision", 1);
        s_frameworkLoaded = true;
    }

    public static unsafe Task<string?> RecognizeTextAsync(byte[] imageData)
    {
        if (imageData.Length == 0) return Task.FromResult<string?>(null);

        EnsureFrameworkLoaded();

        // NSAutoreleasePool so autoreleased ObjC objects are reclaimed promptly
        nint pool = ObjCRuntime.Send(
            ObjCRuntime.Send(ObjCRuntime.GetClass("NSAutoreleasePool"), "alloc"), "init");

        // 1. Create NSData
        nint nsData;
        fixed (byte* ptr = imageData)
        {
            nint alloc = ObjCRuntime.Send(ObjCRuntime.GetClass("NSData"), "alloc");
            nsData = ObjCRuntime.objc_msgSend_bytes(alloc,
                ObjCRuntime.Sel("initWithBytes:length:"),
                (nint)ptr, (nuint)imageData.Length);
        }
        if (nsData == 0)
        {
            ObjCRuntime.Send(pool, "drain");
            return Task.FromResult<string?>(null);
        }

        // 2. NSData -> NSImage -> TIFFRepresentation -> NSBitmapImageRep -> CGImage
        nint nsImage = ObjCRuntime.Send(
            ObjCRuntime.Send(ObjCRuntime.GetClass("NSImage"), "alloc"),
            "initWithData:", nsData);
        ObjCRuntime.Send(nsData, "release");
        if (nsImage == 0)
        {
            ObjCRuntime.Send(pool, "drain");
            return Task.FromResult<string?>(null);
        }

        nint tiffData = ObjCRuntime.Send(nsImage, "TIFFRepresentation");
        // tiffData is autoreleased — no explicit release needed

        if (tiffData == 0)
        {
            ObjCRuntime.Send(nsImage, "release");
            ObjCRuntime.Send(pool, "drain");
            return Task.FromResult<string?>(null);
        }

        nint bitmapRep = ObjCRuntime.Send(
            ObjCRuntime.Send(ObjCRuntime.GetClass("NSBitmapImageRep"), "alloc"),
            "initWithData:", tiffData);
        if (bitmapRep == 0)
        {
            ObjCRuntime.Send(nsImage, "release");
            ObjCRuntime.Send(pool, "drain");
            return Task.FromResult<string?>(null);
        }

        nint cgImage = ObjCRuntime.Send(bitmapRep, "CGImage");
        // cgImage is owned by bitmapRep — do not release separately

        if (cgImage == 0)
        {
            ObjCRuntime.Send(bitmapRep, "release");
            ObjCRuntime.Send(nsImage, "release");
            ObjCRuntime.Send(pool, "drain");
            return Task.FromResult<string?>(null);
        }

        // 3. VNImageRequestHandler with CGImage — retains cgImage internally
        nint emptyDict = ObjCRuntime.Send(ObjCRuntime.GetClass("NSDictionary"), "dictionary");
        nint handler = ObjCRuntime.Send(
            ObjCRuntime.Send(ObjCRuntime.GetClass("VNImageRequestHandler"), "alloc"),
            "initWithCGImage:options:", cgImage, emptyDict);

        // bitmapRep (and nsImage) can be released now — handler retains cgImage
        ObjCRuntime.Send(bitmapRep, "release");
        ObjCRuntime.Send(nsImage, "release");

        if (handler == 0)
        {
            ObjCRuntime.Send(pool, "drain");
            return Task.FromResult<string?>(null);
        }

        // 4. Build global ObjC block
        var descriptor = new BlockDescriptor
        {
            Reserved = 0,
            Size = (nuint)sizeof(BlockLiteral)
        };
        BlockDescriptor* descPtr = (BlockDescriptor*)Marshal.AllocHGlobal(sizeof(BlockDescriptor));
        *descPtr = descriptor;

        nint libObjC = ObjCRuntime.dlopen("libobjc.dylib", 1);
        nint isaPtr = ObjCRuntime.dlsym(libObjC, "_NSConcreteGlobalBlock");

        var blockLit = new BlockLiteral
        {
            Isa = isaPtr,
            Flags = 0x10000000, // BLOCK_IS_GLOBAL only — no signature string in descriptor
            Reserved = 0,
            Invoke = s_completionFuncPtr,
            Descriptor = descPtr
        };
        BlockLiteral* blockPtr = (BlockLiteral*)Marshal.AllocHGlobal(sizeof(BlockLiteral));
        *blockPtr = blockLit;

        // 5. Create VNRecognizeTextRequest
        nint request = ObjCRuntime.Send(
            ObjCRuntime.Send(ObjCRuntime.GetClass("VNRecognizeTextRequest"), "alloc"),
            "initWithCompletionHandler:", (nint)blockPtr);

        // Set recognition level to .accurate (1)
        ObjCRuntime.SendInt(request, "setRecognitionLevel:", 1);

        // 6. Perform request synchronously
        t_pendingResults = 0;
        nint requestsArray = ObjCRuntime.ArrayWithObject(request);
        nint error = 0;
        ObjCRuntime.objc_msgSend_perform(handler,
            ObjCRuntime.Sel("performRequests:error:"),
            requestsArray, ref error);

        Marshal.FreeHGlobal((nint)blockPtr);
        Marshal.FreeHGlobal((nint)descPtr);

        ObjCRuntime.Send(request, "release");
        ObjCRuntime.Send(handler, "release");

        // 7. Extract text from results
        nint results = t_pendingResults;
        if (results == 0)
        {
            ObjCRuntime.Send(pool, "drain");
            return Task.FromResult<string?>(null);
        }

        nint count = ObjCRuntime.ArrayCount(results);
        if (count == 0)
        {
            ObjCRuntime.Send(pool, "drain");
            return Task.FromResult<string?>("");
        }

        var sb = new System.Text.StringBuilder();
        for (nint i = 0; i < count; i++)
        {
            nint observation = ObjCRuntime.ArrayObjectAt(results, i);
            nint candidates = ObjCRuntime.SendInt(observation, "topCandidates:", 1);
            if (candidates == 0) continue;
            nint candidateCount = ObjCRuntime.ArrayCount(candidates);
            if (candidateCount == 0) continue;
            nint candidate = ObjCRuntime.ArrayObjectAt(candidates, 0);
            nint textObj = ObjCRuntime.Send(candidate, "string");
            string? line = ObjCRuntime.ToManagedString(textObj);
            if (line != null)
            {
                if (sb.Length > 0) sb.Append('\n');
                sb.Append(line);
            }
        }

        ObjCRuntime.Send(pool, "drain");
        return Task.FromResult<string?>(sb.Length > 0 ? sb.ToString() : null);
    }
}
