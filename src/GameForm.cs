using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using System.Windows.Forms;

namespace NeonStack
{
    public sealed class GameForm : Form
    {
        private const int CanvasWidth = 1100, CanvasHeight = 820, BoardX = 392, BoardY = 132, Tile = 30;
        private static readonly Color Background = Color.FromArgb(10, 14, 22);
        private static readonly Color PanelColor = Color.FromArgb(15, 22, 33);
        private static readonly Color Border = Color.FromArgb(38, 51, 67);
        private static readonly Color Muted = Color.FromArgb(131, 150, 168);
        private static readonly Color Ink = Color.FromArgb(228, 239, 238);
        private static readonly Color Lime = Color.FromArgb(197, 246, 106);
        private static readonly Color Cyan = Color.FromArgb(81, 217, 231);
        private static readonly Color[] Colors = { Cyan, Color.FromArgb(246, 205, 92), Color.FromArgb(179, 133, 242),
            Color.FromArgb(156, 220, 109), Color.FromArgb(243, 112, 139), Color.FromArgb(111, 149, 239), Color.FromArgb(245, 163, 101) };
        private readonly Game game = new Game();
        private readonly ScoreStore scores;
        private readonly Audio audio;
        private readonly Timer timer = new Timer { Interval = 8 };
        private readonly Stopwatch clock = Stopwatch.StartNew();
        private readonly Dictionary<int, Font> fonts = new Dictionary<int, Font>();
        private readonly HashSet<Keys> held = new HashSet<Keys>();
        private readonly HashSet<Keys> pressed = new HashSet<Keys>();
        private readonly Dictionary<string, RectangleF> buttons = new Dictionary<string, RectangleF>();
        private double previous, lateralTime, dropTime, flashTime, lockPulse, renderTime;
        private int direction;
        private int[] flashedRows = new int[0];
        private int lastClear;
        private int lastClearPoints;
        private string hover, initials = "", toast;
        private double toastTime;
        private bool records, resumeAfterRecords, editingInitials, replaceInitials;
        private ScoreEntry latest;
        private float scale = 1, offsetX, offsetY;
        private bool compact, volumeOpen, draggingVolume, resumeAfterVolume;
        private bool terminalHandled, resultsDismissed;
        private bool ShowGhost { get { return scores.Data.ShowGhost ?? true; } }
        private bool helpOpen, resumeAfterHelp;
        private bool Sprint { get { return game.Mode == GameMode.Sprint40; } }
        private List<ScoreEntry> Ranking { get { return Sprint ? scores.Data.SprintEntries : scores.Data.Entries; } }
        private string RunTime { get { return ScoreStore.FormatTime((long)(game.ElapsedSeconds * 1000)); } }
        private RectangleF volumeSlider;
        internal bool Compact { get { return compact; } }
        internal Func<bool> ConfirmCloseWithoutSaving;

