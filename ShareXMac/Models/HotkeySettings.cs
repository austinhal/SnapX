using ShareX.HelpersLib;

namespace ShareXMac.Models;

public class HotkeySettings
{
    public KeyCombo? CaptureRegion { get; set; }
    public KeyCombo? CaptureWindow { get; set; }
    public KeyCombo? CaptureFullscreen { get; set; }
    public KeyCombo? RecordVideo { get; set; }
    public KeyCombo? RecordGif { get; set; }
}
