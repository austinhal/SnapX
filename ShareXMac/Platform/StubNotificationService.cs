using ShareX.HelpersLib;
namespace ShareXMac.Platform;

public class StubNotificationService : INotificationService
{
    public Task ShowAsync(string title, string body) => Task.CompletedTask;
}
