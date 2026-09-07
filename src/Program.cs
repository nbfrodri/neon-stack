using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace NeonStack
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            if (args.Length >= 2 && args[0] == "--screenshot")
            {
                string path = Path.GetFullPath(args[1]);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                using (GameForm form = new GameForm(new ScoreStore(Path.Combine(Path.GetTempPath(), "NeonStack-preview-" + Guid.NewGuid().ToString("N")))))
                using (Bitmap bitmap = new Bitmap(args.Length >= 5 ? int.Parse(args[3]) : 1100, args.Length >= 5 ? int.Parse(args[4]) : 820))
                using (Graphics graphics = Graphics.FromImage(bitmap))
                {
                    form.PreparePreview(args.Length >= 3 ? args[2] : "ready");
                    form.Render(graphics, bitmap.Width, bitmap.Height);
                    bitmap.Save(path, ImageFormat.Png);
                }
                return;
            }
            bool created;
            using (Mutex mutex = new Mutex(true, "Local\\NeonStack-" + Environment.UserName, out created))
            {
                if (!created) { MessageBox.Show("NEON STACK ya está abierto. Revisa la barra de tareas.", "NEON STACK"); return; }
                try
                {
                    string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NeonStack");
                    using (GameForm form = new GameForm(new ScoreStore(directory), true)) Application.Run(form);
                }
                finally { mutex.ReleaseMutex(); }
            }
        }
    }
}
