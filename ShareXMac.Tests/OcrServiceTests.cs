using ShareX.HelpersLib;
using ShareXMac.Services;
using Xunit;

namespace ShareXMac.Tests;

public class OcrServiceTests
{
    [Fact]
    public async Task CaptureAndRecognize_WhenCaptureReturnsNull_ReturnsNull()
    {
        var stub = new StubOcrCapture(regionResult: null, textResult: null);
        var svc = new OcrService(stub);
        string? result = await svc.CaptureAndRecognizeAsync();
        Assert.Null(result);
    }

    [Fact]
    public async Task CaptureAndRecognize_WhenRecognitionReturnsNull_ReturnsNull()
    {
        var stub = new StubOcrCapture(regionResult: new byte[] { 1, 2, 3 }, textResult: null);
        var svc = new OcrService(stub);
        string? result = await svc.CaptureAndRecognizeAsync();
        Assert.Null(result);
    }

    [Fact]
    public async Task CaptureAndRecognize_WhenRecognitionSucceeds_ReturnsText()
    {
        var stub = new StubOcrCapture(regionResult: new byte[] { 1, 2, 3 }, textResult: "Hello World");
        var svc = new OcrService(stub);
        string? result = await svc.CaptureAndRecognizeAsync();
        Assert.Equal("Hello World", result);
    }

    [Fact]
    public async Task CaptureAndRecognize_DoesNotCallRecognize_WhenCaptureCancelled()
    {
        var stub = new StubOcrCapture(regionResult: null, textResult: "should not be returned");
        var svc = new OcrService(stub);
        await svc.CaptureAndRecognizeAsync();
        Assert.False(stub.RecognizeCalled);
    }
}

internal class StubOcrCapture : IScreenCapture
{
    private readonly byte[]? _regionResult;
    private readonly string? _textResult;
    public bool RecognizeCalled { get; private set; }

    public StubOcrCapture(byte[]? regionResult, string? textResult)
    {
        _regionResult = regionResult;
        _textResult   = textResult;
    }

    public Task<byte[]?> CaptureRegionAsync()     => Task.FromResult(_regionResult);
    public Task<byte[]?> CaptureWindowAsync()     => Task.FromResult<byte[]?>(null);
    public Task<byte[]?> CaptureFullscreenAsync() => Task.FromResult<byte[]?>(null);
    public Task StartRecordingAsync(string outputPath, RecordingFormat format) => Task.CompletedTask;
    public Task StopRecordingAsync()              => Task.CompletedTask;
    public Task<string?> RecognizeTextAsync(byte[] imageData)
    {
        RecognizeCalled = true;
        return Task.FromResult(_textResult);
    }
}
