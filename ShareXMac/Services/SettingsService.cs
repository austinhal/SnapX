using Newtonsoft.Json;
using ShareXMac.Models;

namespace ShareXMac.Services;

public class SettingsService
{
    private readonly string _filePath;

    public AppSettings Current { get; private set; }

    public event Action? Saved;

    public SettingsService(string filePath)
    {
        _filePath = filePath;
        Current = Load();
    }

    private AppSettings Load()
    {
        if (!File.Exists(_filePath)) return new AppSettings();
        try
        {
            string json = File.ReadAllText(_filePath);
            return JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        File.WriteAllText(_filePath, JsonConvert.SerializeObject(Current, Formatting.Indented));
        Saved?.Invoke();
    }
}
