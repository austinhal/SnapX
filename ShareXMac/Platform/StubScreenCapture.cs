using ShareX.HelpersLib;
namespace ShareXMac.Platform;

public class StubScreenCapture : IScreenCapture
{
    public Task<byte[]?> CaptureRegionAsync() => Task.FromResult<byte[]?>(null);
    public Task<byte[]?> CaptureWindowAsync() => Task.FromResult<byte[]?>(null);
    public Task<byte[]?> CaptureFullscreenAsync() => Task.FromResult<byte[]?>(null);
    public Task StartRecordingAsync(string outputPath, RecordingFormat format) => Task.CompletedTask;
    public Task StopRecordingAsync() => Task.CompletedTask;
    public Task<string?> RecognizeTextAsync(byte[] imageData) => Task.FromResult<string?>(null);
}
