using System;
using System.Collections.Generic;

namespace NeonStack
{
    public enum Phase { Ready, Playing, Paused, GameOver, Completed }
    public enum GameMode { Classic, Sprint40 }

    public struct Cell
    {
        public int X, Y;
        public Cell(int x, int y) { X = x; Y = y; }
    }

    public sealed class Game
    {
        public const int Width = 10, Height = 20;
        public const double LockDelay = 0.3;
        public const int MaxLockResets = 6;
        public readonly int[,] Board = new int[Height, Width];
        public Phase State { get; private set; }
        public GameMode Mode { get; private set; }
        public double ElapsedSeconds { get; private set; }
        public int RemainingLines { get { return Math.Max(0, 40 - Lines); } }
        public int Kind { get; private set; }
        public int Rotation { get; private set; }
        public int X { get; private set; }
        public int Y { get; private set; }
        public int Score { get; private set; }
        public int Lines { get; private set; }
        public int Level { get { return Lines / 10 + 1; } }
        public int Pieces { get; private set; }
        public int LockResets { get; private set; }
        public double LockElapsed { get; private set; }
        public double LockFraction { get { return Math.Min(1, LockElapsed / LockDelay); } }
        public bool Grounded { get { return !Fits(Kind, Rotation, X, Y + 1); } }
        public int GhostY { get { int y = Y; while (Fits(Kind, Rotation, X, y + 1)) y++; return y; } }
        public double GravityInterval { get { return Math.Max(0.06, 0.8 * Math.Pow(0.80, Level - 1)); } }
        public int[] Next { get { return queue.ToArray(); } }
        public event Action<int[]> RowsCleared;
        public event Action PieceLocked;
        private readonly Random random;
        private readonly Queue<int> queue = new Queue<int>();
        private double gravity;

        // I, O, T, S, Z, J, L. Coordinates use the standard SRS spawn boxes.
        private static readonly Cell[][] SpawnCells = {
            new[] { new Cell(0,1), new Cell(1,1), new Cell(2,1), new Cell(3,1) },
            new[] { new Cell(1,0), new Cell(2,0), new Cell(1,1), new Cell(2,1) },
            new[] { new Cell(1,0), new Cell(0,1), new Cell(1,1), new Cell(2,1) },
            new[] { new Cell(1,0), new Cell(2,0), new Cell(0,1), new Cell(1,1) },
            new[] { new Cell(0,0), new Cell(1,0), new Cell(1,1), new Cell(2,1) },
            new[] { new Cell(0,0), new Cell(0,1), new Cell(1,1), new Cell(2,1) },
            new[] { new Cell(2,0), new Cell(0,1), new Cell(1,1), new Cell(2,1) }
        };
        private static readonly Cell[][][] Shapes = MakeShapes();

        public Game(int seed) { random = new Random(seed); Kind = 2; State = Phase.Ready; FillQueue(); }
        public Game() : this(Environment.TickCount) { }

        private static Cell[][][] MakeShapes()
        {
            Cell[][][] result = new Cell[7][][];
            for (int kind = 0; kind < 7; kind++)
            {
                result[kind] = new Cell[4][];
                result[kind][0] = SpawnCells[kind];
                for (int r = 1; r < 4; r++)
                {
                    result[kind][r] = new Cell[4];
                    for (int i = 0; i < 4; i++)
                    {
                        Cell c = result[kind][r - 1][i];
                        result[kind][r][i] = kind == 1 ? c : new Cell((kind == 0 ? 3 : 2) - c.Y, c.X);
                    }
                }
            }
            return result;
        }

        public static Cell[] Cells(int kind, int rotation) { return Shapes[kind][rotation]; }

        // Symmetric pieces alternate between two canonical poses. This retro rule avoids
        // translating I/S/Z by a cell when a second turn restores their silhouette.
        public static int RotationCount(int kind) { return kind == 1 ? 1 : (kind == 0 || kind == 3 || kind == 4 ? 2 : 4); }

        private void FillQueue()
        {
            while (queue.Count < 7)
            {
                int[] bag = { 0, 1, 2, 3, 4, 5, 6 };
                for (int i = 6; i > 0; i--) { int j = random.Next(i + 1); int t = bag[i]; bag[i] = bag[j]; bag[j] = t; }
                foreach (int k in bag) queue.Enqueue(k);
            }
        }

        public void Start()
        {
            Array.Clear(Board, 0, Board.Length);
            Score = Lines = Pieces = 0;
            ElapsedSeconds = 0;
            queue.Clear(); FillQueue(); State = Phase.Playing; Spawn();
        }

        public void SelectMode(GameMode mode)
        {
            if (State == Phase.Playing || State == Phase.Paused) return;
            Mode = mode; State = Phase.Ready; Score = Lines = Pieces = 0; ElapsedSeconds = 0;
            Array.Clear(Board, 0, Board.Length);
        }

        private void Spawn()
        {
            Kind = queue.Dequeue(); FillQueue(); Rotation = 0; X = 3; Y = -1;
            gravity = LockElapsed = 0; LockResets = 0;
            if (!Fits(Kind, Rotation, X, Y)) State = Phase.GameOver;
        }

        public bool Fits(int kind, int rotation, int x, int y)
        {
            foreach (Cell c in Cells(kind, rotation))
            {
                int cx = x + c.X, cy = y + c.Y;
                if (cx < 0 || cx >= Width || cy >= Height || cy < -4) return false;
                if (cy >= 0 && Board[cy, cx] != 0) return false;
            }
            return true;
        }

