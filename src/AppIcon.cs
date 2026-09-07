using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace NeonStack
{
    internal static class AppIcon
    {
        [DllImport("user32.dll")] private static extern bool DestroyIcon(IntPtr handle);
        internal static Bitmap Render(int size)
        {
            Bitmap bitmap = new Bitmap(size, size, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.Transparent); g.ScaleTransform(size / 16f, size / 16f);
                using (SolidBrush bg = new SolidBrush(Color.FromArgb(10, 18, 28)))
                    g.FillPolygon(bg, new[] { new Point(2,0), new Point(14,0), new Point(16,2), new Point(16,14), new Point(14,16), new Point(2,16), new Point(0,14), new Point(0,2) });
                using (Pen border = new Pen(Color.FromArgb(54, 85, 101), .5f))
                    g.DrawPolygon(border, new[] { new Point(2,1), new Point(14,1), new Point(15,2), new Point(15,14), new Point(14,15), new Point(2,15), new Point(1,14), new Point(1,2) });
                using (SolidBrush lime = new SolidBrush(Color.FromArgb(197, 246, 106)))
                using (SolidBrush cyan = new SolidBrush(Color.FromArgb(81, 217, 231)))
                using (SolidBrush shine = new SolidBrush(Color.FromArgb(240, 255, 208)))
                {
                    g.FillRectangle(lime, 6, 2, 3, 3);
                    g.FillRectangle(lime, 2, 6, 3, 3); g.FillRectangle(lime, 6, 6, 3, 3); g.FillRectangle(lime, 10, 6, 3, 3);
                    g.FillRectangle(cyan, 6, 10, 3, 3); g.FillRectangle(cyan, 10, 10, 3, 3);
                    g.FillRectangle(shine, 6, 2, 3, .5f); g.FillRectangle(shine, 2, 6, 3, .5f);
                    g.FillRectangle(shine, 6, 6, 3, .5f); g.FillRectangle(shine, 10, 6, 3, .5f);
                }
            }
            return bitmap;
        }

        internal static Icon Create()
        {
            using (Bitmap bitmap = Render(64))
            {
                IntPtr handle = bitmap.GetHicon();
                try { using (Icon icon = Icon.FromHandle(handle)) return (Icon)icon.Clone(); }
                finally { DestroyIcon(handle); }
            }
        }

        internal static void Write(string path)
        {
            int[] sizes = { 16, 24, 32, 48, 64, 128, 256 };
            byte[][] frames = new byte[sizes.Length][];
            for (int i = 0; i < sizes.Length; i++)
                using (Bitmap bitmap = Render(sizes[i])) using (MemoryStream stream = new MemoryStream())
                { bitmap.Save(stream, ImageFormat.Png); frames[i] = stream.ToArray(); }
            using (BinaryWriter writer = new BinaryWriter(File.Create(path)))
            {
                writer.Write((short)0); writer.Write((short)1); writer.Write((short)sizes.Length);
                int offset = 6 + 16 * sizes.Length;
                for (int i = 0; i < sizes.Length; i++)
                {
                    writer.Write((byte)(sizes[i] == 256 ? 0 : sizes[i])); writer.Write((byte)(sizes[i] == 256 ? 0 : sizes[i]));
                    writer.Write((short)0); writer.Write((short)1); writer.Write((short)32);
                    writer.Write(frames[i].Length); writer.Write(offset); offset += frames[i].Length;
                }
                foreach (byte[] frame in frames) writer.Write(frame);
            }
        }
    }
}