        public GameForm(ScoreStore store, bool enableAudio = false)
        {
            scores = store;
            audio = new Audio(enableAudio) { Volume = store.Data.Volume ?? 35, Muted = store.Data.Muted };
            Text = "NEON STACK — Tetris";
            ClientSize = new Size(CanvasWidth, CanvasHeight);
            MinimumSize = new Size(900, 720);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Background;
            AutoScaleMode = AutoScaleMode.None;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            KeyPreview = true;
            Icon = AppIcon.Create();
            game.RowsCleared += delegate(int[] rows) { flashedRows = rows; lastClear = rows.Length; lastClearPoints = new[] { 0, 100, 300, 500, 800 }[rows.Length] * ((game.Lines - rows.Length) / 10 + 1); flashTime = 0.42; audio.Play(rows.Length == 4 ? Effect.Tetris : Effect.Clear); };
            game.PieceLocked += delegate { lockPulse = 0.12; if (flashTime <= 0) audio.Play(Effect.Lock); };
            timer.Tick += delegate { Advance(); };
            Deactivate += delegate { Advance(); game.Pause(); audio.Stop(); ClearInput(); pressed.Clear(); Invalidate(); };
            ConfirmCloseWithoutSaving = delegate { return MessageBox.Show(this,
                "No se han podido guardar los récords o preferencias. ¿Quieres cerrar y descartar los cambios sin guardar?",
                "NEON STACK — Error al guardar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.Yes; };
            FormClosing += delegate(object sender, FormClosingEventArgs e)
            {
                CapturePreferences();
                bool saved = editingInitials && latest != null ? scores.Rename(latest, initials) : scores.Save();
                if (!saved && e.CloseReason == CloseReason.UserClosing)
                {
                    game.Pause(); audio.Stop(); ClearInput();
                    e.Cancel = !ConfirmCloseWithoutSaving(); Invalidate();
                }
            };
            Shown += delegate { previous = clock.Elapsed.TotalSeconds; timer.Start(); };
            TopMost = store.Data.OnTop;
            game.SelectMode(store.Data.Sprint ? GameMode.Sprint40 : GameMode.Classic);
            compact = store.Data.Compact;
            RestoreWindow();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { timer.Dispose(); audio.Dispose(); foreach (Font font in fonts.Values) font.Dispose(); if (Icon != null) Icon.Dispose(); }
            base.Dispose(disposing);
        }

        private void ClearInput() { held.Clear(); direction = 0; lateralTime = dropTime = 0; }

        private void Advance()
        {
            double now = clock.Elapsed.TotalSeconds, elapsed = Math.Max(0, now - previous), dt = Math.Min(0.10, elapsed); previous = now;
            if (game.State == Phase.Playing && !records)
            {
                if (direction != 0)
                {
                    lateralTime -= dt;
                    while (lateralTime <= 0) { game.Move(direction); lateralTime += 0.045; }
                }
                if (held.Contains(Keys.S) || held.Contains(Keys.Down))
                {
                    dropTime -= dt;
                    while (dropTime <= 0) { game.SoftDrop(); dropTime += 0.035; }
                }
                game.Tick(elapsed, .10);
                ObserveGameOver();
                flashTime = Math.Max(0, flashTime - dt); lockPulse = Math.Max(0, lockPulse - dt);
            }
            bool toastVisible = toastTime > 0;
            toastTime = Math.Max(0, toastTime - dt);
            // Poll input frequently, but cap painting at roughly 60 frames per second.
            renderTime += dt;
            if (renderTime >= 1.0 / 60)
            {
                renderTime %= 1.0 / 60;
                if (game.State == Phase.Playing || toastVisible) Invalidate();
            }
            timer.Interval = game.State == Phase.Playing ? 8 : 80;
        }

        private void ObserveGameOver()
        {
            if ((game.State != Phase.GameOver && game.State != Phase.Completed) || terminalHandled) return;
            terminalHandled = true;
            if (Sprint)
                latest = game.State == Phase.Completed ? scores.AddSprint(Math.Max(1, (long)(game.ElapsedSeconds * 1000)), game.Score, game.Level, game.Lines) : null;
            else latest = scores.Add(game.Score, game.Level, game.Lines);
            audio.Play(game.State == Phase.Completed ? Effect.Tetris : Effect.Over);
            initials = scores.Data.LastInitials; editingInitials = latest != null; replaceInitials = true; ClearInput();
            Invalidate();
        }

        private void StartGame()
        {
            latest = null; terminalHandled = false; resultsDismissed = false; editingInitials = false; records = false; flashTime = lockPulse = 0;
            ClearInput(); game.Start(); timer.Interval = 8; previous = clock.Elapsed.TotalSeconds; Invalidate();
            audio.Play(Effect.Start);
        }

        private void SaveInitials()
        {
            if (latest == null) return;
            bool saved = scores.Rename(latest, initials);
            if (saved) { editingInitials = false; toast = "PUNTUACION GUARDADA"; toastTime = 3; }
            else { toast = "ERROR AL GUARDAR. ENTER PARA REINTENTAR"; toastTime = 5; }
            Invalidate();
        }

        private void DismissResults()
        {
            if (game.State != Phase.GameOver && game.State != Phase.Completed) return;
            if (editingInitials && latest != null && !scores.Rename(latest, initials))
            {
                toast = "ERROR AL GUARDAR. ENTER PARA REINTENTAR"; toastTime = 5; Invalidate(); return;
            }
            editingInitials = false; resultsDismissed = true; ClearInput(); Invalidate();
        }

        private void TogglePause()
        {
            if (game.State == Phase.Playing) game.Pause();
            else if (game.State == Phase.Paused) { game.Resume(); timer.Interval = 8; previous = clock.Elapsed.TotalSeconds; }
            ClearInput(); Invalidate();
        }

        private void ToggleRecords()
        {
            if (compact) { ShowCompactRecords(); return; }
            if (!records)
            {
                resumeAfterRecords = game.State == Phase.Playing;
                game.Pause(); records = true;
            }
            else { records = false; if (resumeAfterRecords) { game.Resume(); timer.Interval = 8; } }
            ClearInput(); previous = clock.Elapsed.TotalSeconds; Invalidate();
        }

        protected override bool IsInputKey(Keys keyData)
        {
            Keys key = keyData & Keys.KeyCode;
            return key == Keys.Left || key == Keys.Right || key == Keys.Up || key == Keys.Down || base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            Advance();
            e.Handled = true; e.SuppressKeyPress = true;
            if (!pressed.Add(e.KeyCode)) return;
            if (e.KeyCode == Keys.F2) { SetCompact(!compact); return; }
            if (e.KeyCode == Keys.F3) { TopMost = !TopMost; Invalidate(); return; }
            if (e.KeyCode == Keys.F4 || (e.KeyCode == Keys.M && (!editingInitials || records || volumeOpen || helpOpen))) { audio.Muted = !audio.Muted; Invalidate(); return; }
            if (e.KeyCode == Keys.F1) { SetHelpOpen(!helpOpen); return; }
            if (helpOpen) { if (e.KeyCode == Keys.Escape) SetHelpOpen(false); return; }
            if (e.KeyCode == Keys.F5 && !volumeOpen && !records) { ToggleMode(); return; }
            if (volumeOpen)
            {
                if (e.KeyCode == Keys.Escape || e.KeyCode == Keys.Enter) SetVolumeOpen(false);
                else if (e.KeyCode == Keys.Left || e.KeyCode == Keys.Down) { audio.Volume -= 5; Invalidate(); }
                else if (e.KeyCode == Keys.Right || e.KeyCode == Keys.Up) { audio.Volume += 5; Invalidate(); }
                return;
            }
            if (e.KeyCode == Keys.Tab) { ToggleRecords(); return; }
            if (editingInitials && !records)
            {
                if (e.KeyCode == Keys.Enter) SaveInitials();
                else if (e.KeyCode == Keys.Back) { if (replaceInitials) initials = ""; else if (initials.Length > 0) initials = initials.Substring(0, initials.Length - 1); replaceInitials = false; }
                else if ((e.KeyCode >= Keys.A && e.KeyCode <= Keys.Z) || (e.KeyCode >= Keys.D0 && e.KeyCode <= Keys.D9))
                {
                    if (replaceInitials) { initials = ""; replaceInitials = false; }
                    if (initials.Length < 3) initials += (char)e.KeyCode;
                }
                Invalidate(); return;
            }
            if (!held.Add(e.KeyCode)) return;
            if (records) { if (e.KeyCode == Keys.Escape || e.KeyCode == Keys.Tab) ToggleRecords(); return; }
            if (e.KeyCode == Keys.X && (game.State == Phase.GameOver || game.State == Phase.Completed)) { DismissResults(); return; }
            if (e.KeyCode == Keys.Enter && (game.State == Phase.Ready || game.State == Phase.GameOver || game.State == Phase.Completed)) { StartGame(); return; }
            if (e.KeyCode == Keys.Escape || e.KeyCode == Keys.P) { TogglePause(); return; }
            if (game.State != Phase.Playing) return;
            if (e.KeyCode == Keys.Left || e.KeyCode == Keys.A) { direction = -1; lateralTime = 0.16; game.Move(-1); }
            else if (e.KeyCode == Keys.Right || e.KeyCode == Keys.D) { direction = 1; lateralTime = 0.16; game.Move(1); }
            else if (e.KeyCode == Keys.Up || e.KeyCode == Keys.W) { if (game.Rotate(1)) audio.Play(Effect.Rotate); }
            else if (e.KeyCode == Keys.Z) { if (game.Rotate(-1)) audio.Play(Effect.Rotate); }
            else if (e.KeyCode == Keys.Down || e.KeyCode == Keys.S) { game.SoftDrop(); dropTime = 0.035; }
            else if (e.KeyCode == Keys.Space) { game.HardDrop(); ObserveGameOver(); }
            Invalidate();
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e); held.Remove(e.KeyCode); pressed.Remove(e.KeyCode);
            bool left = held.Contains(Keys.Left) || held.Contains(Keys.A), right = held.Contains(Keys.Right) || held.Contains(Keys.D);
            int old = direction;
            if (direction == -1 && !left) direction = right ? 1 : 0;
            if (direction == 1 && !right) direction = left ? -1 : 0;
            if (old != direction) lateralTime = 0.16;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            PointF point = new PointF((e.X - offsetX) / scale, (e.Y - offsetY) / scale);
            if (draggingVolume) { SetVolumeFromPoint(point); return; }
            string oldHover = hover;
            hover = buttons.Where(p => p.Value.Contains(point)).Select(p => p.Key).FirstOrDefault();
            Cursor = hover == null ? Cursors.Default : Cursors.Hand;
            if (oldHover != hover) Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hover = null; Cursor = Cursors.Default; Invalidate(); }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Advance();
            if (e.Button != MouseButtons.Left) return;
            PointF point = new PointF((e.X - offsetX) / scale, (e.Y - offsetY) / scale);
            if (volumeOpen && volumeSlider.Contains(point)) { draggingVolume = true; Capture = true; SetVolumeFromPoint(point); return; }
            string button = buttons.Where(p => p.Value.Contains(point)).Select(p => p.Key).FirstOrDefault();
            if (button == "start") StartGame();
            else if (button == "pause" || button == "resume") TogglePause();
            else if (button == "records" || button == "compact-records" || button == "close-records") ToggleRecords();
            else if (button == "save") SaveInitials();
            else if (button == "mute") audio.Muted = !audio.Muted;
            else if (button == "volume") SetVolumeOpen(!volumeOpen);
            else if (button == "compact") SetCompact(!compact);
            else if (button == "top") TopMost = !TopMost;
            else if (button == "vol-down") { audio.Volume -= 10; audio.Play(Effect.Rotate); }
            else if (button == "vol-up") { audio.Volume += 10; audio.Play(Effect.Rotate); }
            else if (button == "vol-close") SetVolumeOpen(false);
            else if (button == "mode") ToggleMode();
            else if (button == "end-run") { game.EndRun(); ObserveGameOver(); }
            else if (button == "exit") DismissResults();
            else if (button == "help") SetHelpOpen(true);
            else if (button == "help-close") SetHelpOpen(false);
            else if (button == "ghost") scores.Data.ShowGhost = !ShowGhost;
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (draggingVolume) { draggingVolume = false; Capture = false; audio.Play(Effect.Rotate); }
        }

