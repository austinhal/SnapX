using ShareXMac.ViewModels;
using Xunit;

namespace ShareXMac.Tests;

public class AppViewModelTests
{
    [Fact]
    public void AppViewModel_InitializesWithExpectedTitle()
    {
        var vm = new AppViewModel();
        Assert.Equal("ShareX Mac", vm.Title);
    }
}
