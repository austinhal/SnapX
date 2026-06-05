using System.Diagnostics;
using ShareX.HelpersLib;

namespace ShareXMac.ScreenCaptureLib;

public class MacScreenCapture : IScreenCapture
{
    private Process? _recordingProcess;

    public async Task<byte[]?> CaptureRegionAsync()
    {
        string path = TempPath("png");
        if (!await RunCapture($"-i -t png \"{path}\"")) return null;
        return await ReadAndDelete(path);
    }

    public async Task<byte[]?> CaptureWindowAsync()
    {
        string path = TempPath("png");
        if (!await RunCapture($"-w -t png \"{path}\"")) return null;
        return await ReadAndDelete(path);
    }

    public async Task<byte[]?> CaptureFullscreenAsync()
    {
        string path = TempPath("png");
        if (!await RunCapture($"-x -t png \"{path}\"")) return null;
        return await ReadAndDelete(path);
    }

    public async Task StartRecordingAsync(string outputPath, RecordingFormat format)
    {
        if (_recordingProcess != null) return;

        string ffmpeg = FindFfmpeg();
        string args = format == RecordingFormat.GIF
            ? $"-f avfoundation -i \"1:none\" -r 10 -vf \"scale=1280:-1\" \"{outputPath}\""
            : $"-f avfoundation -i \"1:none\" -r 30 -c:v libx264 -preset fast \"{outputPath}\"";

        _recordingProcess = new Process
        {
            StartInfo = new ProcessStartInfo(ffmpeg, args)
            {
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        _recordingProcess.Start();
        await Task.CompletedTask;
    }

    public async Task StopRecordingAsync()
    {
        if (_recordingProcess == null) return;
        try
        {
            await _recordingProcess.StandardInput.WriteAsync('q');
            await _recordingProcess.StandardInput.FlushAsync();
            await _recordingProcess.WaitForExitAsync();
        }
        catch
        {
            _recordingProcess.Kill();
        }
        finally
        {
            _recordingProcess.Dispose();
            _recordingProcess = null;
        }
    }

    public Task<string?> RecognizeTextAsync(byte[] imageData) =>
        MacVision.RecognizeTextAsync(imageData);

    public static string FindFfmpeg()
    {
        string bundled = Path.Combine(AppContext.BaseDirectory, "ffmpeg");
        if (File.Exists(bundled)) return bundled;
        string homebrew = "/opt/homebrew/bin/ffmpeg";
        if (File.Exists(homebrew)) return homebrew;
        return "/usr/local/bin/ffmpeg";
    }

    private static async Task<bool> RunCapture(string args)
    {
        using var proc = Process.Start(new ProcessStartInfo("screencapture", args)
        {
            UseShellExecute = false,
            CreateNoWindow = true
        });
        if (proc == null) return false;
        await proc.WaitForExitAsync();
        return proc.ExitCode == 0;
    }

    private static string TempPath(string ext) =>
        Path.Combine(Path.GetTempPath(), $"sharexmac-{Guid.NewGuid():N}.{ext}");

    private static async Task<byte[]?> ReadAndDelete(string path)
    {
        if (!File.Exists(path)) return null;
        byte[] data = await File.ReadAllBytesAsync(path);
        File.Delete(path);
        return data;
    }
}
