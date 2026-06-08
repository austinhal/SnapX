using ShareXMac.Platform;
using ShareXMac.Services;
using ShareXMac.ViewModels;
using Xunit;

namespace ShareXMac.Tests;

public class TrayViewModelRecordingTests
{
    private static TrayViewModel MakeVm() => new TrayViewModel(
        new StubScreenCapture(),
        new SettingsService(Path.GetTempFileName()),
        new HistoryService(Path.GetTempFileName()),
        new UploadService(),
        new StubHotkeyManager(),
        new OcrService(new StubScreenCapture()));

    [Fact]
    public void IsRecording_DefaultsFalse()
        => Assert.False(MakeVm().IsRecording);

    [Fact]
    public void RecordVideoHeader_IsRecordVideo_WhenNotRecording()
        => Assert.Equal("Record Video", MakeVm().RecordVideoHeader);

    [Fact]
    public void RecordGifHeader_IsRecordGif_WhenNotRecording()
        => Assert.Equal("Record GIF", MakeVm().RecordGifHeader);

    [Fact]
    public void RecordVideoHeader_ChangesWhenRecordingStarts()
    {
        var vm = MakeVm();
        vm.IsRecording = true;
        Assert.Equal("Stop Recording (Video)", vm.RecordVideoHeader);
    }

    [Fact]
    public void RecordGifHeader_ChangesWhenRecordingStarts()
    {
        var vm = MakeVm();
        vm.IsRecording = true;
        Assert.Equal("Stop Recording (GIF)", vm.RecordGifHeader);
    }
}