        private void SetVolumeFromPoint(PointF point)
        { audio.Volume = (int)Math.Round(100 * (point.X - volumeSlider.X) / volumeSlider.Width); Invalidate(); }

        private void SetVolumeOpen(bool value)
        {
            if (volumeOpen == value) return;
            if (value) { resumeAfterVolume = game.State == Phase.Playing; game.Pause(); }
            else
            {
                draggingVolume = false; Capture = false;
                if (resumeAfterVolume) { game.Resume(); timer.Interval = 8; }
            }
            volumeOpen = value; ClearInput(); previous = clock.Elapsed.TotalSeconds; Invalidate();
        }

        private void SetHelpOpen(bool value)
        {
            if (helpOpen == value) return;
            if (value)
            {
                SetVolumeOpen(false);
                if (records) { records = false; if (resumeAfterRecords) game.Resume(); }
                resumeAfterHelp = game.State == Phase.Playing; game.Pause();
            }
            else if (resumeAfterHelp) { game.Resume(); timer.Interval = 8; }
            helpOpen = value; ClearInput(); previous = clock.Elapsed.TotalSeconds; Invalidate();
        }

        private void CapturePreferences()
        { RememberWindow(); scores.Data.Volume = audio.Volume; scores.Data.Muted = audio.Muted; scores.Data.Compact = compact; scores.Data.OnTop = TopMost; scores.Data.Sprint = Sprint; }

        private void RememberWindow()
        {
            Rectangle bounds = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;
            if (bounds.Width <= 0 || bounds.Height <= 0) return;
            if (compact) scores.Data.CompactWindow = WindowPlacement.From(bounds);
            else scores.Data.FullWindow = WindowPlacement.From(bounds);
        }

        private void RestoreWindow()
        {
            WindowPlacement saved = compact ? scores.Data.CompactWindow : scores.Data.FullWindow;
            Size preferred = compact ? new Size(322, 723) : new Size(1116, 859);
            Rectangle requested = saved != null && saved.Width > 0 && saved.Height > 0 ? saved.Bounds : new Rectangle(Location, preferred);
            Rectangle area = Screen.FromRectangle(requested).WorkingArea;
            if (saved == null) requested.Location = new Point(area.Left + (area.Width - preferred.Width) / 2, area.Top + (area.Height - preferred.Height) / 2);
            Size minimum = compact ? new Size(240, 440) : new Size(900, 720);
            WindowState = FormWindowState.Normal;
            MinimumSize = new Size(Math.Min(minimum.Width, area.Width), Math.Min(minimum.Height, area.Height));
            StartPosition = FormStartPosition.Manual; Bounds = WindowPlacement.Fit(requested, area, minimum);
        }

        private void ToggleMode()
        {
            if (editingInitials || game.State == Phase.Playing || game.State == Phase.Paused) return;
            game.SelectMode(Sprint ? GameMode.Classic : GameMode.Sprint40);
            latest = null; terminalHandled = false; resultsDismissed = false; ClearInput(); Invalidate();
        }

        internal void SetCompact(bool value)
        {
            if (compact == value) return;
            SetVolumeOpen(false); records = false; hover = null;
            RememberWindow(); compact = value; RestoreWindow();
            ClearInput(); Invalidate();
        }

        private void ShowCompactRecords()
        {
            bool resume = game.State == Phase.Playing; game.Pause(); ClearInput();
            using (Form dialog = new Form())
            using (ListView list = new ListView())
            {
                dialog.Text = Sprint ? "NEON STACK — Sprint 40 / Tiempos" : "NEON STACK — Clasificación"; dialog.ClientSize = new Size(580, 430);
                dialog.StartPosition = FormStartPosition.CenterParent; dialog.BackColor = Background; dialog.KeyPreview = true;
                dialog.MinimizeBox = false; dialog.MaximizeBox = false; dialog.ShowInTaskbar = false;
                list.Dock = DockStyle.Fill; list.View = View.Details; list.FullRowSelect = true;
                list.BackColor = Background; list.ForeColor = Ink;
                list.Columns.Add("#", 35); list.Columns.Add("Nombre", 70); list.Columns.Add(Sprint ? "Tiempo" : "Puntos", 95);
                list.Columns.Add("Nivel", 55); list.Columns.Add("Líneas", 60); list.Columns.Add("Fecha", 165);
                for (int i = 0; i < Ranking.Count; i++)
                {
                    ScoreEntry entry = Ranking[i];
                    list.Items.Add(new ListViewItem(new[] { (i + 1).ToString(), entry.Initials, Sprint ? ScoreStore.FormatTime(entry.Milliseconds.Value) : entry.Score.ToString(), entry.Level.ToString(), entry.Lines.ToString(), entry.Date.ToString("dd/MM/yyyy HH:mm") }));
                }
                dialog.Controls.Add(list); dialog.KeyDown += delegate(object sender, KeyEventArgs e) { if (e.KeyCode == Keys.Escape) dialog.Close(); };
                dialog.ShowDialog(this);
            }
            if (resume) { game.Resume(); timer.Interval = 8; }
            previous = clock.Elapsed.TotalSeconds; Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e) { Render(e.Graphics, ClientSize.Width, ClientSize.Height); }

