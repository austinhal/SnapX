namespace ShareX.HelpersLib;

public interface INotificationService
{
    Task ShowAsync(string title, string body);
}
