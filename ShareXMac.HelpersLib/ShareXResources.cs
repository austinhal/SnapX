namespace ShareX.HelpersLib
{
    // Minimal stub for ShareXResources — macOS port.
    // Theming and full icon support will be implemented in the Avalonia UI layer.
    public static class ShareXResources
    {
        public static string Name { get; set; } = "ShareX-Mac";

        public static string UserAgent => $"{Name}/{Helpers.GetApplicationVersion()}";
    }
}