        public bool Move(int direction)
        {
            if (State != Phase.Playing || (direction != -1 && direction != 1)) return false;
            bool grounded = Grounded;
            if (!Fits(Kind, Rotation, X + direction, Y)) return false;
            X += direction; ResetLock(grounded); return true;
        }

        private void ResetLock(bool wasGrounded)
        {
            if (wasGrounded && LockResets < MaxLockResets) { LockElapsed = 0; LockResets++; }
        }

        // SRS offset data: differences between the old and new orientation give each kick.
        private static readonly Cell[][] NormalOffsets = {
            new[] { new Cell(0,0), new Cell(0,0), new Cell(0,0), new Cell(0,0), new Cell(0,0) },
            new[] { new Cell(0,0), new Cell(1,0), new Cell(1,-1), new Cell(0,2), new Cell(1,2) },
            new[] { new Cell(0,0), new Cell(0,0), new Cell(0,0), new Cell(0,0), new Cell(0,0) },
            new[] { new Cell(0,0), new Cell(-1,0), new Cell(-1,-1), new Cell(0,2), new Cell(-1,2) }
        };
        private static readonly Cell[][] IOffsets = {
            new[] { new Cell(0,0), new Cell(-1,0), new Cell(2,0), new Cell(-1,0), new Cell(2,0) },
            new[] { new Cell(-1,0), new Cell(0,0), new Cell(0,0), new Cell(0,1), new Cell(0,-2) },
            new[] { new Cell(-1,1), new Cell(1,1), new Cell(-2,1), new Cell(1,0), new Cell(-2,0) },
            new[] { new Cell(0,1), new Cell(0,1), new Cell(0,1), new Cell(0,-1), new Cell(0,2) }
        };

        public bool Rotate(int direction)
        {
            if (State != Phase.Playing || Kind == 1 || (direction != -1 && direction != 1)) return false;
            int count = RotationCount(Kind);
            int next = (Rotation + direction + count) % count;
            bool grounded = Grounded;
            // I offsets include orientation translations; subtract the first test to match
            // the fixed 4x4 matrix representation used by Shapes.
            Cell[][] offsets = Kind == 0 ? IOffsets : NormalOffsets;
            int baseX = offsets[Rotation][0].X - offsets[next][0].X;
            int baseY = offsets[Rotation][0].Y - offsets[next][0].Y;
            for (int i = 0; i < 5; i++)
            {
                int dx = offsets[Rotation][i].X - offsets[next][i].X - baseX;
                int dy = -(offsets[Rotation][i].Y - offsets[next][i].Y - baseY);
                if (!Fits(Kind, next, X + dx, Y + dy)) continue;
                X += dx; Y += dy; Rotation = next; ResetLock(grounded); return true;
            }
            return false;
        }

        public bool SoftDrop()
        {
            if (State != Phase.Playing || Grounded) return false;
            Y++; Score++; gravity = 0; return true;
        }

        public void HardDrop()
        {
            if (State != Phase.Playing) return;
            int target = GhostY; Score += (target - Y) * 2; Y = target; Lock();
        }

        public void Tick(double elapsed, double simulationLimit = double.MaxValue)
        {
            if (State != Phase.Playing || elapsed <= 0 || double.IsNaN(elapsed) || double.IsInfinity(elapsed)) return;
            ElapsedSeconds += elapsed;
            elapsed = Math.Min(elapsed, simulationLimit);
            // Fixed-size slices make collision and lock timing independent of render cadence.
            while (elapsed > 0 && State == Phase.Playing)
            {
                double step = Math.Min(elapsed, 0.01); elapsed -= step;
                if (Grounded)
                {
                    LockElapsed += step;
                    if (LockElapsed + 0.0000001 >= LockDelay) { Lock(); return; }
                }
                else
                {
                    gravity += step;
                    if (gravity >= GravityInterval) { gravity -= GravityInterval; Y++; }
                }
            }
        }

        private void Lock()
        {
            foreach (Cell c in Cells(Kind, Rotation))
            {
                if (Y + c.Y < 0) { State = Phase.GameOver; return; }
            }
            foreach (Cell c in Cells(Kind, Rotation)) Board[Y + c.Y, X + c.X] = Kind + 1;
            Pieces++;
            List<int> cleared = new List<int>();
            for (int row = 0; row < Height; row++)
            {
                bool full = true;
                for (int col = 0; col < Width; col++) if (Board[row, col] == 0) { full = false; break; }
                if (full) cleared.Add(row);
            }
            int write = Height - 1;
            for (int row = Height - 1; row >= 0; row--)
            {
                if (cleared.Contains(row)) continue;
                for (int col = 0; col < Width; col++) Board[write, col] = Board[row, col];
                write--;
            }
            for (; write >= 0; write--) for (int col = 0; col < Width; col++) Board[write, col] = 0;
            if (cleared.Count > 0)
            {
                Score += new[] { 0, 100, 300, 500, 800 }[cleared.Count] * Level;
                Lines += cleared.Count;
                if (RowsCleared != null) RowsCleared(cleared.ToArray());
            }
            if (PieceLocked != null) PieceLocked();
            if (Mode == GameMode.Sprint40 && Lines >= 40) { State = Phase.Completed; return; }
            Spawn();
        }

        public void Pause() { if (State == Phase.Playing) State = Phase.Paused; }
        public void Resume() { if (State == Phase.Paused) State = Phase.Playing; }
        public void EndRun() { if (State == Phase.Playing || State == Phase.Paused) State = Phase.GameOver; }
    }
}
