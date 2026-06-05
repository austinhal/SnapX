using ShareXMac.ViewModels;
using Xunit;

namespace ShareXMac.Tests;

public class TrayViewModelTests
{
    [Fact]
    public void TrayViewModel_CaptureRegionCommand_IsNotNull()
    {
        var vm = new TrayViewModel();
        Assert.NotNull(vm.CaptureRegionCommand);
    }

    [Fact]
    public void TrayViewModel_QuitCommand_IsNotNull()
    {
        var vm = new TrayViewModel();
        Assert.NotNull(vm.QuitCommand);
    }
}
