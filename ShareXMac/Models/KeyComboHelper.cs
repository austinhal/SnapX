using ShareX.HelpersLib;

namespace ShareXMac.Models;

public static class KeyComboHelper
{
    public static string ToString(KeyCombo? combo)
    {
        if (combo == null) return "";
        return string.IsNullOrEmpty(combo.Modifiers)
            ? combo.Key
            : $"{combo.Modifiers}+{combo.Key}";
    }

    public static KeyCombo? Parse(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        int lastPlus = s.LastIndexOf('+');
        if (lastPlus == s.Length - 1) return null; // trailing '+', no key
        if (lastPlus < 0) return new KeyCombo("", s.Trim());
        return new KeyCombo(s[..lastPlus].Trim(), s[(lastPlus + 1)..].Trim());
    }
}
