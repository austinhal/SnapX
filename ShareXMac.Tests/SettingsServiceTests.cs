using ShareX.UploadersLib;
using ShareXMac.Models;
using ShareXMac.Services;
using Xunit;

namespace ShareXMac.Tests;

public class SettingsServiceTests
{
    [Fact]
    public void AppSettings_Defaults_AreReasonable()
    {
        var s = new AppSettings();
        Assert.False(string.IsNullOrEmpty(s.SavePath));
        Assert.True(s.AutoCopyImage);
        Assert.True(s.ShowPostCaptureToolbar);
    }

    [Fact]
    public void SettingsService_SaveAndLoad_RoundTrips()
    {
        string tempFile = Path.Combine(Path.GetTempPath(), $"sharexmac-test-{Guid.NewGuid():N}.json");
        try
        {
            var svc = new SettingsService(tempFile);
            svc.Current.SavePath = "/tmp/sharexmac-test";
            svc.Current.AutoCopyImage = false;
            svc.Save();

            var svc2 = new SettingsService(tempFile);
            Assert.Equal("/tmp/sharexmac-test", svc2.Current.SavePath);
            Assert.False(svc2.Current.AutoCopyImage);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void SettingsService_MissingFile_ReturnsDefaults()
    {
        string nonexistent = Path.Combine(Path.GetTempPath(), $"no-such-file-{Guid.NewGuid():N}.json");
        var svc = new SettingsService(nonexistent);
        Assert.False(string.IsNullOrEmpty(svc.Current.SavePath));
    }

    [Fact]
    public void AppSettings_UploadDefaults_AreCorrect()
    {
        var s = new AppSettings();
        Assert.Equal("", s.ImgurClientId);
        Assert.Equal(ImageDestination.Imgur, s.ActiveImageDestination);
        Assert.False(s.AutoUploadAfterCapture);
    }

    [Fact]
    public void SettingsService_FiresSavedEvent_WhenSaveCalled()
    {
        bool fired = false;
        var svc = new SettingsService(Path.GetTempFileName());
        svc.Saved += () => fired = true;
        svc.Save();
        Assert.True(fired);
    }
}
