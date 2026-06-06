using System.Diagnostics;

namespace ShareXMac.Services;

public class LoginItemService
{
    private readonly string _label;

    public LoginItemService(string label = "com.austinhal.snapx")
    {
        _label = label;
    }

    public string PlistPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        "Library", "LaunchAgents", $"{_label}.plist");

    public bool IsEnabled => File.Exists(PlistPath);

    public void Enable(string executablePath)
    {
        string plist = $"""
            <?xml version="1.0" encoding="UTF-8"?>
            <!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
            <plist version="1.0">
            <dict>
                <key>Label</key>
                <string>{_label}</string>
                <key>ProgramArguments</key>
                <array>
                    <string>{executablePath}</string>
                </array>
                <key>RunAtLoad</key>
                <true/>
                <key>KeepAlive</key>
                <false/>
            </dict>
            </plist>
            """;

        Directory.CreateDirectory(Path.GetDirectoryName(PlistPath)!);
        File.WriteAllText(PlistPath, plist);

        RunLaunchctl("load", PlistPath);
    }

    public void Disable()
    {
        if (!File.Exists(PlistPath)) return;
        RunLaunchctl("unload", PlistPath);
        File.Delete(PlistPath);
    }

    private static void RunLaunchctl(string command, string path)
    {
        using var p = Process.Start(new ProcessStartInfo("launchctl", $"{command} {path}")
        {
            UseShellExecute = false,
            RedirectStandardError = true
        });
        p?.WaitForExit(5000);
    }
}
