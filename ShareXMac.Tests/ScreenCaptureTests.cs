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
