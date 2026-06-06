using ShareX.HistoryLib;

namespace ShareXMac.Services;

public class HistoryService
{
    private readonly HistoryManagerJSON _manager;

    public HistoryService(string historyFilePath)
    {
        var dir = Path.GetDirectoryName(historyFilePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        _manager = new HistoryManagerJSON(historyFilePath);
    }

    public void AddCapture(string filePath, string? url = null)
    {
        _manager.AppendHistoryItem(new HistoryItem
        {
            FileName = Path.GetFileName(filePath),
            FilePath = filePath,
            DateTime = DateTime.Now,
            Type = "Image",
            Host = url != null && Uri.TryCreate(url, UriKind.Absolute, out var u) ? u.Host : "",
            URL = url ?? ""
        });
    }

    public List<HistoryItem> GetItems() => _manager.GetHistoryItems();

    public Task<List<HistoryItem>> GetItemsAsync() => _manager.GetHistoryItemsAsync();
}
