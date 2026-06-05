// macOS compatibility shims — replaces WinForms-specific enums
// These mirrors the exact values of their System.Windows.Forms counterparts.

using System;

namespace ShareX.HelpersLib
{
    [Flags]
    public enum AnchorStyles
    {
        None = 0,
        Top = 1,
        Bottom = 2,
        Left = 4,
        Right = 8,
    }

    public enum Orientation
    {
        Horizontal = 0,
        Vertical = 1,
    }
}
