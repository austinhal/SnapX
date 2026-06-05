#region License Information (GPL v3)

/*
    ShareX - A program that allows you to take screenshots and share any file type
    Copyright (c) 2007-2025 ShareX Team

    This program is free software; you can redistribute it and/or
    modify it under the terms of the GNU General Public License
    as published by the Free Software Foundation; either version 2
    of the License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

    Optionally you can also view the license at <http://www.gnu.org/licenses/>.
*/

#endregion License Information (GPL v3)

// macOS stub — Windows cursor P/Invokes not available on macOS.
// Real cursor capture implementation comes in Plan 2.

using System;
using System.Drawing;

namespace ShareX.HelpersLib
{
    public class CursorData
    {
        public IntPtr Handle { get; private set; } = IntPtr.Zero;
        public Point Position { get; private set; } = Point.Empty;
        public Size Size { get; private set; } = Size.Empty;
        public float SizeMultiplier { get; private set; } = 1f;
        public bool IsDefaultSize => SizeMultiplier == 1f;
        public Point Hotspot { get; private set; } = Point.Empty;
        public Point DrawPosition => new Point(Position.X - Hotspot.X, Position.Y - Hotspot.Y);
        public bool IsVisible { get; private set; } = false;

        public CursorData() { }

        public void UpdateCursorData() { }

        public Bitmap ToBitmap() => null;

        public void DrawCursorToImage(Bitmap bmp) { }
    }
}
