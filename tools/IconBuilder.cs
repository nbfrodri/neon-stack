using System.IO;
using NeonStack;
internal static class IconBuilder
{
    private static void Main(string[] args)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[0])));
        AppIcon.Write(args[0]);
        if (args.Length > 1) using (System.Drawing.Bitmap bitmap = AppIcon.Render(256)) bitmap.Save(args[1]);
    }
}
