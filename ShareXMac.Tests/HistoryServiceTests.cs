using ShareXMac.Services;
using ShareXMac.ViewModels;
using Xunit;

namespace ShareXMac.Tests;

public class HistoryServiceTests
{
    [Fact]
    public void AddCapture_ThenGetItems_ContainsEntry()
    {
        string tempFile = Path.Combine(Path.GetTempPath(), $"sharexmac-hist-{Guid.NewGuid():N}.json");
        try
        {
            var svc = new HistoryService(tempFile);
            svc.AddCapture("/tmp/test.png");
            var items = svc.GetItems();
            Assert.Single(items);
            Assert.Equal("test.png", items[0].FileName);
            Assert.Equal("/tmp/test.png", items[0].FilePath);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task GetItemsAsync_ReturnsItems()
    {
        string tempFile = Path.Combine(Path.GetTempPath(), $"sharexmac-hist-{Guid.NewGuid():N}.json");
        try
        {
            var svc = new HistoryService(tempFile);
            svc.AddCapture("/tmp/async-test.png");
            var items = await svc.GetItemsAsync();
            Assert.NotEmpty(items);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void MissingFile_GetItems_ReturnsEmpty()
    {
        string nonexistent = Path.Combine(Path.GetTempPath(), $"no-hist-{Guid.NewGuid():N}.json");
        var svc = new HistoryService(nonexistent);
        Assert.Empty(svc.GetItems());
    }
}

public class HistoryViewModelTests
{
    [Fact]
    public void HistoryViewModel_Constructor_DoesNotThrow()
    {
        string tempFile = Path.Combine(Path.GetTempPath(), $"sharexmac-hvm-{Guid.NewGuid():N}.json");
        try
        {
            var svc = new HistoryService(tempFile);
            var vm = new HistoryViewModel(svc);
            Assert.NotNull(vm);
        }
        finally { if (File.Exists(tempFile)) File.Delete(tempFile); }
    }

    [Fact]
    public void HistoryViewModel_SearchText_FiltersItems()
    {
        string tempFile = Path.Combine(Path.GetTempPath(), $"sharexmac-hvm2-{Guid.NewGuid():N}.json");
        try
        {
            var svc = new HistoryService(tempFile);
            svc.AddCapture("/tmp/screenshot-001.png");
            svc.AddCapture("/tmp/screenshot-002.png");
            svc.AddCapture("/tmp/recording-001.mp4");
            var vm = new HistoryViewModel(svc);
            vm.LoadItems();
            Assert.Equal(3, vm.FilteredItems.Count);
            vm.SearchText = "recording";
            Assert.Equal(1, vm.FilteredItems.Count);
            Assert.Contains("recording", vm.FilteredItems[0].FileName);
        }
        finally { if (File.Exists(tempFile)) File.Delete(tempFile); }
    }
}