        internal void Render(Graphics g, int width, int height)
        {
            g.Clear(Background);
            int canvasWidth = compact ? 340 : CanvasWidth, canvasHeight = compact ? 760 : CanvasHeight;
            scale = Math.Min(width / (float)canvasWidth, height / (float)canvasHeight);
            offsetX = (width - canvasWidth * scale) / 2; offsetY = (height - canvasHeight * scale) / 2;
            GraphicsState saved = g.Save();
            g.TranslateTransform(offsetX, offsetY); g.ScaleTransform(scale, scale);
            g.SmoothingMode = SmoothingMode.None; g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            buttons.Clear();
            if (compact) DrawCompact(g);
            else
            {
                DrawChrome(g); DrawStats(g); DrawBoard(g); DrawNext(g); DrawControls(g);
                if (game.State != Phase.Playing && !resultsDismissed) DrawOverlay(g);
                if (records) { buttons.Clear(); DrawRecords(g); }
                else { DrawToolbar(g, 392, 43, 78); DrawMode(g, 392, 78, 328, 22); Button(g, "help", "F1 AYUDA", 748, 43, 104, 27, false, 1); DrawGhostButton(g, 732, 78, 22); }
            }
            if (volumeOpen) DrawVolume(g);
            if (helpOpen) DrawHelp(g);
            g.Restore(saved);
        }

        private void DrawToolbar(Graphics g, int x, int y, int width)
        {
            Button(g, "mute", "", x, y, width, 27, false, 1);
            DrawSpeaker(g, x + width / 2 - 15, y + 5);
            Label(g, "M", x + width - 13, y + 8, 10, Muted);
            Button(g, "volume", "VOL " + audio.Volume, x + width + 5, y, width, 27, false, 1);
            Button(g, "compact", compact ? "AMPLIAR" : "MINI", x + (width + 5) * 2, y, width, 27, false, 1);
            Button(g, "top", TopMost ? "FIJADO" : "ENCIMA", x + (width + 5) * 3, y, width, 27, false, 1);
        }

        private void DrawSpeaker(Graphics g, int x, int y)
        {
            Color color = audio.Muted || audio.Volume == 0 ? Muted : Ink;
            using (SolidBrush brush = new SolidBrush(color))
            using (Pen pen = new Pen(color, 1.6f))
            {
                g.FillRectangle(brush, x, y + 5, 5, 7);
                g.FillPolygon(brush, new[] { new Point(x + 4,y + 5), new Point(x + 11,y), new Point(x + 11,y + 17), new Point(x + 4,y + 12) });
                if (audio.Muted || audio.Volume == 0) { g.DrawLine(pen, x + 16, y + 5, x + 24, y + 13); g.DrawLine(pen, x + 24, y + 5, x + 16, y + 13); }
                else { g.DrawArc(pen, x + 9, y + 3, 12, 12, -55, 110); g.DrawArc(pen, x + 7, y, 19, 18, -55, 110); }
            }
        }

        private void DrawGhostButton(Graphics g, int x, int y, int height)
        {
            Button(g, "ghost", ShowGhost ? "FANTASMA ON" : "FANTASMA OFF", x, y, 104, height, false, 1);
        }

        private void DrawMode(Graphics g, int x, int y, int width, int height)
        {
            string name = Sprint ? "SPRINT 40" : "CLASICO";
            if (!editingInitials && game.State != Phase.Playing && game.State != Phase.Paused)
                Button(g, "mode", "MODO: " + name + " / F5 >", x, y, width, height, false, 1);
            else Label(g, Sprint ? "SPRINT 40 / TIEMPO ACTIVO" : "CLÁSICO / PUNTUACIÓN", x + 4, y + 4, 11, Muted);
        }

        private void ImmediateNext(Graphics g, int x, int y, int tile)
        {
            Cell[] cells = Game.Cells(game.Next[0], 0);
            int minX = cells.Min(c => c.X), minY = cells.Min(c => c.Y);
            foreach (Cell cell in cells) Block(g, x + (cell.X - minX) * tile, y + (cell.Y - minY) * tile, tile, game.Next[0], false);
        }

        private void DrawCompact(Graphics g)
        {
            // Reuse the actual board/overlays at native logical tile size. Hit targets
            // are translated too, so buttons retain their behavior when scaled down.
            GraphicsState saved = g.Save(); g.TranslateTransform(20 - BoardX, 116 - BoardY);
            DrawBoard(g); if (game.State != Phase.Playing && !resultsDismissed) DrawOverlay(g);
            g.Restore(saved);
            foreach (string key in buttons.Keys.ToArray())
            { RectangleF rect = buttons[key]; rect.Offset(20 - BoardX, 116 - BoardY); buttons[key] = rect; }
            Pixel(g, Sprint ? RunTime : game.Score.ToString("D6"), 20, 13, Sprint ? 2 : 3, Lime);
            Label(g, Sprint ? game.RemainingLines + " POR LIMPIAR" : "NV " + game.Level.ToString("D2") + " / " + game.Lines + " LÍNEAS", 179, 19, 12, Ink);
            DrawToolbar(g, 20, 47, 71);
            Button(g, game.State == Phase.Playing || game.State == Phase.Paused ? "pause" : resultsDismissed ? "start" : "records", game.State == Phase.Playing ? "PAUSA" : game.State == Phase.Paused ? "SEGUIR" : resultsDismissed ? "JUGAR" : "RECORDS", 20, 83, 73, 24, false, 1);
            Button(g, "compact-records", "TOP 20", 100, 83, 63, 24, false, 1);
            Button(g, "help", "?", 170, 83, 25, 24, false, 1);
            Label(g, "SIG.", 199, 89, 11, Muted); ImmediateNext(g, 247, 83, 14);
            DrawMode(g, 20, 733, 190, 20);
            DrawGhostButton(g, 216, 733, 20);
            if (scores.Notice != null) Label(g, "! Revisar guardado de récords", 20, 717, 11, Colors[4]);
        }

