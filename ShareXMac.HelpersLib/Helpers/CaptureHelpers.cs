// macOS stub — real screen capture implementation comes in Plan 2

using System.Drawing;

namespace ShareX.HelpersLib
{
    public static class CaptureHelpers
    {
        public static Rectangle GetScreenBounds() => new Rectangle(0, 0, 2560, 1440);
        public static Rectangle GetActiveScreenBounds() => GetScreenBounds();
        public static Rectangle GetScreenWorkingArea() => GetScreenBounds();
    }
}
