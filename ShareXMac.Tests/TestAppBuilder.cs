using Avalonia;
using Avalonia.Headless;

[assembly: Avalonia.Headless.AvaloniaTestApplication(typeof(ShareXMac.Tests.TestAppBuilder))]

namespace ShareXMac.Tests;

public class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions());
}

/// <summary>
/// xUnit class fixture that initialises the Avalonia headless platform once per test class.
/// Use with <c>[Collection(nameof(HeadlessAvaloniaFixture))]</c> or as a
/// <c>IClassFixture&lt;HeadlessAvaloniaFixture&gt;</c> on any test class that creates
/// Avalonia bitmaps / platform objects from plain [Fact] tests.
/// </summary>
public sealed class HeadlessAvaloniaFixture
{
    private static readonly object _lock = new();
    private static bool _initialized;

    public HeadlessAvaloniaFixture()
    {
        lock (_lock)
        {
            if (_initialized) return;
            try
            {
                TestAppBuilder.BuildAvaloniaApp().SetupWithoutStarting();
            }
            catch
            {
                // Already initialised by a previous fixture or [AvaloniaTestApplication].
            }
            _initialized = true;
        }
    }
}
