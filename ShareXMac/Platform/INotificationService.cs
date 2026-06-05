namespace ShareXMac.Platform;

public interface INotificationService
{
    Task ShowAsync(string title, string body);
}
