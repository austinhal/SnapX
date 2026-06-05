using ShareXMac.ScreenCaptureLib.ObjC;
using Xunit;

namespace ShareXMac.Tests;

public class ObjCRuntimeTests
{
    [Fact]
    public void GetClass_NSString_ReturnsNonZero()
    {
        nint cls = ObjCRuntime.GetClass("NSString");
        Assert.NotEqual(0, cls);
    }

    [Fact]
    public void Send_NSStringClass_Description_ReturnsNonZero()
    {
        nint cls = ObjCRuntime.GetClass("NSString");
        nint desc = ObjCRuntime.Send(cls, "description");
        Assert.NotEqual(0, desc);
    }

    [Fact]
    public void ToNSString_ThenToManagedString_RoundTrips()
    {
        nint nsStr = ObjCRuntime.ToNSString("hello");
        string? result = ObjCRuntime.ToManagedString(nsStr);
        Assert.Equal("hello", result);
    }
}
