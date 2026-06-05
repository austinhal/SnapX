using ShareX.HelpersLib;
using ShareXMac.ScreenCaptureLib;
using Xunit;

namespace ShareXMac.Tests;

public class MacClipboardTests
{
    [Fact]
    public void SetText_ThenGetText_RoundTrips()
    {
        string text = $"sharexmac-test-{Guid.NewGuid()}";
        MacClipboard.SetText(text);
        string? result = MacClipboard.GetText();
        Assert.Equal(text, result);
    }

    [Fact]
    public void ContainsText_AfterSetText_ReturnsTrue()
    {
        MacClipboard.SetText("hello");
        Assert.True(MacClipboard.ContainsText());
    }

    [Fact]
    public void GetText_ReturnsNonNull_AfterSetText()
    {
        MacClipboard.SetText("test");
        string? result = MacClipboard.GetText();
        Assert.NotNull(result);
    }

    [Fact]
    public void SetImage_DoesNotThrow()
    {
        // 1x1 white PNG (minimal valid PNG)
        byte[] png = new byte[]
        {
            0x89,0x50,0x4E,0x47,0x0D,0x0A,0x1A,0x0A,0x00,0x00,0x00,0x0D,
            0x49,0x48,0x44,0x52,0x00,0x00,0x00,0x01,0x00,0x00,0x00,0x01,
            0x08,0x02,0x00,0x00,0x00,0x90,0x77,0x53,0xDE,0x00,0x00,0x00,
            0x0C,0x49,0x44,0x41,0x54,0x08,0xD7,0x63,0xF8,0xCF,0xC0,0x00,
            0x00,0x00,0x02,0x00,0x01,0xE2,0x21,0xBC,0x33,0x00,0x00,0x00,
            0x00,0x49,0x45,0x4E,0x44,0xAE,0x42,0x60,0x82
        };
        MacClipboard.SetImage(png); // must not throw
    }
}

public class MacNotificationServiceTests
{
    [Fact]
    public async Task ShowAsync_DoesNotThrow()
    {
        var svc = new MacNotificationService();
        await svc.ShowAsync("Test", "This is a test notification from ShareX-Mac");
    }
}

public class MacHotkeyManagerTests
{
    [Fact]
    public void IsAvailable_ReturnsBoolean_WithoutThrowing()
    {
        var mgr = new MacHotkeyManager();
        bool _ = mgr.IsAvailable; // just verify it doesn't throw
    }

    [Fact]
    public void UnregisterAll_WhenEmpty_DoesNotThrow()
    {
        var mgr = new MacHotkeyManager();
        mgr.UnregisterAll();
    }

    [Fact]
    public void Register_ThenUnregister_DoesNotThrow()
    {
        var mgr = new MacHotkeyManager();
        if (!mgr.IsAvailable) return; // skip without Accessibility permission
        mgr.Register("test-id", new KeyCombo("cmd", "1"), () => { });
        mgr.Unregister("test-id");
    }
}
