using ShareXMac.Services;
using Xunit;

namespace ShareXMac.Tests;

public class LoginItemServiceTests : IDisposable
{
    private readonly string _testLabel = $"com.test.snapx.{Guid.NewGuid():N}";
    private LoginItemService? _svc;

    private LoginItemService MakeSvc()
    {
        _svc = new LoginItemService(_testLabel);
        return _svc;
    }

    [Fact]
    public void IsEnabled_ReturnsFalse_WhenPlistDoesNotExist()
    {
        var svc = MakeSvc();
        Assert.False(svc.IsEnabled);
    }

    [Fact]
    public void Enable_CreatesPlistFile()
    {
        var svc = MakeSvc();
        svc.Enable("/Applications/SnapX.app/Contents/MacOS/SnapX");
        Assert.True(svc.IsEnabled);
    }

    [Fact]
    public void Disable_RemovesPlistFile()
    {
        var svc = MakeSvc();
        svc.Enable("/Applications/SnapX.app/Contents/MacOS/SnapX");
        svc.Disable();
        Assert.False(svc.IsEnabled);
    }

    [Fact]
    public void Enable_PlistContainsExecutablePath()
    {
        var svc = MakeSvc();
        const string exePath = "/Applications/SnapX.app/Contents/MacOS/SnapX";
        svc.Enable(exePath);
        string content = File.ReadAllText(svc.PlistPath);
        Assert.Contains(exePath, content);
    }

    [Fact]
    public void Disable_WhenNotEnabled_DoesNotThrow()
    {
        var svc = MakeSvc();
        svc.Disable();
    }

    public void Dispose()
    {
        _svc?.Disable();
    }
}