        private void DrawVolume(Graphics g)
        {
            int x = compact ? 20 : 432, y = compact ? 114 : 80;
            // Modal pointer layer: do not let clicks reach the game controls underneath.
            buttons.Clear(); Panel(g, x, y, 300, 111);
            Label(g, "VOLUMEN " + audio.Volume + "%" + (audio.Muted ? " / MUDO" : ""), x + 14, y + 13, 13, Ink);
            Button(g, "vol-close", "X", x + 263, y + 9, 25, 25, false, 1);
            Button(g, "vol-down", "-", x + 14, y + 48, 30, 29, false, 2);
            Button(g, "vol-up", "+", x + 256, y + 48, 30, 29, false, 2);
            volumeSlider = new RectangleF(x + 57, y + 45, 185, 35);
            Fill(g, Border, x + 57, y + 60, 185, 5);
            Fill(g, Lime, x + 57, y + 60, 185 * audio.Volume / 100f, 5);
            Fill(g, Ink, x + 54 + 185 * audio.Volume / 100f, y + 53, 6, 19);
            Label(g, "Arrastra / flechas · ESC para cerrar", x + 14, y + 88, 11, Muted);
        }

        private void DrawHelp(Graphics g)
        {
            buttons.Clear();
            Fill(g, Color.FromArgb(245, Background), 0, 0, compact ? 340 : CanvasWidth, compact ? 760 : CanvasHeight);
            int x = compact ? 12 : 240, y = compact ? 70 : 125, width = compact ? 316 : 620;
            Panel(g, x, y, width, 625);
            Pixel(g, "AYUDA", x + 18, y + 20, 3, Lime);
            Button(g, "help-close", "X", x + width - 42, y + 15, 25, 27, false, 1);
            Label(g, "CONTROLES Y ATAJOS", x + 18, y + 58, compact ? 11 : 13, Muted);
            string[][] rows = {
                new[] { "Mover izquierda / derecha", "A/D · ←/→" },
                new[] { "Girar a la derecha", "W / ↑" },
                new[] { "Girar a la izquierda", "Z" },
                new[] { "Bajar más rápido", "S / ↓" },
                new[] { "Caer y fijar", "Espacio" },
                new[] { "Pausar / continuar", "Esc / P" },
                new[] { "Empezar / repetir", "Enter" },
                new[] { "Clásico / Sprint 40", "F5 (antes)" },
                new[] { "Compacto / completo", "F2" },
                new[] { "Siempre encima", "F3" },
                new[] { "Silenciar / activar", "M / F4" },
                new[] { "Clasificación", "Tab / Esc" },
                new[] { "Ayuda / cerrar ayuda", "F1 / Esc" },
                new[] { "Editar iniciales", "A–Z / 0–9" },
                new[] { "Borrar / guardar nombre", "Retroceso / Enter" },
                new[] { "Cerrar el resultado", "X / Salir" },
                new[] { "Pieza fantasma", "Botón ON/OFF" }
            };
            for (int i = 0; i < rows.Length; i++)
            {
                int rowY = y + 91 + i * 25;
                if (i % 2 == 0) Fill(g, Color.FromArgb(22, 32, 43), x + 10, rowY - 3, width - 20, 25);
                Label(g, rows[i][0], x + 18, rowY, compact ? 10 : 14, Ink);
                Label(g, rows[i][1], x + (compact ? 185 : 385), rowY, compact ? 10 : 14, Cyan);
            }
            Label(g, "VOL: barra, +/− o flechas.", x + 18, y + 522, compact ? 11 : 13, Muted);
            Label(g, "Esc/Enter cierran el volumen.", x + 18, y + 542, compact ? 11 : 13, Muted);
            Label(g, "Pausa: botón Terminar partida.", x + 18, y + 562, compact ? 11 : 13, Muted);
            Label(g, "Al escribir iniciales, M y X son letras.", x + 18, y + 582, compact ? 10 : 13, Muted);
            Label(g, "X cierra el resultado, no la aplicación.", x + 18, y + 599, compact ? 10 : 13, Muted);
        }

        private static void Fill(Graphics g, Color color, float x, float y, float w, float h)
        { using (SolidBrush brush = new SolidBrush(color)) g.FillRectangle(brush, x, y, w, h); }
        private static void Line(Graphics g, Color color, float x1, float y1, float x2, float y2)
        { using (Pen pen = new Pen(color)) g.DrawLine(pen, x1, y1, x2, y2); }
        private static void Frame(Graphics g, Color color, float x, float y, float w, float h)
        { using (Pen pen = new Pen(color)) g.DrawRectangle(pen, x, y, w, h); }
        private void Label(Graphics g, string text, float x, float y, int size, Color color)
        {
            Font font;
            if (!fonts.TryGetValue(size, out font)) { font = new Font("Consolas", size, FontStyle.Regular, GraphicsUnit.Pixel); fonts[size] = font; }
            using (SolidBrush brush = new SolidBrush(color)) g.DrawString(text, font, brush, x, y, StringFormat.GenericTypographic);
        }
        private static void Pixel(Graphics g, string text, float x, float y, int size, Color color) { PixelFont.Draw(g, text, x, y, size, color); }
        private static void CenterPixel(Graphics g, string text, float x, float y, float w, int size, Color color)
        { Pixel(g, text, x + (w - PixelFont.Width(text, size)) / 2, y, size, color); }
        private static void Panel(Graphics g, int x, int y, int w, int h) { Fill(g, PanelColor, x, y, w, h); Frame(g, Border, x, y, w, h); }

