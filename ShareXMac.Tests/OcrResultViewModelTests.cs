using ShareXMac.ViewModels;
using Xunit;

namespace ShareXMac.Tests;

[Collection(nameof(HeadlessAvaloniaFixture))]
public class OcrResultViewModelTests
{
    [Fact]
    public void Constructor_SetsRecognizedText()
    {
        var vm = new OcrResultViewModel("Hello World");
        Assert.Equal("Hello World", vm.RecognizedText);
    }

    [Fact]
    public void AutoDismissSeconds_DefaultIs30()
    {
        var vm = new OcrResultViewModel("text");
        Assert.Equal(30, vm.AutoDismissSeconds);
    }

    [Fact]
    public void DismissCommand_RaisesCloseRequested()
    {
        var vm = new OcrResultViewModel("text");
        bool raised = false;
        vm.CloseRequested += () => raised = true;
        vm.DismissCommand.Execute(null);
        Assert.True(raised);
    }

    [Fact]
    public void CopyCommand_RaisesCloseRequested()
    {
        var vm = new OcrResultViewModel("text");
        bool raised = false;
        vm.CloseRequested += () => raised = true;
        vm.CopyCommand.Execute(null);
        Assert.True(raised);
    }

    [Fact]
    public void CopyCommand_DoesNotThrow_WhenTextIsEmpty()
    {
        var vm = new OcrResultViewModel("");
        vm.CopyCommand.Execute(null);
    }
}
