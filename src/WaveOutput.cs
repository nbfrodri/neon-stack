using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace NeonStack
{
    // One device stream, three reusable buffers. Mixing never runs on the UI thread.
    internal sealed class WaveOutput : IDisposable
    {
        [StructLayout(LayoutKind.Sequential, Pack = 2)]
        private struct Format { public ushort Tag, Channels; public uint Rate, BytesPerSecond; public ushort Block, Bits, Extra; }
        [StructLayout(LayoutKind.Sequential)]
        private struct Header { public IntPtr Data; public uint Length, Recorded; public UIntPtr User; public uint Flags, Loops; public IntPtr Next; public UIntPtr Reserved; }
        [DllImport("winmm.dll")] private static extern uint waveOutOpen(out IntPtr device, uint id, ref Format format, IntPtr callback, IntPtr instance, uint flags);
        [DllImport("winmm.dll")] private static extern uint waveOutPrepareHeader(IntPtr device, IntPtr header, uint size);
        [DllImport("winmm.dll")] private static extern uint waveOutWrite(IntPtr device, IntPtr header, uint size);
        [DllImport("winmm.dll")] private static extern uint waveOutReset(IntPtr device);
        [DllImport("winmm.dll")] private static extern uint waveOutUnprepareHeader(IntPtr device, IntPtr header, uint size);
        [DllImport("winmm.dll")] private static extern uint waveOutClose(IntPtr device);
        private IntPtr device;
        private readonly IntPtr[] headers = new IntPtr[3], data = new IntPtr[3];
        private readonly bool[] prepared = new bool[3];
        private readonly AudioMixer mixer;
        private readonly short[] samples = new short[256];
        private readonly ManualResetEvent stopped = new ManualResetEvent(false);
        private readonly uint headerSize = (uint)Marshal.SizeOf(typeof(Header));
        private readonly int flagsOffset = (int)Marshal.OffsetOf(typeof(Header), "Flags");
        private Thread thread;
        private volatile bool available;
        internal bool Available { get { return available; } }

        internal WaveOutput(AudioMixer mixer)
        {
            this.mixer = mixer;
            Format format = new Format { Tag = 1, Channels = 1, Rate = 22050, BytesPerSecond = 44100, Block = 2, Bits = 16 };
            if (waveOutOpen(out device, uint.MaxValue, ref format, IntPtr.Zero, IntPtr.Zero, 0) != 0) return;
            for (int i = 0; i < 3; i++)
            {
                data[i] = Marshal.AllocHGlobal(samples.Length * 2); headers[i] = Marshal.AllocHGlobal((int)headerSize);
                Marshal.StructureToPtr(new Header { Data = data[i], Length = (uint)samples.Length * 2 }, headers[i], false);
                if (waveOutPrepareHeader(device, headers[i], headerSize) != 0) { Dispose(); return; }
                prepared[i] = true;
                if (!Queue(i)) { Dispose(); return; }
            }
            available = true;
            thread = new Thread(Run) { IsBackground = true, Name = "NeonStack audio" }; thread.Start();
        }

        private bool Queue(int index)
        {
            mixer.Render(samples); Marshal.Copy(samples, 0, data[index], samples.Length);
            return waveOutWrite(device, headers[index], headerSize) == 0;
        }

        private void Run()
        {
            while (!stopped.WaitOne(4))
                for (int i = 0; i < headers.Length; i++)
                    if ((Marshal.ReadInt32(headers[i], flagsOffset) & 1) != 0 && !Queue(i)) { available = false; return; }
        }

        private bool disposed;
        public void Dispose()
        {
            if (disposed) return; disposed = true; available = false;
            stopped.Set(); if (thread != null) thread.Join();
            if (device != IntPtr.Zero)
            {
                waveOutReset(device);
                for (int i = 0; i < headers.Length; i++)
                {
                    if (prepared[i]) waveOutUnprepareHeader(device, headers[i], headerSize);
                    if (headers[i] != IntPtr.Zero) Marshal.FreeHGlobal(headers[i]);
                    if (data[i] != IntPtr.Zero) Marshal.FreeHGlobal(data[i]);
                }
                waveOutClose(device); device = IntPtr.Zero;
            }
            stopped.Dispose();
        }
    }
}
