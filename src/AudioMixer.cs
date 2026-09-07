using System;
using System.Collections.Generic;
using System.Linq;

namespace NeonStack
{
    internal sealed class AudioMixer
    {
        private sealed class Voice { internal Effect Effect; internal short[] Samples; internal int Position; internal int Priority; }
        private readonly object gate = new object();
        private readonly List<Voice> voices = new List<Voice>();
        private readonly Dictionary<Effect, short[]> clips = new Dictionary<Effect, short[]>();
        private int volume = 35;
        private bool muted;
        internal int Volume { get { lock (gate) return volume; } set { lock (gate) volume = Math.Max(0, Math.Min(100, value)); } }
        internal bool Muted { get { lock (gate) return muted; } set { lock (gate) { muted = value; if (muted) voices.Clear(); } } }
        internal bool HasVoice(Effect effect) { lock (gate) return voices.Any(v => v.Effect == effect); }
        internal int VoiceCount { get { lock (gate) return voices.Count; } }
        internal void Stop() { lock (gate) voices.Clear(); }

        internal void Play(Effect effect)
        {
            lock (gate)
            {
                if (muted || volume == 0) return;
                int priority = effect == Effect.Rotate ? 0 : effect == Effect.Lock ? 1 : 2;
                if (effect == Effect.Rotate && voices.Count(v => v.Effect == effect) >= 2) return;
                if (voices.Count >= 8)
                {
                    Voice lowest = voices.OrderBy(v => v.Priority).First();
                    if (priority < lowest.Priority) return;
                    voices.Remove(lowest);
                }
                short[] clip;
                if (!clips.TryGetValue(effect, out clip))
                {
                    byte[] wave = Audio.Wave(effect, 100); clip = new short[(wave.Length - 44) / 2];
                    Buffer.BlockCopy(wave, 44, clip, 0, wave.Length - 44); clips[effect] = clip;
                }
                voices.Add(new Voice { Effect = effect, Samples = clip, Priority = priority });
            }
        }

        internal void Render(short[] output)
        {
            lock (gate)
            {
                for (int i = 0; i < output.Length; i++)
                {
                    int mixed = 0;
                    foreach (Voice voice in voices) if (voice.Position < voice.Samples.Length) mixed += voice.Samples[voice.Position++];
                    double sample = mixed * (muted ? 0 : volume / 100.0);
                    output[i] = (short)Math.Max(short.MinValue, Math.Min(short.MaxValue, sample));
                }
                voices.RemoveAll(v => v.Position >= v.Samples.Length);
            }
        }
    }
}
