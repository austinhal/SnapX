using ShareX.HelpersLib;

namespace ShareXMac.Services;

public class OcrService
{
    private readonly IScreenCapture _capture;

    public OcrService(IScreenCapture capture) => _capture = capture;

    public virtual async Task<string?> CaptureAndRecognizeAsync()
    {
        byte[]? image = await _capture.CaptureRegionAsync();
        if (image == null) return null;
        return await _capture.RecognizeTextAsync(image);
    }
}