        private void DrawChrome(Graphics g)
        {
            Pixel(g, "NEON", 40, 34, 4, Ink); Pixel(g, "STACK", 158, 34, 4, Lime);
            Label(g, "EL CLÁSICO. UNA PARTIDA MÁS.", 41, 77, 12, Muted);
            Fill(g, Lime, 890, 41, 6, 6); Label(g, "ARCADE / OFFLINE", 908, 36, 14, Ink);
            Label(g, "01   /   EDICIÓN DE ESCRITORIO", 846, 76, 12, Muted);
            Line(g, Border, 40, 104, 1060, 104);
            Label(g, Sprint ? "01 / SPRINT 40" : "01 / PUNTUACIÓN", 40, 113, 10, Muted);
            Label(g, "02 / ZONA DE JUEGO", BoardX, 113, 10, Muted);
            Label(g, "SIGUIENTE", 539, 113, 10, Cyan); ImmediateNext(g, 625, 105, 12);
            Label(g, "03 / SIGUIENTES", 736, 113, 10, Muted);
            Line(g, Border, 40, 760, 1060, 760);
            Fill(g, game.State == Phase.Playing ? Lime : Muted, 41, 784, 5, 5);
            string state = game.State == Phase.Playing ? "EN JUEGO" : game.State == Phase.Paused ? "EN PAUSA" : game.State == Phase.Completed ? "40 LINEAS COMPLETADAS" : game.State == Phase.GameOver ? "FIN DE PARTIDA" : "LISTO PARA JUGAR";
            Label(g, state, 55, 778, 12, Ink);
            Label(g, "300 ms para ajustar al tocar el suelo", 359, 778, 12, Muted);
            Label(g, "SIN RED. SOLO TUS RÉCORDS.", 878, 778, 11, Muted);
        }

        private void DrawStats(Graphics g)
        {
            Panel(g, 40, 132, 308, 170);
            Label(g, Sprint ? "TIEMPO / OBJETIVO: 40 LÍNEAS" : "SCORE", 60, 153, 13, Muted);
            Pixel(g, Sprint ? RunTime : game.Score.ToString("D6"), 60, 184, Sprint ? 4 : game.Score > 999999 ? 4 : 5, Lime);
            Line(g, Border, 60, 241, 328, 241);
            Label(g, "NIVEL", 60, 256, 11, Muted); Label(g, Sprint ? "FALTAN" : "LÍNEAS", 164, 256, 11, Muted);
            Pixel(g, game.Level.ToString("D2"), 113, 258, 2, Ink); Pixel(g, (Sprint ? game.RemainingLines : game.Lines).ToString("D3"), 220, 258, 2, Ink);
            Fill(g, Border, 60, 284, 268, 3); Fill(g, Lime, 60, 284, 268 * (Sprint ? Math.Min(40, game.Lines) / 40f : (game.Lines % 10) / 10f), 3);

            Panel(g, 40, 318, 308, 76);
            Label(g, "MEJOR MARCA", 60, 333, 11, Muted);
            Pixel(g, Sprint ? (Ranking.Count > 0 ? ScoreStore.FormatTime(Ranking[0].Milliseconds.Value) : "--:--.---") : Math.Max(scores.Best, game.Score).ToString("D6"), 60, 355, 3, Ink);
            Pixel(g, "HI", 294, 350, 2, Lime);

            Panel(g, 40, 410, 308, 272);
            Pixel(g, "TOP 5", 60, 432, 2, Ink);
            Label(g, "LOCAL", 290, 434, 11, Muted);
            Line(g, Border, 60, 464, 328, 464);
            for (int i = 0; i < 5; i++)
            {
                int y = 482 + i * 33;
                Label(g, (i + 1).ToString("D2"), 60, y, 13, i == 0 ? Lime : Muted);
                ScoreEntry entry = Ranking.Count > i ? Ranking[i] : null;
                Label(g, entry == null ? "---" : entry.Initials, 103, y, 14, Ink);
                Label(g, entry == null ? "------" : Sprint ? ScoreStore.FormatTime(entry.Milliseconds.Value) : entry.Score.ToString("D6"), Sprint ? 215 : 244, y, Sprint ? 13 : 14, entry == null ? Muted : Lime);
            }
            Button(g, "records", "VER CLASIFICACION  >", 40, 699, 308, 33, false, 1);
            if (scores.Notice != null) Label(g, "! " + scores.Notice, 40, 742, 10, Colors[4]);
        }

        private static void Block(Graphics g, float x, float y, int size, int kind, bool ghost)
        {
            Color c = Colors[kind];
            if (ghost)
            {
                Fill(g, Color.FromArgb(25, c), x + 2, y + 2, size - 4, size - 4);
                Frame(g, Color.FromArgb(145, c), x + 3, y + 3, size - 7, size - 7);
                return;
            }
            Fill(g, Color.FromArgb(c.R / 2, c.G / 2, c.B / 2), x + 1, y + 1, size - 2, size - 2);
            Fill(g, c, x + 2, y + 2, size - 4, size - 5);
            Fill(g, Color.FromArgb(95, Color.White), x + 3, y + 3, size - 6, 2);
            Fill(g, Color.FromArgb(40, Color.White), x + 3, y + 5, 2, size - 10);
            Fill(g, Color.FromArgb(30, Color.Black), x + 5, y + size - 7, size - 8, 2);
        }

        private void DrawBoard(Graphics g)
        {
            Fill(g, Color.FromArgb(7, 12, 20), BoardX, BoardY, 300, 600);
            for (int x = 0; x <= 10; x++) Line(g, Color.FromArgb(26, 37, 51), BoardX + x * Tile, BoardY, BoardX + x * Tile, BoardY + 600);
            for (int y = 0; y <= 20; y++) Line(g, Color.FromArgb(26, 37, 51), BoardX, BoardY + y * Tile, BoardX + 300, BoardY + y * Tile);
            for (int y = 0; y < 20; y++) for (int x = 0; x < 10; x++)
                if (game.Board[y, x] > 0) Block(g, BoardX + x * Tile, BoardY + y * Tile, Tile, game.Board[y, x] - 1, false);
            if (game.State == Phase.Ready)
            {
                int[] heights = { 3, 2, 4, 2, 1, 0, 2, 3, 2, 4 };
                for (int x = 0; x < 10; x++) for (int j = 0; j < heights[x]; j++)
                    Block(g, BoardX + x * Tile, BoardY + (19 - j) * Tile, Tile, (x / 2 + j / 2) % 7, false);
            }
            if (game.State == Phase.Playing || game.State == Phase.Paused)
            {
                if (ShowGhost)
                {
                    int ghostY = game.GhostY;
                    foreach (Cell cell in Game.Cells(game.Kind, game.Rotation))
                        if (ghostY + cell.Y >= 0) Block(g, BoardX + (game.X + cell.X) * Tile, BoardY + (ghostY + cell.Y) * Tile, Tile, game.Kind, true);
                }
                foreach (Cell cell in Game.Cells(game.Kind, game.Rotation))
                    if (game.Y + cell.Y >= 0) Block(g, BoardX + (game.X + cell.X) * Tile, BoardY + (game.Y + cell.Y) * Tile, Tile, game.Kind, false);
                if (game.Grounded)
                {
                    Fill(g, Border, BoardX, 741, 300, 3);
                    Fill(g, Lime, BoardX, 741, (float)(300 * (1 - game.LockFraction)), 3);
                }
            }
            if (flashTime > 0)
            {
                foreach (int row in flashedRows) Fill(g, Color.FromArgb((int)(110 * flashTime / .42), Cyan), BoardX, BoardY + row * Tile, 300, Tile);
            }
            Frame(g, lockPulse > 0 ? Cyan : Border, BoardX - 1, BoardY - 1, 302, 602);
            foreach (int x in new[] { BoardX - 3, BoardX + 291 })
                foreach (int y in new[] { BoardY - 3, BoardY + 601 }) Fill(g, Cyan, x, y, 12, 2);
            if (flashTime > 0) CenterPixel(g, (lastClear == 4 ? "TETRIS " : "") + "+" + lastClearPoints, BoardX, 386, 300, 2, Ink);
        }

