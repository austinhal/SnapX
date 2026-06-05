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
