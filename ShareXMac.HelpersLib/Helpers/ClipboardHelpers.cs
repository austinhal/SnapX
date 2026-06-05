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

using System.Drawing;

namespace ShareX.HelpersLib
{
    // macOS stub — real clipboard implementation comes in Plan 2
    public static class ClipboardHelpers
    {
        public const string FORMAT_PNG = "PNG";
        public const string FORMAT_17 = "Format17";

        public static bool Clear() => false;
        public static bool CopyText(string text) => false;
        public static bool CopyImage(Image img, string fileName = null) => false;
        public static bool CopyFile(string path) => false;
        public static bool CopyFile(string[] paths) => false;
        public static bool CopyImageFromFile(string path) => false;
        public static bool CopyTextFromFile(string path) => false;
        public static System.Drawing.Bitmap GetImage(bool checkContainsImage = false) => null;
        public static System.Drawing.Bitmap GetImageAlternative2() => null;
        public static string GetText(bool checkContainsText = false) => null;
        public static string[] GetFileDropList(bool checkContainsFileDropList = false) => null;
        public static System.Drawing.Bitmap TryGetImage() => null;
        public static bool ContainsImage() => false;
        public static bool ContainsText() => false;
        public static bool ContainsFileDropList() => false;
    }
}
