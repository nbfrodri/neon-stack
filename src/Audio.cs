using System;
using System.Collections.Generic;
using System.IO;
using System.Media;

namespace NeonStack
{
    internal enum Effect { Rotate, Lock, Clear, Tetris, Start, Over }

    internal sealed class Audio : IDisposable
    {
        private readonly bool enabled;
        private readonly AudioMixer mixer = new AudioMixer();
        private WaveOutput output;
        private bool failed;
        public bool Available { get { return !failed && (output == null || output.Available); } }
        public int Volume { get { return mixer.Volume; } set { mixer.Volume = value; } }
        public bool Muted { get { return mixer.Muted; } set { mixer.Muted = value; } }
        public Audio(bool enabled) { this.enabled = enabled; }

        public void Play(Effect effect)
        {
            if (!enabled || !Available || Muted || Volume == 0) return;
            try
            {
                mixer.Play(effect);
                if (output == null) output = new WaveOutput(mixer);
            }
            catch (InvalidOperationException) { failed = true; }
            catch (System.ComponentModel.Win32Exception) { failed = true; }
            catch (DllNotFoundException) { failed = true; }
        }

        public void Stop() { mixer.Stop(); }
        public void Dispose() { Stop(); if (output != null) output.Dispose(); }

        internal static byte[] Wave(Effect effect, int volume)
        {
            double[] notes;
            double duration;
            switch (effect)
            {
                case Effect.Rotate: notes = new[] { 740.0, 990 }; duration = .034; break;
                case Effect.Lock: notes = new[] { 165.0, 110 }; duration = .045; break;
                case Effect.Clear: notes = new[] { 523.25, 659.25, 783.99 }; duration = .07; break;
                case Effect.Tetris: notes = new[] { 523.25, 659.25, 783.99, 1046.5 }; duration = .085; break;
                case Effect.Start: notes = new[] { 392.0, 523.25, 783.99 }; duration = .075; break;
                default: notes = new[] { 392.0, 329.63, 261.63, 130.81 }; duration = .11; break;
            }
            const int rate = 22050;
            int perNote = (int)(duration * rate), samples = perNote * notes.Length;
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + samples * 2);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16);
                writer.Write((short)1); writer.Write((short)1); writer.Write(rate); writer.Write(rate * 2);
                writer.Write((short)2); writer.Write((short)16);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(samples * 2);
                double gain = Math.Max(0, Math.Min(100, volume)) / 100.0 * .24;
                foreach (double frequency in notes)
                    for (int i = 0; i < perNote; i++)
                    {
                        double t = i / (double)rate;
                        // Two harmonics and a short envelope retain a chiptune timbre without clicks.
                        double wave = Math.Sin(2 * Math.PI * frequency * t) + .25 * Math.Sin(6 * Math.PI * frequency * t);
                        double envelope = Math.Min(1, t / .004) * Math.Min(1, (perNote - 1 - i) / (rate * .018));
                        writer.Write((short)(wave * envelope * gain * short.MaxValue));
                    }
                return stream.ToArray();
            }
        }
    }
}
