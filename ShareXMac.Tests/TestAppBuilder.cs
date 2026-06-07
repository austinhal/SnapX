using Avalonia;
using Avalonia.Headless;
using Xunit;

[assembly: Avalonia.Headless.AvaloniaTestApplication(typeof(ShareXMac.Tests.TestAppBuilder))]

namespace ShareXMac.Tests;

public class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions());
}

// All test classes that create Avalonia objects must belong to this collection.
// xUnit runs all classes in the same collection on the same thread sequentially,
// which prevents "Call from invalid thread" when multiple fixtures bind the dispatcher.
[CollectionDefinition(nameof(HeadlessAvaloniaFixture))]
public class HeadlessAvaloniaCollection : ICollectionFixture<HeadlessAvaloniaFixture> { }

public sealed class HeadlessAvaloniaFixture
{
    public HeadlessAvaloniaFixture()
    {
        try
        {
            TestAppBuilder.BuildAvaloniaApp().SetupWithoutStarting();
        }
        catch
        {
            // Already initialised by [AvaloniaTestApplication] or a prior run.
        }
    }
}
