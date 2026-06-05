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

// macOS stub — WinForms UserControl not available on macOS.
// Real implementation will use Avalonia in Plan 3.

using System.Drawing;

namespace ShareX.HelpersLib
{
    public abstract class ColorUserControl
    {
        public event ColorEventHandler ColorChanged;

        public bool CrosshairVisible { get; set; } = true;
        public MyColor SelectedColor { get; set; }
        public DrawStyle DrawStyle { get; set; }

        protected void OnColorChanged()
        {
            ColorChanged?.Invoke(this, new ColorEventArgs(SelectedColor, DrawStyle));
        }

        // Fields used by ColorBox/ColorSlider subclasses
        protected Bitmap bmp;
        protected int clientWidth = 1;
        protected int clientHeight = 1;
        protected DrawStyle drawStyle;
        protected MyColor selectedColor;
        protected bool mouseDown;
        protected Point lastPos;
        public string Name { get; set; }
        public Size ClientSize { get; set; }

        protected int Round(double val)
        {
            int ret_val = (int)val;
            int temp = (int)(val * 100);
            if ((temp % 100) >= 50) ret_val += 1;
            return ret_val;
        }

        // Abstract members kept so subclasses (ColorBox, ColorSlider) still compile
        protected virtual void Initialize() { }
        protected abstract void DrawCrosshair(Graphics g);
        protected abstract void DrawHue();
        protected abstract void DrawSaturation();
        protected abstract void DrawBrightness();
        protected abstract void DrawRed();
        protected abstract void DrawGreen();
        protected abstract void DrawBlue();
    }
}
