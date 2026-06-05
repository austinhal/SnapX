namespace ShareXMac.Platform;

public interface IHotkeyManager
{
    bool IsAvailable { get; }
    void Register(string id, KeyCombo combo, Action callback);
    void Unregister(string id);
    void UnregisterAll();
}

public record KeyCombo(string Modifiers, string Key);