        private void DrawNext(Graphics g)
        {
            Panel(g, 736, 132, 324, 252);
            Label(g, "EN COLA", 756, 152, 12, Muted);
            Label(g, "7-BAG", 998, 152, 11, Muted);
            int[] next = game.Next;
            for (int i = 0; i < 3; i++)
            {
                int y = 180 + i * 62;
                Label(g, (i + 1).ToString("D2"), 758, y + 18, 12, i == 0 ? Cyan : Muted);
                Cell[] shape = Game.Cells(next[i], 0);
                int minX = shape.Min(c => c.X), minY = shape.Min(c => c.Y);
                int maxX = shape.Max(c => c.X), blockSize = i == 0 ? 25 : 21;
                float x = 920 - (maxX - minX + 1) * blockSize / 2f;
                foreach (Cell cell in shape) Block(g, x + (cell.X - minX) * blockSize, y + (cell.Y - minY) * blockSize, blockSize, next[i], false);
            }
        }

        private void KeyCap(Graphics g, string key, int x, int y, int width)
        {
            Fill(g, Color.FromArgb(24, 34, 47), x, y, width, 25); Frame(g, Border, x, y, width, 25);
            Label(g, key, x + 7, y + 4, 12, Ink);
        }

        private void DrawControls(Graphics g)
        {
            Panel(g, 736, 400, 324, 254);
            Pixel(g, "CONTROLES", 756, 421, 2, Ink);
            KeyCap(g, "A D", 756, 458, 42); KeyCap(g, "← →", 805, 458, 43); Label(g, "Mover", 887, 463, 13, Muted);
            KeyCap(g, "W", 756, 493, 28); KeyCap(g, "↑", 791, 493, 28); Label(g, "Girar", 887, 498, 13, Muted);
            KeyCap(g, "S", 756, 528, 28); KeyCap(g, "↓", 791, 528, 28); Label(g, "Bajar", 887, 533, 13, Muted);
            KeyCap(g, "ESPACIO", 756, 563, 92); Label(g, "Caída directa", 887, 568, 13, Lime);
            Line(g, Border, 756, 603, 1040, 603);
            Label(g, "ESC  Pausa     Z  Giro inverso", 756, 621, 12, Muted);
            if (game.State == Phase.Playing || game.State == Phase.Paused)
                Button(g, "pause", game.State == Phase.Paused ? "CONTINUAR" : "PAUSAR", 736, 672, 324, 60, false, 2);
            else if (resultsDismissed) Button(g, "start", "NUEVA PARTIDA", 736, 672, 324, 60, false, 2);
            else Label(g, "TUS RÉCORDS SE GUARDAN EN ESTE PC", 751, 693, 12, Muted);
        }

        private void Button(Graphics g, string id, string text, int x, int y, int w, int h, bool primary, int fontScale)
        {
            buttons[id] = new RectangleF(x, y, w, h);
            Color fill = primary ? (hover == id ? Color.FromArgb(216, 255, 152) : Lime) : (hover == id ? Color.FromArgb(35, 49, 62) : PanelColor);
            Fill(g, fill, x, y, w, h); Frame(g, primary ? Lime : Border, x, y, w, h);
            CenterPixel(g, text, x, y + (h - 7 * fontScale) / 2, w, fontScale, primary ? Background : Ink);
        }

        private void DrawOverlay(Graphics g)
        {
            Fill(g, Color.FromArgb(game.State == Phase.Paused ? 242 : 170, Background), BoardX, BoardY, 300, 600);
            int y = 282;
            Fill(g, Background, BoardX + 13, y - 27, 274, game.State == Phase.GameOver || game.State == Phase.Completed ? 330 : 289);
            Line(g, Border, BoardX + 31, y - 27, BoardX + 269, y - 27);
            if (game.State == Phase.Ready)
            {
                CenterPixel(g, Sprint ? "SPRINT" : "INSERT", BoardX, y, 300, 4, Ink);
                CenterPixel(g, Sprint ? "40" : "PLAY", BoardX, y + 42, 300, 4, Lime);
                Label(g, Sprint ? "40 líneas. Tu mejor tiempo." : "Encaja. Completa. Supera.", BoardX + 32, y + 97, 15, Muted);
                Button(g, "start", "JUGAR", BoardX + 38, y + 143, 224, 50, true, 2);
                CenterPixel(g, "PULSA ENTER", BoardX, y + 215, 300, 1, Muted);
            }
            else if (game.State == Phase.Paused)
            {
                CenterPixel(g, "PAUSA", BoardX, y + 23, 300, 4, Ink);
                Label(g, "Tómate un respiro.", BoardX + 69, y + 87, 15, Muted);
                Button(g, "resume", "CONTINUAR", BoardX + 38, y + 143, 224, 50, true, 2);
                Button(g, "end-run", "TERMINAR PARTIDA", BoardX + 38, y + 205, 224, 32, false, 1);
                CenterPixel(g, "ESC PARA VOLVER", BoardX, y + 252, 300, 1, Muted);
            }
            else
            {
                CenterPixel(g, game.State == Phase.Completed ? "COMPLETADO" : "GAME OVER", BoardX, y - 5, 300, 3, Ink);
                CenterPixel(g, Sprint ? RunTime : game.Score.ToString("D6"), BoardX, y + 35, 300, 3, Lime);
                if (editingInitials)
                {
                    Label(g, "Escribe tus iniciales", BoardX + 56, y + 78, 15, Muted);
                    for (int i = 0; i < 3; i++)
                    {
                        int x = BoardX + 76 + i * 52;
                        Frame(g, replaceInitials ? Lime : Border, x, y + 105, 44, 40);
                        CenterPixel(g, i < initials.Length ? initials[i].ToString() : "_", x, y + 115, 44, 3, Lime);
                    }
                    Button(g, "save", "GUARDAR", BoardX + 38, y + 169, 224, 44, true, 2);
                    CenterPixel(g, "ENTER PARA GUARDAR", BoardX, y + 234, 300, 1, Muted);
                }
                else
                {
                    Label(g, Sprint && game.State == Phase.GameOver ? "Faltaron " + game.RemainingLines + " líneas." : "Cada partida cuenta.", BoardX + 50, y + 97, 15, Muted);
                    Button(g, "start", "OTRA PARTIDA", BoardX + 26, y + 143, 248, 50, true, 2);
                    CenterPixel(g, "PULSA ENTER", BoardX, y + 215, 300, 1, Muted);
                }
                Button(g, "exit", editingInitials ? "SALIR" : "X / SALIR", BoardX + 38, y + 263, 224, 32, false, 1);
            }
            if (toastTime > 0) CenterPixel(g, toast, BoardX - 25, 599, 350, 1, Cyan);
        }

