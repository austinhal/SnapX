// macOS compatibility shim — replaces System.Windows.Forms.Padding

namespace ShareX.HelpersLib
{
    public struct Padding
    {
        public int Left { get; set; }
        public int Top { get; set; }
        public int Right { get; set; }
        public int Bottom { get; set; }

        public int All
        {
            get
            {
                if (Left == Top && Top == Right && Right == Bottom)
                    return Left;
                return -1;
            }
            set
            {
                Left = Top = Right = Bottom = value;
            }
        }

        public int Horizontal => Left + Right;
        public int Vertical => Top + Bottom;

        public Padding(int all)
        {
            Left = Top = Right = Bottom = all;
        }

        public Padding(int left, int top, int right, int bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }
    }
}
