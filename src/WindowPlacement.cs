using System;
using System.Drawing;
using System.Runtime.Serialization;

namespace NeonStack
{
    [DataContract]
    public sealed class WindowPlacement
    {
        [DataMember] public int X;
        [DataMember] public int Y;
        [DataMember] public int Width;
        [DataMember] public int Height;
        public Rectangle Bounds { get { return new Rectangle(X, Y, Width, Height); } }
        public static WindowPlacement From(Rectangle bounds)
        { return new WindowPlacement { X = bounds.X, Y = bounds.Y, Width = bounds.Width, Height = bounds.Height }; }

        public static Rectangle Fit(Rectangle requested, Rectangle area, Size minimum)
        {
            int width = Math.Min(area.Width, Math.Max(minimum.Width, requested.Width));
            int height = Math.Min(area.Height, Math.Max(minimum.Height, requested.Height));
            int x = Math.Max(area.Left, Math.Min(requested.X, area.Right - width));
            int y = Math.Max(area.Top, Math.Min(requested.Y, area.Bottom - height));
            return new Rectangle(x, y, width, height);
        }
    }
}
