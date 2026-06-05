namespace ShareXMac.Platform;

public interface IScreenCapture
{
    Task<byte[]?> CaptureRegionAsync();
    Task<byte[]?> CaptureWindowAsync();
    Task<byte[]?> CaptureFullscreenAsync();
    Task StartRecordingAsync(string outputPath, RecordingFormat format);
    Task StopRecordingAsync();
    Task<string?> RecognizeTextAsync(byte[] imageData);
}

public enum RecordingFormat { MP4, GIF }
