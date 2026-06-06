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
