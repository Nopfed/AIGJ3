using System;
using System.IO;
using SpiceWizard.Web.Audio;

namespace Microsoft.Xna.Framework.Audio
{
    public enum AudioChannels { Mono = 1, Stereo = 2 }
    /// <summary>Stub of KNI's SoundEffect that just keeps the PCM so it can be written out as a WAV.</summary>
    public class SoundEffect
    {
        public readonly byte[] Pcm; public readonly int Rate; public readonly AudioChannels Channels;
        public SoundEffect(byte[] pcm, int rate, AudioChannels ch) { Pcm = pcm; Rate = rate; Channels = ch; }
        public void Save(string path)
        {
            using var w = new BinaryWriter(File.Create(path));
            int ch = (int)Channels;
            w.Write("RIFF".ToCharArray()); w.Write(36 + Pcm.Length); w.Write("WAVE".ToCharArray());
            w.Write("fmt ".ToCharArray()); w.Write(16); w.Write((short)1); w.Write((short)ch);
            w.Write(Rate); w.Write(Rate * ch * 2); w.Write((short)(ch * 2)); w.Write((short)16);
            w.Write("data".ToCharArray()); w.Write(Pcm.Length); w.Write(Pcm);
        }
    }
}

static class Program
{
    static void Main(string[] args)
    {
        string dir = args.Length > 0 ? args[0] : ".";
        Directory.CreateDirectory(dir);
        foreach (var t in Music.Tracks) Dump(dir, "music_" + t.Name, t.Effect);
        Dump(dir, "music_" + Music.MothLamp.Name, Music.MothLamp.Effect);
        Dump(dir, "music_" + Music.Festival.Name, Music.Festival.Effect);
        Dump(dir, "amb_wind", Ambience.Wind); Dump(dir, "amb_crickets", Ambience.Crickets);
        Dump(dir, "amb_cauldron", Ambience.Cauldron); Dump(dir, "amb_rain", Ambience.Rain); Dump(dir, "amb_hiss", Ambience.Hiss);
        for (int i = 0; i < 4; i++) Dump(dir, "amb_bird" + i, Ambience.Bird(i));
        foreach (var n in new[] { Sfx.Splash, Sfx.Sparkle, Sfx.Bubble, Sfx.Chime, Sfx.Harvest, Sfx.Coin, Sfx.Cook, Sfx.Jar, Sfx.Grind,
                                  Sfx.Ship, Sfx.LevelUp, Sfx.Fanfare, Sfx.Cheer, Sfx.Cart, Sfx.Purr, Sfx.Bucket, Sfx.Plant, Sfx.Blend,
                                  Sfx.Variant(Sfx.Meow, 0), Sfx.Variant(Sfx.Blorp, 0), Sfx.Variant(Sfx.Step, 0) })
            Dump(dir, "sfx_" + n, Sfx.Get(n));
    }
    static void Dump(string dir, string name, Microsoft.Xna.Framework.Audio.SoundEffect fx)
    {
        string path = Path.Combine(dir, name.Replace(' ', '_').ToLowerInvariant() + ".wav");
        fx.Save(path);
        Console.WriteLine($"{path}  {fx.Pcm.Length / 2.0 / fx.Rate:F2}s");
    }
}
