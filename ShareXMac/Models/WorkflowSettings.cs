namespace ShareXMac.Models;

public class WorkflowSettings
{
    public bool ShowToolbar   { get; set; } = true;
    public bool AutoCopyImage { get; set; } = true;
    public bool AutoUpload    { get; set; } = false;
    public string? SaveFolder { get; set; }

    public CaptureWorkflow RegionOverride     { get; set; } = new();
    public CaptureWorkflow WindowOverride     { get; set; } = new();
    public CaptureWorkflow FullscreenOverride { get; set; } = new();

    public ResolvedWorkflow Resolve(CaptureType type)
    {
        var o = type switch
        {
            CaptureType.Region     => RegionOverride,
            CaptureType.Window     => WindowOverride,
            CaptureType.Fullscreen => FullscreenOverride,
            _                      => new CaptureWorkflow()
        };
        return new ResolvedWorkflow(
            ShowToolbar:   o.ShowToolbar   ?? ShowToolbar,
            AutoCopyImage: o.AutoCopyImage ?? AutoCopyImage,
            AutoUpload:    o.AutoUpload    ?? AutoUpload,
            SaveFolder:    o.SaveFolder    ?? SaveFolder);
    }
}
