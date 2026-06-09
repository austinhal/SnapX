using ShareXMac.Models;
using Xunit;

namespace ShareXMac.Tests;

public class WorkflowSettingsTests
{
    [Fact]
    public void Resolve_InheritsGlobalDefaults_WhenNoOverrideSet()
    {
        var wf = new WorkflowSettings { ShowToolbar = true, AutoCopyImage = true, AutoUpload = false, SaveFolder = null };
        var r = wf.Resolve(CaptureType.Region);
        Assert.True(r.ShowToolbar);
        Assert.True(r.AutoCopyImage);
        Assert.False(r.AutoUpload);
        Assert.Null(r.SaveFolder);
    }

    [Fact]
    public void Resolve_ShowToolbarOverrideTrue_WinsOverGlobalFalse()
    {
        var wf = new WorkflowSettings { ShowToolbar = false };
        wf.RegionOverride.ShowToolbar = true;
        Assert.True(wf.Resolve(CaptureType.Region).ShowToolbar);
    }

    [Fact]
    public void Resolve_ShowToolbarOverrideFalse_WinsOverGlobalTrue()
    {
        var wf = new WorkflowSettings { ShowToolbar = true };
        wf.WindowOverride.ShowToolbar = false;
        Assert.False(wf.Resolve(CaptureType.Window).ShowToolbar);
    }

    [Fact]
    public void Resolve_NullOverride_FallsThroughToGlobal()
    {
        var wf = new WorkflowSettings { ShowToolbar = true };
        // RegionOverride.ShowToolbar is null by default
        Assert.True(wf.Resolve(CaptureType.Region).ShowToolbar);
    }

    [Fact]
    public void Resolve_SaveFolderOverride_WinsOverGlobal()
    {
        var wf = new WorkflowSettings { SaveFolder = "/global" };
        wf.FullscreenOverride.SaveFolder = "/fullscreen";
        Assert.Equal("/fullscreen", wf.Resolve(CaptureType.Fullscreen).SaveFolder);
    }

    [Fact]
    public void Resolve_SaveFolderOverrideNull_FallsBackToGlobal()
    {
        var wf = new WorkflowSettings { SaveFolder = "/global" };
        // FullscreenOverride.SaveFolder is null by default
        Assert.Equal("/global", wf.Resolve(CaptureType.Fullscreen).SaveFolder);
    }

    [Fact]
    public void Resolve_SaveFolderBothNull_ReturnsNull()
    {
        var wf = new WorkflowSettings();
        Assert.Null(wf.Resolve(CaptureType.Region).SaveFolder);
    }

    [Fact]
    public void Resolve_AllActionsInheritForWindowCapture()
    {
        var wf = new WorkflowSettings { ShowToolbar = false, AutoCopyImage = false, AutoUpload = true, SaveFolder = "/s" };
        var r = wf.Resolve(CaptureType.Window);
        Assert.False(r.ShowToolbar);
        Assert.False(r.AutoCopyImage);
        Assert.True(r.AutoUpload);
        Assert.Equal("/s", r.SaveFolder);
    }
}
