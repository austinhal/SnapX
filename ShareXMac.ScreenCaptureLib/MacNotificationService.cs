using ShareX.HelpersLib;
using ShareXMac.ScreenCaptureLib.ObjC;

namespace ShareXMac.ScreenCaptureLib;

public class MacNotificationService : INotificationService
{
    private static bool s_frameworkLoaded;

    private static void EnsureFrameworkLoaded()
    {
        if (s_frameworkLoaded) return;
        ObjCRuntime.dlopen(
            "/System/Library/Frameworks/UserNotifications.framework/UserNotifications", 1);
        s_frameworkLoaded = true;
    }

    private static bool HasAppBundle()
    {
        // UNUserNotificationCenter requires a proper app bundle;
        // calling it from an unbundled process throws NSInternalInconsistencyException.
        nint mainBundle = ObjCRuntime.Send(ObjCRuntime.GetClass("NSBundle"), "mainBundle");
        if (mainBundle == 0) return false;
        nint bundleId = ObjCRuntime.Send(mainBundle, "bundleIdentifier");
        return bundleId != 0;
    }

    public Task ShowAsync(string title, string body)
    {
        try
        {
            EnsureFrameworkLoaded();

            if (!HasAppBundle()) return Task.CompletedTask;

            nint center = ObjCRuntime.Send(
                ObjCRuntime.GetClass("UNUserNotificationCenter"),
                "currentNotificationCenter");
            if (center == 0) return Task.CompletedTask;

            nint content = ObjCRuntime.Send(
                ObjCRuntime.Send(ObjCRuntime.GetClass("UNMutableNotificationContent"), "alloc"),
                "init");
            if (content == 0) return Task.CompletedTask;

            ObjCRuntime.Send(content, "setTitle:", ObjCRuntime.ToNSString(title));
            ObjCRuntime.Send(content, "setBody:", ObjCRuntime.ToNSString(body));

            string identifier = Guid.NewGuid().ToString("N");
            nint nsId = ObjCRuntime.ToNSString(identifier);

            nint notifRequest = ObjCRuntime.Send(
                ObjCRuntime.GetClass("UNNotificationRequest"),
                "requestWithIdentifier:content:trigger:",
                nsId, content, 0);

            ObjCRuntime.Send(center, "addNotificationRequest:withCompletionHandler:",
                notifRequest, 0);
        }
        catch
        {
            // Silent failure — notification permission not granted or framework unavailable
        }
        return Task.CompletedTask;
    }
}
