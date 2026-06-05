namespace ShareXMac.Platform;

public class StubHotkeyManager : IHotkeyManager
{
    public bool IsAvailable => false;
    public void Register(string id, KeyCombo combo, Action callback) { }
    public void Unregister(string id) { }
    public void UnregisterAll() { }
}
