using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Audio;

namespace SpiceWizard.Web.Audio
{
    /// <summary>Short one-shot effects, rendered the first time each is asked for.</summary>
    public static class Sfx
    {
        public const string Click = "click", Splash = "splash", Sparkle = "sparkle", Bubble = "bubble", Thud = "thud", Chime = "chime";

        static readonly Dictionary<string, SoundEffect> _cache = new Dictionary<string, SoundEffect>();

        public static SoundEffect Get(string name)
        {
            if (!_cache.TryGetValue(name, out var fx))
            {
                fx = Synth.ToSoundEffect(Render(name));
                _cache[name] = fx;
            }
            return fx;
        }

        static float[] Render(string name)
        {
            var rng = new Random(name.GetHashCode());
            switch (name)
            {
                case Click:
                {
                    var b = Synth.Buffer(0.08);
                    Synth.Note(b, Synth.Wave.Square, 880, 0, 0.03, 0.25f, 0.002, 0.02, 0.3, 0.03);
                    Synth.Note(b, Synth.Wave.Sine, 1320, 0.01, 0.02, 0.2f, 0.002, 0.01, 0.2, 0.03);
                    return b;
                }
                case Splash:
                {
                    var b = Synth.Buffer(0.45);
                    Synth.Noise(b, rng, 0.6f);
                    Synth.LowPass(b, i => 0.05f + 0.25f * (float)Math.Exp(-i / (double)Synth.Rate * 8));
                    for (int i = 0; i < b.Length; i++) b[i] *= (float)Math.Exp(-i / (double)Synth.Rate * 7);
                    Synth.Note(b, Synth.Wave.Sine, 520, 0.02, 0.06, 0.25f, 0.005, 0.05, 0.2, 0.05);
                    Synth.Note(b, Synth.Wave.Sine, 700, 0.09, 0.05, 0.15f, 0.005, 0.04, 0.2, 0.05);
                    return b;
                }
                case Sparkle:
                {
                    var b = Synth.Buffer(0.5);
                    int[] notes = { 84, 88, 91, 96 };
                    for (int i = 0; i < notes.Length; i++)
                        Synth.Note(b, Synth.Wave.Sine, Synth.Midi(notes[i]), i * 0.06, 0.12, 0.25f, 0.003, 0.05, 0.4, 0.15);
                    return b;
                }
                case Bubble:
                {
                    var b = Synth.Buffer(0.25);
                    for (int k = 0; k < 3; k++)
                    {
                        double f0 = 500 + rng.Next(400), start = k * 0.07;
                        int i0 = (int)(start * Synth.Rate), n = (int)(0.1 * Synth.Rate);
                        double phase = 0;
                        for (int i = 0; i < n && i0 + i < b.Length; i++)
                        {
                            double t = i / (double)Synth.Rate;
                            phase += f0 * (1 + t * 8) / Synth.Rate;
                            b[i0 + i] += (float)Math.Sin(phase * Math.PI * 2) * 0.3f * (float)Math.Exp(-t * 35);
                        }
                    }
                    return b;
                }
                case Thud:
                {
                    var b = Synth.Buffer(0.3);
                    Synth.Noise(b, rng, 0.5f);
                    Synth.LowPass(b, 0.04f);
                    for (int i = 0; i < b.Length; i++) b[i] *= (float)Math.Exp(-i / (double)Synth.Rate * 18) * 3f;
                    Synth.Note(b, Synth.Wave.Sine, 90, 0, 0.12, 0.5f, 0.002, 0.1, 0.3, 0.08);
                    return b;
                }
                default: // Chime
                {
                    var b = Synth.Buffer(0.8);
                    Synth.Note(b, Synth.Wave.Sine, Synth.Midi(79), 0, 0.3, 0.3f, 0.005, 0.2, 0.5, 0.4);
                    Synth.Note(b, Synth.Wave.Sine, Synth.Midi(86), 0.12, 0.3, 0.25f, 0.005, 0.2, 0.5, 0.4);
                    Synth.Note(b, Synth.Wave.Triangle, Synth.Midi(91), 0.24, 0.3, 0.2f, 0.005, 0.2, 0.5, 0.4);
                    return b;
                }
            }
        }
    }
}
