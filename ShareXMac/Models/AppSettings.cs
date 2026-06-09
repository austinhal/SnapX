using ShareX.UploadersLib;

namespace ShareXMac.Models;

public class AppSettings
{
    public string SavePath { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
        "ShareX Mac");
    public int PostCaptureToolbarTimeoutSeconds { get; set; } = 8;

    // Upload settings
    public string ImgurClientId { get; set; } = "";
    public ImageDestination ActiveImageDestination { get; set; } = ImageDestination.Imgur;

    // Hotkeys
    public HotkeySettings Hotkeys { get; set; } = new HotkeySettings();

    // Workflow
    public WorkflowSettings Workflow { get; set; } = new WorkflowSettings();
}