        private static readonly int[] RecordColumns = { 230, 275, 360, 480, 550, 638, 870 };

        private void RecordCell(Graphics g, string text, int column, int y, Color color)
        {
            // Headers and values share their rectangle and alignment, irrespective of
            // glyph width, score length or the window's render scale.
            RectangleF rect = new RectangleF(RecordColumns[column], y, RecordColumns[column + 1] - RecordColumns[column] - 12, 20);
            using (Font font = new Font("Consolas", 13, FontStyle.Regular, GraphicsUnit.Pixel))
            using (SolidBrush brush = new SolidBrush(color))
            using (StringFormat format = new StringFormat(StringFormat.GenericTypographic))
            {
                format.Alignment = column >= 2 && column <= 4 ? StringAlignment.Far : StringAlignment.Near;
                format.Trimming = StringTrimming.EllipsisCharacter; format.FormatFlags |= StringFormatFlags.NoWrap;
                g.DrawString(text, font, brush, rect, format);
            }
        }

        private void DrawRecords(Graphics g)
        {
            Fill(g, Color.FromArgb(243, Background), 0, 106, CanvasWidth, 651);
            Panel(g, 196, 127, 708, 611);
            Pixel(g, "HALL OF FAME", 230, 151, 3, Lime);
            Label(g, Sprint ? "SPRINT 40 / LOS 20 MEJORES TIEMPOS / MENOR ES MEJOR" : "CLÁSICO / TUS 20 MEJORES PARTIDAS", 230, 187, 12, Muted);
            string[] headings = { "#", "NOMBRE", Sprint ? "TIEMPO" : "PUNTOS", "NIVEL", "LINEAS", "FECHA" };
            for (int column = 0; column < headings.Length; column++) RecordCell(g, headings[column], column, 222, Muted);
            Line(g, Border, 230, 245, 870, 245);
            if (Ranking.Count == 0)
            {
                CenterPixel(g, "TU PRIMER RECORD TE ESPERA", 196, 379, 708, 2, Ink);
                Label(g, Sprint ? "Completa 40 líneas para registrar tu tiempo." : "Juega una partida para inaugurar la clasificación.", 305, 423, 14, Muted);
            }
            for (int i = 0; i < Ranking.Count; i++)
            {
                ScoreEntry e = Ranking[i]; int y = 253 + i * 20;
                if (i % 2 == 0) Fill(g, Color.FromArgb(20, 30, 41), 223, y - 1, 647, 20);
                RecordCell(g, (i + 1).ToString("D2"), 0, y, i == 0 ? Lime : Muted);
                RecordCell(g, e.Initials, 1, y, Ink);
                RecordCell(g, Sprint ? ScoreStore.FormatTime(e.Milliseconds.Value) : e.Score.ToString("D6"), 2, y, Lime);
                RecordCell(g, e.Level.ToString("D2"), 3, y, Ink);
                RecordCell(g, e.Lines.ToString("D3"), 4, y, Ink);
                RecordCell(g, e.Date.ToString("dd/MM/yyyy HH:mm"), 5, y, Muted);
            }
            Button(g, "close-records", "VOLVER  /  ESC", 650, 681, 220, 35, false, 1);
            Label(g, "Sin cuentas. Sin conexión.", 230, 691, 12, Muted);
        }

        internal void PreparePreview(string mode)
        {
            if (mode.StartsWith("compact")) { SetCompact(true); mode = mode.Replace("compact-", ""); if (mode == "compact") mode = "playing"; }
            if (mode.StartsWith("sprint")) { game.SelectMode(GameMode.Sprint40); mode = mode.Replace("sprint-", ""); if (mode == "sprint") mode = "playing"; }
            bool previewHelp = mode == "help";
            bool previewVolume = mode == "volume";
            if (mode == "ready") return;
            StartGame();
            int[] heights = { 3, 5, 4, 3, 0, 0, 2, 3, 4, 2 };
            for (int x = 0; x < 10; x++) for (int i = 0; i < heights[x]; i++) game.Board[19 - i, x] = (x / 2 + i) % 7 + 1;
            for (int i = 0; i < 5; i++) game.SoftDrop();
            if (mode == "paused") game.Pause();
            if (previewVolume) SetVolumeOpen(true);
            if (previewHelp) SetHelpOpen(true);
            if (mode == "records") { game.Pause(); records = true; }
            if (mode == "records-filled")
            {
                game.Pause(); records = true;
                for (int i = 0; i < 20; i++) Ranking.Add(new ScoreEntry { Initials = new[] { "REX", "ACE", "PIX", "NEO" }[i % 4], Score = i == 0 ? 12345678 : 65432 - i * 2345, Milliseconds = Sprint ? (long?)(64234 + i * 5172) : null, Level = Sprint ? 5 : 20 - i, Lines = Sprint ? 40 : (20 - i) * 10 - 1, Date = new DateTime(2026, 9, 7, 18, i, 0) });
            }
            if (mode == "gameover")
            {
                for (int i = 0; i < 40 && game.State == Phase.Playing; i++) game.HardDrop();
                // Preview scores stay in memory; screenshot mode never writes a leaderboard.
                latest = new ScoreEntry { Initials = "AAA", Score = game.Score, Level = game.Level, Lines = game.Lines };
                initials = "AAA"; editingInitials = true; replaceInitials = true;
            }
        }
    }
}
