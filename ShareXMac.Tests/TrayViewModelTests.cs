using ShareXMac.ViewModels;
using ShareXMac.Platform;
using ShareX.HelpersLib;
using Xunit;

namespace ShareXMac.Tests;

public class TrayViewModelTests
{
    [Fact]
    public void TrayViewModel_CaptureRegionCommand_IsNotNull()
    {
        var vm = new TrayViewModel(new StubScreenCapture());
        Assert.NotNull(vm.CaptureRegionCommand);
    }

    [Fact]
    public void TrayViewModel_QuitCommand_IsNotNull()
    {
        var vm = new TrayViewModel(new StubScreenCapture());
        Assert.NotNull(vm.QuitCommand);
    }
}
