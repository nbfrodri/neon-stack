using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using NeonStack;

internal static class Tests
{
    private static int passed, failed;
    private static void Assert(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static void Check(string name, Action action)
    {
        try { action(); passed++; Console.WriteLine("PASS " + name); }
        catch (Exception ex) { failed++; Console.WriteLine("FAIL " + name + ": " + ex.Message); }
    }
    private static Game Fresh(int kind)
    {
        for (int seed = 0; seed < 200; seed++) { Game g = new Game(seed); g.Start(); if (g.Kind == kind) return g; }
        throw new Exception("Cannot find deterministic seed.");
    }
    private static void Land(Game g) { while (g.SoftDrop()) { } }
    private static string Occupied(Game g) { return string.Join(";", Game.Cells(g.Kind, g.Rotation).Select(c => (c.X + g.X) + "," + (c.Y + g.Y)).OrderBy(s => s)); }
    private static void Set(Game g, string property, object value)
    { typeof(Game).GetProperty(property).GetSetMethod(true).Invoke(g, new[] { value }); }
    private static string Temp() { return Path.Combine(Path.GetTempPath(), "NeonStack-tests-" + Guid.NewGuid().ToString("N")); }
    private static void Storage(Action<string> test)
    {
        string directory = Temp();
        try { test(directory); }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }
    private static void Key(GameForm f, Keys key, bool release)
    {
        typeof(GameForm).GetMethod("OnKeyDown", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(f, new object[] { new KeyEventArgs(key) });
        if (release) typeof(GameForm).GetMethod("OnKeyUp", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(f, new object[] { new KeyEventArgs(key) });
    }
    private static Game Engine(GameForm f) { return (Game)typeof(GameForm).GetField("game", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(f); }
    private static Audio Sound(GameForm f) { return (Audio)typeof(GameForm).GetField("audio", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(f); }
    private static void Paint(GameForm f, int width, int height) { using (Bitmap b = new Bitmap(width, height)) using (Graphics g = Graphics.FromImage(b)) f.Render(g, width, height); }
    private static void ClickButton(GameForm f, string id, int width, int height)
    {
        Paint(f, width, height);
        Dictionary<string, RectangleF> buttons = (Dictionary<string, RectangleF>)typeof(GameForm).GetField("buttons", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(f);
        Assert(buttons.ContainsKey(id), "missing button " + id);
        RectangleF rect = buttons[id];
        float scale = (float)typeof(GameForm).GetField("scale", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(f);
        float x = (float)typeof(GameForm).GetField("offsetX", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(f);
        float y = (float)typeof(GameForm).GetField("offsetY", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(f);
        typeof(GameForm).GetMethod("OnMouseDown", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(f, new object[] { new MouseEventArgs(MouseButtons.Left, 1, (int)(x + (rect.X + rect.Width / 2) * scale), (int)(y + (rect.Y + rect.Height / 2) * scale), 0) });
    }

    [STAThread]
    private static int Main()
    {
        Application.EnableVisualStyles();
        Check("ready does not advance", delegate { Game g = new Game(0); g.Tick(20); Assert(g.State == Phase.Ready && g.Pieces == 0, "advanced"); });
        Check("seven-bag contains all seven pieces", delegate {
            Game g = new Game(15); g.Start(); HashSet<int> kinds = new HashSet<int>();
            for (int i = 0; i < 70; i++) { kinds.Add(g.Kind); g.HardDrop(); Array.Clear(g.Board, 0, g.Board.Length);
                if ((i + 1) % 7 == 0) { Assert(kinds.Count == 7, "bag duplicate"); kinds.Clear(); } }
        });
        Check("walls and floor reject all seven kinds", delegate {
            for (int k = 0; k < 7; k++) { Game g = Fresh(k);
                while (g.Move(-1)) { } Assert(Game.Cells(k, 0).Min(c => c.X + g.X) == 0, "left boundary");
                while (g.Move(1)) { } Assert(Game.Cells(k, 0).Max(c => c.X + g.X) == 9, "right boundary");
                Land(g); Assert(Game.Cells(k, 0).Max(c => c.Y + g.Y) == 19, "floor boundary"); }
        });
        Check("ghost agrees with hard drop across rotations", delegate {
            for (int k = 0; k < 7; k++) for (int r = 0; r < 4; r++) {
                Game g = Fresh(k); for (int i = 0; i < r; i++) g.Rotate(1);
                int y = g.GhostY, x = g.X, rotation = g.Rotation, distance = y - g.Y;
                g.HardDrop(); Assert(g.Score == distance * 2 && g.Pieces == 1, "drop scoring/lock");
                foreach (Cell c in Game.Cells(k, rotation)) Assert(g.Board[y + c.Y, x + c.X] == k + 1, "ghost mismatch");
            }
        });
        Check("ghost stops above existing blocks", delegate { Game g = Fresh(1); g.Board[15, 4] = 3; Assert(g.GhostY == 13, "stack landing"); });
        Check("soft drop awards one point per cell", delegate { Game g = Fresh(2); int y = g.Y; g.SoftDrop(); Assert(g.Y == y + 1 && g.Score == 1, "soft drop"); Land(g); int s = g.Score; g.SoftDrop(); Assert(g.Score == s, "blocked drop scores"); });
        Check("gravity waits its interval", delegate { Game g = Fresh(0); int y = g.Y; g.Tick(.79); Assert(g.Y == y, "early"); g.Tick(.02); Assert(g.Y == y + 1, "late"); });
        Check("lock allows 300 ms", delegate { Game g = Fresh(1); Land(g); g.Tick(.29); Assert(g.Pieces == 0, "early lock"); g.Tick(.011); Assert(g.Pieces == 1, "missing lock"); });
        Check("valid move renews lock timer", delegate { Game g = Fresh(2); Land(g); g.Tick(.2); Assert(g.Move(1), "move failed"); g.Tick(.2); Assert(g.Pieces == 0 && g.LockResets == 1, "timer did not reset"); g.Tick(.11); Assert(g.Pieces == 1, "never locked"); });
        Check("blocked move cannot renew lock", delegate { Game g = Fresh(1); while (g.Move(-1)) { } Land(g); g.Tick(.2); Assert(!g.Move(-1), "wall move"); g.Tick(.11); Assert(g.Pieces == 1, "wall reset timer"); });
        Check("lock reset cap is enforced", delegate { Game g = Fresh(1); Land(g); for (int i = 0; i < 6; i++) { g.Tick(.2); g.Move(i % 2 == 0 ? 1 : -1); } Assert(g.Pieces == 0 && g.LockResets == 6, "reset allowance"); g.Tick(.2); g.Move(-1); g.Tick(.11); Assert(g.Pieces == 1, "cap bypassed"); });
        Check("square rotation cannot extend lock", delegate { Game g = Fresh(1); Land(g); g.Tick(.2); Assert(!g.Rotate(1), "square spun"); g.Tick(.11); Assert(g.Pieces == 1, "square stall"); });
        Check("four rotations restore each shape", delegate { for (int k = 0; k < 7; k++) { Game g = Fresh(k); for (int i = 0; i < 4; i++) g.Rotate(1); Assert(g.Rotation == 0 && g.X == 3 && g.Y == -1, "orientation drift"); } });
        Check("clockwise and inverse rotations cancel", delegate { Game g = Fresh(2); g.Rotate(1); g.Rotate(-1); Assert(g.Rotation == 0 && g.X == 3, "inverse failed"); });
        Check("symmetric pieces return to exact cells after two turns", delegate {
            foreach (int kind in new[] { 0, 3, 4 }) foreach (int direction in new[] { -1, 1 }) {
                Game g = Fresh(kind); Set(g, "Y", 7);
                string before = Occupied(g); int ghost = g.GhostY;
                Assert(g.Rotate(direction) && Occupied(g) != before, "first turn did not change silhouette");
                Assert(g.Rotate(direction) && Occupied(g) == before && g.GhostY == ghost, "second turn drifted");
            }
        });
        Check("T J L rotate clockwise through four distinct silhouettes", delegate {
            // The off-center cell points up, right, down, left in successive T poses.
            Game t = Fresh(2); Set(t, "Y", 7);
            Cell[] tips = { new Cell(1,0), new Cell(2,1), new Cell(1,2), new Cell(0,1) };
            for (int r = 0; r < 4; r++) { Assert(Game.Cells(t.Kind, t.Rotation).Any(c => c.X == tips[r].X && c.Y == tips[r].Y), "wrong T direction"); t.Rotate(1); }
            foreach (int kind in new[] { 2, 5, 6 }) { Game g = Fresh(kind); Set(g, "Y", 7); string before = Occupied(g); HashSet<string> poses = new HashSet<string>();
                for (int r = 0; r < 4; r++) { poses.Add(Occupied(g)); Assert(g.Rotate(1), "turn failed"); }
                Assert(poses.Count == 4 && Occupied(g) == before, "lost orientation or drifted"); }
        });
        Check("T floor kick succeeds", delegate { Game g = Fresh(2); Land(g); Assert(g.Rotate(1), "kick failed"); Assert(g.Fits(g.Kind, g.Rotation, g.X, g.Y), "kick overlaps"); Assert(g.Y == 17 && g.X == 2, "wrong SRS kick"); });
        Check("I floor kick succeeds", delegate { Game g = Fresh(0); Land(g); Assert(g.Rotate(1), "I kick failed"); Assert(g.Y == 16 && g.X == 4, "wrong I kick"); });
        Check("I wall kick succeeds", delegate { Game g = Fresh(0); g.Rotate(1); while (g.Move(-1)) { } Assert(g.X == -2, "setup"); Assert(g.Rotate(1) && g.X == 0, "I wall kick"); });
        Check("blocked rotation preserves position", delegate { Game g = Fresh(2); Set(g, "Y", 8); for (int y = 5; y < 14; y++) for (int x = 0; x < 10; x++) g.Board[y, x] = 1; foreach (Cell c in Game.Cells(g.Kind, 0)) g.Board[g.Y + c.Y, g.X + c.X] = 0; Assert(!g.Rotate(1) && g.Rotation == 0 && g.X == 3 && g.Y == 8, "rotated through stack"); });
        Check("single double triple tetris clear and score", delegate {
            for (int count = 1; count <= 4; count++) { Game g = Fresh(0); g.Rotate(1);
                for (int y = 20 - count; y < 20; y++) for (int x = 0; x < 10; x++) if (x != 5) g.Board[y, x] = 2;
                Land(g); int before = g.Score; g.HardDrop();
                Assert(g.Lines == count && g.Score - before == new[] { 0, 100, 300, 500, 800 }[count], "line count/score " + count);
                Assert(g.Board.Cast<int>().Count(v => v != 0) == 4 - count, "compaction " + count);
            }
        });
        Check("line clear preserves row order", delegate { Game g = Fresh(0); g.Rotate(1); for (int x = 0; x < 10; x++) if (x != 5) g.Board[19, x] = 2; g.Board[18, 0] = 3; g.Board[17, 1] = 4; g.HardDrop(); Assert(g.Board[19, 0] == 3 && g.Board[18, 1] == 4 && g.Board[0, 0] == 0, "row order"); });
        Check("level rises at ten lines using old level score", delegate { Game g = Fresh(0); Set(g, "Lines", 9); g.Rotate(1); for (int x = 0; x < 10; x++) if (x != 5) g.Board[19, x] = 2; Land(g); int s = g.Score; double speed = g.GravityInterval; g.HardDrop(); Assert(g.Level == 2 && g.Score - s == 100 && g.GravityInterval < speed, "level transition"); });
        Check("pause freezes movement and all timers", delegate { Game g = Fresh(2); Land(g); g.Tick(.2); g.Pause(); int x = g.X, score = g.Score; g.Tick(5); g.Move(1); g.Rotate(1); g.SoftDrop(); g.HardDrop(); Assert(g.X == x && g.Score == score && g.Pieces == 0, "changed while paused"); g.Resume(); g.Tick(.11); Assert(g.Pieces == 1, "lock did not resume"); });
        Check("spawn collision ends game", delegate { Game g = Fresh(1); Land(g); for (int x = 0; x < 10; x++) g.Board[0, x] = 2; g.Board[0, 0] = 0; g.HardDrop(); Assert(g.State == Phase.GameOver, "spawn allowed"); });
        Check("lock above top ends game without partial writes", delegate { Game g = Fresh(2); for (int x = 0; x < 10; x++) g.Board[1, x] = 2; int before = g.Board.Cast<int>().Count(v => v != 0); g.HardDrop(); Assert(g.State == Phase.GameOver && g.Board.Cast<int>().Count(v => v != 0) == before, "top out"); });
        Check("restart resets board and state", delegate { Game g = Fresh(3); g.HardDrop(); g.Start(); Assert(g.Score == 0 && g.Lines == 0 && g.Pieces == 0 && g.Board.Cast<int>().All(v => v == 0) && g.State == Phase.Playing, "restart"); });
        Check("randomized input maintains collision invariants", delegate { Random random = new Random(1234); Game g = new Game(1234); g.Start(); for (int i = 0; i < 20000; i++) { if (g.State == Phase.GameOver) g.Start(); switch (random.Next(7)) { case 0: g.Move(-1); break; case 1: g.Move(1); break; case 2: g.Rotate(1); break; case 3: g.Rotate(-1); break; case 4: g.SoftDrop(); break; case 5: g.Tick(.16); break; case 6: g.HardDrop(); break; } if (g.State == Phase.Playing) { Assert(g.Fits(g.Kind, g.Rotation, g.X, g.Y), "active collision"); Assert(g.GhostY >= g.Y && !g.Fits(g.Kind, g.Rotation, g.X, g.GhostY + 1), "ghost invariant"); } Assert(g.Board.Cast<int>().All(v => v >= 0 && v <= 7), "invalid cell"); } });
        Check("scores persist across instances", delegate { Storage(delegate(string d) { ScoreStore s = new ScoreStore(d); ScoreEntry e = s.Add(1234, 3, 24); Assert(s.Rename(e, "rex"), "save"); ScoreStore loaded = new ScoreStore(d); Assert(loaded.Best == 1234 && loaded.Data.Entries[0].Initials == "REX" && loaded.Data.LastInitials == "REX" && loaded.Data.Entries[0].Lines == 24, "roundtrip"); }); });
        Check("scores sorted and limited to twenty", delegate { Storage(delegate(string d) { ScoreStore s = new ScoreStore(d); for (int i = 0; i < 25; i++) s.Add(i * 10, 1, 0); Assert(s.Data.Entries.Count == 20 && s.Best == 240 && s.Data.Entries.Last().Score == 50, "ranking"); }); });
        Check("initials sanitize and default", delegate { Assert(ScoreStore.NormalizeInitials("a!b$cdef") == "ABC" && ScoreStore.NormalizeInitials(null) == "AAA", "initials"); });
        Check("corrupt primary recovers backup and preserves broken file", delegate { Storage(delegate(string d) { ScoreStore s = new ScoreStore(d); s.Add(100, 1, 0); s.Add(200, 2, 10); File.WriteAllText(s.FilePath, "{ broken"); ScoreStore recovered = new ScoreStore(d); Assert(recovered.Best == 100 && recovered.Notice != null, "backup recovery"); Assert(recovered.Save() && Directory.GetFiles(d, "*.corrupt-*").Length == 1, "corrupt preservation"); Assert(new ScoreStore(d).Best == 100, "recovery persist"); }); });
        Check("corrupt file without backup remains playable", delegate { Storage(delegate(string d) { Directory.CreateDirectory(d); File.WriteAllText(Path.Combine(d, "scores.json"), "null"); ScoreStore s = new ScoreStore(d); Assert(s.Best == 0 && s.Notice != null, "empty recovery"); s.Add(30, 1, 0); Assert(new ScoreStore(d).Best == 30, "new save"); }); });
        Check("save failure keeps score in memory and reports error", delegate { Storage(delegate(string d) { Directory.CreateDirectory(d); string blocked = Path.Combine(d, "blocked"); File.WriteAllText(blocked, "file"); ScoreStore s = new ScoreStore(blocked); s.Add(500, 1, 0); Assert(s.Best == 500 && s.Notice != null && !s.Save(), "failure not surfaced"); }); });
        Check("WASD and arrows control the same engine", delegate { using (GameForm f = new GameForm(new ScoreStore(Temp()))) { Key(f, Keys.Enter, true); Game g = Engine(f); int x = g.X; Key(f, Keys.A, true); Key(f, Keys.Right, true); Assert(g.X == x, "lateral mapping"); Key(f, Keys.D, true); Key(f, Keys.Left, true); Assert(g.X == x, "alternate mapping"); int y = g.Y; Key(f, Keys.S, true); Key(f, Keys.Down, true); Assert(g.Y == y + 2, "soft mapping"); int r = g.Rotation; Key(f, Keys.W, true); Key(f, Keys.Up, true); Assert(g.Rotation == ((r + 2) % Game.RotationCount(g.Kind)), "rotate mapping"); Key(f, Keys.Space, true); Assert(g.Pieces == 1, "space drop"); } });
        Check("OS auto-repeat cannot retrigger pause", delegate { using (GameForm f = new GameForm(new ScoreStore(Temp()))) { Key(f, Keys.Enter, true); Key(f, Keys.Escape, false); Key(f, Keys.Escape, false); Assert(Engine(f).State == Phase.Paused, "repeat unpaused"); } });
        Check("OS auto-repeat cannot drop multiple pieces", delegate { using (GameForm f = new GameForm(new ScoreStore(Temp()))) { Key(f, Keys.Enter, true); Key(f, Keys.Space, false); Key(f, Keys.Space, false); Assert(Engine(f).Pieces == 1, "repeat dropped"); } });
        Check("focus loss pauses active game", delegate { using (GameForm f = new GameForm(new ScoreStore(Temp()))) { Key(f, Keys.Enter, true); typeof(Form).GetMethod("OnDeactivate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(f, new object[] { EventArgs.Empty }); Assert(Engine(f).State == Phase.Paused, "focus pause"); } });
        Check("leaderboard returns to prior play state", delegate { using (GameForm f = new GameForm(new ScoreStore(Temp()))) { Key(f, Keys.Enter, true); Key(f, Keys.Tab, true); Assert(Engine(f).State == Phase.Paused, "records must pause"); Key(f, Keys.Escape, true); Assert(Engine(f).State == Phase.Playing, "records resume"); Key(f, Keys.Escape, true); Key(f, Keys.Tab, true); Key(f, Keys.Escape, true); Assert(Engine(f).State == Phase.Paused, "records lost manual pause"); } });
        Check("game over saves once and initials persist", delegate { Storage(delegate(string d) { using (GameForm f = new GameForm(new ScoreStore(d))) { Key(f, Keys.Enter, true); Game g = Engine(f); for (int i = 0; i < 40 && g.State == Phase.Playing; i++) Key(f, Keys.Space, true); Assert(g.State == Phase.GameOver, "not over"); Assert(new ScoreStore(d).Data.Entries.Count == 1, "automatic save"); Key(f, Keys.R, true); Key(f, Keys.E, true); Key(f, Keys.X, true); Key(f, Keys.Enter, true); ScoreStore loaded = new ScoreStore(d); Assert(loaded.Data.Entries.Count == 1 && loaded.Data.Entries[0].Initials == "REX", "rename duplicated or missing"); Key(f, Keys.Enter, true); Assert(g.State == Phase.Playing && g.Score == 0, "restart after save"); } }); });
        Check("all UI states render at minimum and default size", delegate { foreach (string mode in new[] { "ready", "playing", "paused", "records", "gameover" }) using (GameForm f = new GameForm(new ScoreStore(Temp()))) { f.PreparePreview(mode); using (Bitmap b = new Bitmap(1100, 820)) using (Graphics g = Graphics.FromImage(b)) { f.Render(g, 1100, 820); f.Render(g, 884, 681); } } });
        Check("generated sounds load as PCM waves with bounded samples", delegate {
            foreach (Effect effect in Enum.GetValues(typeof(Effect))) {
                byte[] bytes = Audio.Wave(effect, 100);
                Assert(System.Text.Encoding.ASCII.GetString(bytes, 0, 4) == "RIFF" && BitConverter.ToInt32(bytes, 40) == bytes.Length - 44, "invalid wave");
                using (MemoryStream stream = new MemoryStream(bytes)) using (System.Media.SoundPlayer player = new System.Media.SoundPlayer(stream)) player.Load();
                int peak = 0; for (int i = 44; i < bytes.Length; i += 2) peak = Math.Max(peak, Math.Abs((int)BitConverter.ToInt16(bytes, i)));
                Assert(peak > 1000 && peak < 16000 && BitConverter.ToInt16(bytes, 44) == 0 && BitConverter.ToInt16(bytes, bytes.Length - 2) == 0, "clipped or missing envelope");
            }
        });
        Check("volume changes actual samples and zero volume is silent", delegate {
            byte[] high = Audio.Wave(Effect.Clear, 100), low = Audio.Wave(Effect.Clear, 50), silent = Audio.Wave(Effect.Clear, 0);
            Assert(high.Length == low.Length && low.Length == silent.Length, "duration changed");
            for (int i = 44; i < high.Length; i += 2) { int h = BitConverter.ToInt16(high, i), l = BitConverter.ToInt16(low, i); Assert(Math.Abs(h / 2 - l) <= 1 && BitConverter.ToInt16(silent, i) == 0, "gain mismatch"); }
        });
        Check("compact toggle preserves active piece board and score", delegate { using (GameForm f = new GameForm(new ScoreStore(Temp()))) {
            Key(f, Keys.Enter, true); Key(f, Keys.Space, true); Game g = Engine(f); int score = g.Score; string cells = Occupied(g);
            Key(f, Keys.F2, true); Assert(f.Compact && f.Width < 400 && g.State == Phase.Playing && g.Score == score && Occupied(g) == cells, "compact resets game");
            Key(f, Keys.F2, true); Assert(!f.Compact && g.Score == score && Occupied(g) == cells, "expand resets game");
        } });
        Check("compact overlay play and pause hit targets follow scaling", delegate { using (GameForm f = new GameForm(new ScoreStore(Temp()))) {
            f.SetCompact(true); ClickButton(f, "start", 260, 580); Assert(Engine(f).State == Phase.Playing, "start target");
            ClickButton(f, "pause", 260, 580); Assert(Engine(f).State == Phase.Paused, "pause target");
            ClickButton(f, "resume", 260, 580); Assert(Engine(f).State == Phase.Playing, "resume target");
        } });
        Check("mute and volume buttons work in both layouts", delegate {
            foreach (bool compact in new[] { false, true }) using (GameForm f = new GameForm(new ScoreStore(Temp()))) {
                f.SetCompact(compact); int w = compact ? 306 : 1100, h = compact ? 684 : 820;
                ClickButton(f, "mute", w, h); Assert(Sound(f).Muted, "mute"); ClickButton(f, "mute", w, h); Assert(!Sound(f).Muted, "unmute");
                ClickButton(f, "volume", w, h); int before = Sound(f).Volume;
                ClickButton(f, "vol-up", w, h); Assert(Sound(f).Volume == before + 10, "volume up");
                ClickButton(f, "vol-down", w, h); Assert(Sound(f).Volume == before, "volume down");
                ClickButton(f, "vol-close", w, h); ClickButton(f, "mute", w, h); Assert(Sound(f).Muted, "close panel");
            }
        });
        Check("audio and window preferences roundtrip", delegate { Storage(delegate(string d) {
            ScoreStore store = new ScoreStore(d); store.Data.Volume = 72; store.Data.Muted = true; store.Data.Compact = true; store.Data.OnTop = true; Assert(store.Save(), "save preferences");
            using (GameForm f = new GameForm(new ScoreStore(d))) Assert(f.Compact && f.TopMost && Sound(f).Muted && Sound(f).Volume == 72, "load preferences");
        }); });
        Check("old score files use default volume without losing scores", delegate { Storage(delegate(string d) {
            Directory.CreateDirectory(d); File.WriteAllText(Path.Combine(d, "scores.json"), "{\"LastInitials\":\"OLD\",\"Entries\":[]}");
            ScoreStore store = new ScoreStore(d); using (GameForm f = new GameForm(store)) Assert(Sound(f).Volume == 35 && !Sound(f).Muted && store.Data.LastInitials == "OLD", "legacy preferences");
        }); });
        Check("compact states and volume render down to minimum window", delegate {
            foreach (string mode in new[] { "compact-ready", "compact", "compact-paused", "compact-gameover", "compact-volume" })
                using (GameForm f = new GameForm(new ScoreStore(Temp()))) { f.PreparePreview(mode); Paint(f, 306, 684); Paint(f, 224, 401); }
        });
        Console.WriteLine("\n" + passed + " passed, " + failed + " failed.");
        return failed == 0 ? 0 : 1;
    }
}
