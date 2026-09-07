using System;
using System.IO;
using System.Threading;
using NeonStack;

internal static class AudioCheck
{
    private static int Main(string[] args)
    {
        string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "audio-mix.wav");
        AudioMixer mixer = new AudioMixer(); mixer.Play(Effect.Tetris);
        short[] samples = new short[22050];
        for (int i = 0; i < 50; i++)
        {
            if (i == 3 || i == 7 || i == 11) mixer.Play(Effect.Rotate);
            short[] block = new short[441]; mixer.Render(block); Array.Copy(block, 0, samples, i * 441, 441);
        }
        using (BinaryWriter writer = new BinaryWriter(File.Create(path)))
        {
            byte[] header = Audio.Wave(Effect.Clear, 35);
            Buffer.BlockCopy(BitConverter.GetBytes(36 + samples.Length * 2), 0, header, 4, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(samples.Length * 2), 0, header, 40, 4);
            writer.Write(header, 0, 44); foreach (short sample in samples) writer.Write(sample);
        }
        Console.WriteLine("Mezcla de prueba: " + path);
        if (args.Length == 0 || args[0] != "--listen") return 0;
        using (Audio audio = new Audio(true))
        {
            Console.WriteLine("Reproduciendo: melodia Tetris + tres giros simultaneos, silencio y melodia final.");
            audio.Play(Effect.Tetris);
            if (!audio.Available) { Console.Error.WriteLine("No hay salida de audio disponible."); return 1; }
            for (int i = 0; i < 3; i++) { Thread.Sleep(70); audio.Play(Effect.Rotate); }
            Thread.Sleep(450); audio.Muted = true; audio.Play(Effect.Lock); Thread.Sleep(400);
            audio.Muted = false; audio.Volume = 25; audio.Play(Effect.Start); Thread.Sleep(500);
            if (!audio.Available) return 1;
            Console.WriteLine("Salida nativa completada sin errores del dispositivo.");
        }
        return 0;
    }
}
