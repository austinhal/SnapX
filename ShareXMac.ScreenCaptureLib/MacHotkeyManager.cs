using System.Runtime.InteropServices;
using ShareX.HelpersLib;

namespace ShareXMac.ScreenCaptureLib;

public class MacHotkeyManager : IHotkeyManager, IDisposable
{
    private const int kCGHIDEventTap = 0;
    private const int kCGHeadInsertEventTap = 0;
    private const int kCGEventTapOptionListenOnly = 1;
    private const ulong kCGEventKeyDownMask = 1ul << 10;
    private const int kCGKeyboardEventKeycode = 9;

    // CGEventFlags bitmasks
    private const ulong FlagShift   = 0x00020000UL;
    private const ulong FlagControl = 0x00040000UL;
    private const ulong FlagAlt     = 0x00080000UL;
    private const ulong FlagCommand = 0x00100000UL;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate nint CGEventCallback(nint proxy, uint type, nint eventRef, nint userInfo);

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern nint CGEventTapCreate(
        int tap, int place, int options,
        ulong eventsOfInterest,
        CGEventCallback callback,
        nint userInfo);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern nint CFMachPortCreateRunLoopSource(
        nint allocator, nint tap, nint order);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern nint CFRunLoopGetCurrent();

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern void CFRunLoopAddSource(nint runloop, nint source, nint mode);

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern long CGEventGetIntegerValueField(nint eventRef, int field);

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern ulong CGEventGetFlags(nint eventRef);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern void CFMachPortInvalidate(nint port);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern void CFRelease(nint cf);

    // Non-blocking accessibility check — does not trigger a permission dialog
    [DllImport("/System/Library/Frameworks/ApplicationServices.framework/ApplicationServices")]
    private static extern bool AXIsProcessTrustedWithOptions(nint options);

    private readonly Dictionary<string, (KeyCombo combo, Action callback)> _hotkeys = new();
    private nint _tapPort;
    private readonly CGEventCallback _nativeCallback;

    public MacHotkeyManager()
    {
        _nativeCallback = HandleEvent;
    }

    public bool IsAvailable
    {
        get
        {
            try
            {
                // AXIsProcessTrustedWithOptions with nil options = check without prompting
                return AXIsProcessTrustedWithOptions(0);
            }
            catch
            {
                return false;
            }
        }
    }

    public void Register(string id, KeyCombo combo, Action callback)
    {
        _hotkeys[id] = (combo, callback);
        EnsureTapRunning();
    }

    public void Unregister(string id) => _hotkeys.Remove(id);

    public void UnregisterAll() => _hotkeys.Clear();

    private void EnsureTapRunning()
    {
        if (_tapPort != 0) return;
        _tapPort = CGEventTapCreate(
            kCGHIDEventTap, kCGHeadInsertEventTap,
            kCGEventTapOptionListenOnly,
            kCGEventKeyDownMask,
            _nativeCallback, 0);
        if (_tapPort == 0) return;
        nint source = CFMachPortCreateRunLoopSource(0, _tapPort, 0);
        if (source == 0) return;
        CFRunLoopAddSource(CFRunLoopGetCurrent(), source, 0);
    }

    private nint HandleEvent(nint proxy, uint type, nint eventRef, nint userInfo)
    {
        if (type == 10) // kCGEventKeyDown
        {
            long keycode = CGEventGetIntegerValueField(eventRef, kCGKeyboardEventKeycode);
            ulong flags  = CGEventGetFlags(eventRef);
            bool cmdHeld   = (flags & FlagCommand) != 0;
            bool shiftHeld = (flags & FlagShift)   != 0;
            bool ctrlHeld  = (flags & FlagControl) != 0;
            bool altHeld   = (flags & FlagAlt)     != 0;

            foreach (var (_, (combo, callback)) in _hotkeys)
            {
                if (GetVirtualKeyCode(combo.Key) != keycode) continue;
                bool needsCmd   = combo.Modifiers.Contains("Cmd");
                bool needsShift = combo.Modifiers.Contains("Shift");
                bool needsCtrl  = combo.Modifiers.Contains("Ctrl");
                bool needsAlt   = combo.Modifiers.Contains("Alt");
                if (cmdHeld == needsCmd && shiftHeld == needsShift
                    && ctrlHeld == needsCtrl && altHeld == needsAlt)
                    callback();
            }
        }
        return eventRef;
    }

    private static long GetVirtualKeyCode(string key) => key.ToUpperInvariant() switch
    {
        "A" => 0, "S" => 1, "D" => 2, "F" => 3, "H" => 4, "G" => 5,
        "Z" => 6, "X" => 7, "C" => 8, "V" => 9, "B" => 11, "Q" => 12,
        "W" => 13, "E" => 14, "R" => 15, "Y" => 16, "T" => 17,
        "I" => 34, "O" => 31, "U" => 32, "J" => 38, "K" => 40,
        "L" => 37, "M" => 46, "N" => 45, "P" => 35,
        "1" => 18, "2" => 19, "3" => 20, "4" => 21, "5" => 23,
        "6" => 22, "7" => 26, "8" => 28, "9" => 25, "0" => 29,
        "F1" => 122, "F2" => 120, "F3" => 99,  "F4" => 118,
        "F5" => 96,  "F6" => 97,  "F7" => 98,  "F8" => 100,
        "F9" => 101, "F10" => 109, "F11" => 103, "F12" => 111,
        _ => -1
    };

    public void Dispose()
    {
        if (_tapPort != 0)
        {
            CFMachPortInvalidate(_tapPort);
            CFRelease(_tapPort);
            _tapPort = 0;
        }
    }
}
