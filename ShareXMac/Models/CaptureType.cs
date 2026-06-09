namespace ShareXMac.Models;

public enum CaptureType { Region, Window, Fullscreen }

public record ResolvedWorkflow(
    bool ShowToolbar,
    bool AutoCopyImage,
    bool AutoUpload,
    string? SaveFolder);
