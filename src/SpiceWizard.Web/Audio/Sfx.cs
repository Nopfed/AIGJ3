using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Audio;

namespace SpiceWizard.Web.Audio
{
    /// <summary>Short one-shot effects, rendered the first time each is asked for.</summary>
    public static class Sfx
    {
        public const string Click = "click", Splash = "splash", Sparkle = "sparkle", Bubble = "bubble", Thud = "thud", Chime = "chime";
        public const string Open = "open", Close = "close", Denied = "denied";
        public const string Plant = "plant", Harvest = "harvest", Coin = "coin", Cook = "cook", Jar = "jar", Grind = "grind";
        public const string Pinch = "pinch", Blend = "blend", Eat = "eat", Ship = "ship", Unship = "unship", Bucket = "bucket";
        public const string LevelUp = "levelup", Yawn = "yawn";
        public const string Step = "step", Blorp = "blorp", Mumble = "mumble", Think = "think", Affirm = "affirm";
        public const int StepVariants = 4, BlorpVariants = 3, MumbleVariants = 4, ThinkVariants = 3, AffirmVariants = 3;

        /// <summary>Name of a numbered variant: "step2", "mumble0".</summary>
        public static string Variant(string family, int n) => family + n;

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
            var rng = new Random(StableHash(name));
            if (TryRenderVariant(name, out var variant)) return variant;
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
                case Open:
                {
                    // A soft wooden tap and a little upward flourish: a panel unfolding.
                    var b = Synth.Buffer(0.25);
                    Synth.Burst(b, rng, 0, 0.05, 0.5f, 0.2f, 60);
                    Synth.Note(b, Synth.Wave.Triangle, 660, 0.01, 0.06, 0.18f, 0.005, 0.03, 0.4, 0.06);
                    Synth.Note(b, Synth.Wave.Triangle, 990, 0.06, 0.08, 0.16f, 0.005, 0.03, 0.4, 0.08);
                    return b;
                }
                case Close:
                {
                    var b = Synth.Buffer(0.22);
                    Synth.Note(b, Synth.Wave.Triangle, 880, 0, 0.05, 0.16f, 0.005, 0.03, 0.4, 0.05);
                    Synth.Note(b, Synth.Wave.Triangle, 590, 0.05, 0.07, 0.15f, 0.005, 0.03, 0.4, 0.06);
                    Synth.Burst(b, rng, 0.1, 0.05, 0.45f, 0.18f, 60);
                    return b;
                }
                case Denied:
                {
                    // Two low buzzes: "no".
                    var b = Synth.Buffer(0.3);
                    Synth.Note(b, Synth.Wave.Square, 150, 0, 0.07, 0.5f, 0.005, 0.02, 0.6, 0.03);
                    Synth.Note(b, Synth.Wave.Square, 130, 0.11, 0.09, 0.55f, 0.005, 0.02, 0.6, 0.05);
                    Synth.LowPass(b, 0.2f);
                    return b;
                }
                case Plant:
                {
                    // Dig, pat, pat.
                    var b = Synth.Buffer(0.45);
                    Synth.Burst(b, rng, 0, 0.12, 0.7f, 0.12f, 30);
                    Synth.Burst(b, rng, 0.16, 0.06, 0.5f, 0.09f, 70);
                    Synth.Burst(b, rng, 0.27, 0.06, 0.45f, 0.09f, 70);
                    Synth.Note(b, Synth.Wave.Sine, 110, 0.16, 0.04, 0.25f, 0.002, 0.03, 0.3, 0.04);
                    Synth.Note(b, Synth.Wave.Sine, 105, 0.27, 0.04, 0.22f, 0.002, 0.03, 0.3, 0.04);
                    return b;
                }
                case Harvest:
                {
                    // Snip, snip, then the pepper pops free.
                    var b = Synth.Buffer(0.5);
                    Synth.Burst(b, rng, 0, 0.03, 0.6f, 0.7f, 120);
                    Synth.Burst(b, rng, 0.09, 0.03, 0.6f, 0.7f, 120);
                    Synth.Pluck(b, 520, 0.2, 0.25, 0.3f);
                    Synth.Pluck(b, 780, 0.26, 0.24, 0.25f);
                    return b;
                }
                case Coin:
                {
                    var b = Synth.Buffer(0.5);
                    Synth.Note(b, Synth.Wave.Sine, 2093, 0, 0.05, 0.25f, 0.002, 0.04, 0.3, 0.2);
                    Synth.Note(b, Synth.Wave.Sine, 3136, 0.02, 0.05, 0.15f, 0.002, 0.04, 0.3, 0.25);
                    Synth.Note(b, Synth.Wave.Sine, 2637, 0.09, 0.06, 0.25f, 0.002, 0.04, 0.3, 0.3);
                    Synth.Note(b, Synth.Wave.Sine, 3951, 0.11, 0.05, 0.12f, 0.002, 0.04, 0.3, 0.3);
                    return b;
                }
                case Cook:
                {
                    // A whoosh of flame, a swirl and a burst of bubbles.
                    var b = Synth.Buffer(0.9);
                    var whoosh = Synth.Buffer(0.9);
                    Synth.Noise(whoosh, rng, 0.5f);
                    Synth.LowPass(whoosh, i => 0.02f + 0.3f * (float)Math.Sin(Math.Min(1, i / (double)Synth.Rate / 0.5) * Math.PI));
                    for (int i = 0; i < whoosh.Length; i++) whoosh[i] *= (float)Math.Sin(Math.Min(1, i / (double)Synth.Rate / 0.6) * Math.PI) * 1.5f;
                    Synth.Mix(b, whoosh, 1f);
                    Bubbles(b, rng, 0.3, 5, 0.25f);
                    Synth.Note(b, Synth.Wave.Sine, 330, 0.15, 0.3, 0.12f, 0.05, 0.1, 0.5, 0.2, 0.02);
                    Synth.Note(b, Synth.Wave.Sine, 495, 0.35, 0.3, 0.1f, 0.05, 0.1, 0.5, 0.2, 0.02);
                    return b;
                }
                case Jar:
                {
                    // Glass clink and a lid twisting on.
                    var b = Synth.Buffer(0.45);
                    Synth.Note(b, Synth.Wave.Sine, 2400, 0, 0.02, 0.25f, 0.001, 0.03, 0.2, 0.15);
                    Synth.Note(b, Synth.Wave.Sine, 3600, 0, 0.02, 0.12f, 0.001, 0.02, 0.2, 0.1);
                    for (int k = 0; k < 4; k++) Synth.Burst(b, rng, 0.14 + k * 0.05, 0.03, 0.3f, 0.5f, 90);
                    return b;
                }
                case Grind:
                {
                    // Pestle scraping round the bowl: gritty pulses with a stony ring.
                    var b = Synth.Buffer(0.7);
                    for (int k = 0; k < 5; k++)
                    {
                        Synth.Burst(b, rng, k * 0.12, 0.1, 0.5f, 0.35f, 25);
                        Synth.Note(b, Synth.Wave.Sine, 1800 + rng.Next(400), k * 0.12, 0.03, 0.06f, 0.002, 0.02, 0.3, 0.05);
                    }
                    Synth.HighPass(b, 0.15f);
                    return b;
                }
                case Pinch:
                {
                    var b = Synth.Buffer(0.12);
                    Synth.Burst(b, rng, 0, 0.06, 0.5f, 0.8f, 60);
                    Synth.HighPass(b, 0.4f);
                    Synth.Note(b, Synth.Wave.Sine, 1500, 0, 0.02, 0.1f, 0.002, 0.02, 0.2, 0.03);
                    return b;
                }
                case Blend:
                {
                    // Shake, shake, poof.
                    var b = Synth.Buffer(0.8);
                    for (int k = 0; k < 3; k++) { Synth.Burst(b, rng, k * 0.1, 0.06, 0.5f, 0.8f, 50); }
                    var poof = Synth.Buffer(0.8);
                    Synth.Noise(poof, rng, 0.4f);
                    Synth.LowPass(poof, 0.12f);
                    for (int i = 0; i < poof.Length; i++)
                    {
                        double t = i / (double)Synth.Rate - 0.32;
                        poof[i] *= t < 0 ? 0 : (float)(Math.Min(1, t / 0.04) * Math.Exp(-t * 6)) * 2f;
                    }
                    Synth.Mix(b, poof, 1f);
                    int[] notes = { 81, 85, 88, 93 };
                    for (int i = 0; i < notes.Length; i++)
                        Synth.Note(b, Synth.Wave.Sine, Synth.Midi(notes[i]), 0.36 + i * 0.05, 0.1, 0.16f, 0.003, 0.05, 0.4, 0.2);
                    return b;
                }
                case Eat:
                {
                    // Crunch, crunch, gulp.
                    var b = Synth.Buffer(0.7);
                    Synth.Burst(b, rng, 0, 0.08, 0.7f, 0.3f, 40);
                    Synth.Burst(b, rng, 0.15, 0.08, 0.6f, 0.3f, 40);
                    Synth.Note(b, Synth.Wave.Sine, 260, 0.36, 0.12, 0.25f, 0.02, 0.05, 0.6, 0.08);
                    Synth.Note(b, Synth.Wave.Sine, 150, 0.42, 0.12, 0.22f, 0.02, 0.05, 0.6, 0.1);
                    return b;
                }
                case Ship:
                {
                    // A bottle slides over wood and settles in the straw.
                    var b = Synth.Buffer(0.4);
                    var slide = Synth.Buffer(0.4);
                    Synth.Noise(slide, rng, 0.35f);
                    Synth.LowPass(slide, 0.25f);
                    for (int i = 0; i < slide.Length; i++) { double t = i / (double)Synth.Rate; slide[i] *= t < 0.18 ? (float)Math.Sin(t / 0.18 * Math.PI) : 0; }
                    Synth.Mix(b, slide, 1f);
                    Synth.Burst(b, rng, 0.2, 0.08, 0.6f, 0.08f, 40);
                    Synth.Note(b, Synth.Wave.Sine, 180, 0.2, 0.05, 0.25f, 0.002, 0.04, 0.3, 0.05);
                    return b;
                }
                case Unship:
                {
                    var b = Synth.Buffer(0.4);
                    Synth.Burst(b, rng, 0, 0.06, 0.5f, 0.1f, 50);
                    var slide = Synth.Buffer(0.4);
                    Synth.Noise(slide, rng, 0.35f);
                    Synth.LowPass(slide, 0.25f);
                    for (int i = 0; i < slide.Length; i++) { double t = i / (double)Synth.Rate - 0.08; slide[i] *= t > 0 && t < 0.18 ? (float)Math.Sin(t / 0.18 * Math.PI) : 0; }
                    Synth.Mix(b, slide, 1f);
                    Synth.Note(b, Synth.Wave.Sine, 2000, 0.02, 0.02, 0.1f, 0.001, 0.02, 0.2, 0.08);
                    return b;
                }
                case Bucket:
                {
                    // The well's crank creaks, then the bucket hits the water.
                    var b = Synth.Buffer(0.9);
                    for (int k = 0; k < 3; k++)
                        Synth.Note(b, Synth.Wave.Saw, 220 + k * 30, k * 0.14, 0.1, 0.12f, 0.03, 0.03, 0.7, 0.04, 0.05);
                    Synth.LowPass(b, 0.3f);
                    var splash = Synth.Buffer(0.9);
                    Synth.Noise(splash, rng, 0.6f);
                    Synth.LowPass(splash, 0.2f);
                    for (int i = 0; i < splash.Length; i++) { double t = i / (double)Synth.Rate - 0.45; splash[i] *= t < 0 ? 0 : (float)Math.Exp(-t * 9); }
                    Synth.Mix(b, splash, 1f);
                    Synth.Note(b, Synth.Wave.Sine, 480, 0.47, 0.06, 0.22f, 0.005, 0.05, 0.2, 0.06);
                    Synth.Note(b, Synth.Wave.Sine, 640, 0.53, 0.05, 0.14f, 0.005, 0.04, 0.2, 0.06);
                    return b;
                }
                case LevelUp:
                {
                    var b = Synth.Buffer(1.4);
                    int[] notes = { 72, 76, 79, 84, 79, 84, 88 };
                    double[] at = { 0, 0.1, 0.2, 0.3, 0.45, 0.55, 0.65 };
                    for (int i = 0; i < notes.Length; i++)
                    {
                        Synth.Note(b, Synth.Wave.Square, Synth.Midi(notes[i]), at[i], i == notes.Length - 1 ? 0.4 : 0.09, 0.18f, 0.005, 0.03, 0.6, 0.15, 0, 0.004);
                        Synth.Note(b, Synth.Wave.Triangle, Synth.Midi(notes[i] - 12), at[i], i == notes.Length - 1 ? 0.4 : 0.09, 0.15f, 0.005, 0.03, 0.6, 0.15);
                    }
                    Synth.Delay(b, 0.18, 0.4f, 0.5f);
                    return b;
                }
                case Yawn: return Voice.Yawn();
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

        /// <summary>Families with numbered variants so repeats do not sound like a sample on a loop.</summary>
        static bool TryRenderVariant(string name, out float[] buf)
        {
            buf = null;
            int n = 0;
            while (name.Length > 0 && char.IsDigit(name[name.Length - 1])) { n = name[name.Length - 1] - '0'; name = name.Substring(0, name.Length - 1); }
            var rng = new Random(StableHash(name) + n * 7919);
            switch (name)
            {
                case Step:
                {
                    // A boot on packed earth: a dull thump with a whisper of grit.
                    buf = Synth.Buffer(0.14);
                    Synth.Burst(buf, rng, 0, 0.1, 0.9f, 0.06f + n * 0.008f, 45);
                    Synth.Burst(buf, rng, 0.005, 0.04, 0.25f, 0.5f, 110);
                    Synth.Note(buf, Synth.Wave.Sine, 95 + n * 9, 0, 0.03, 0.22f, 0.002, 0.03, 0.2, 0.03);
                    return true;
                }
                case Blorp:
                {
                    buf = Synth.Buffer(0.3);
                    Bubbles(buf, rng, 0, 1 + n % 2, 0.35f);
                    return true;
                }
                case Mumble: buf = Voice.Mumble(n); return true;
                case Think: buf = Voice.Think(n); return true;
                case Affirm: buf = Voice.Affirm(n); return true;
            }
            return false;
        }

        /// <summary>A few rising sine blips, each pitched like a bubble breaking the surface.</summary>
        static void Bubbles(float[] b, Random rng, double start, int count, float vol)
        {
            for (int k = 0; k < count; k++)
            {
                double f0 = 350 + rng.Next(500), t0 = start + k * (0.05 + rng.NextDouble() * 0.06);
                int i0 = (int)(t0 * Synth.Rate), n = (int)(0.1 * Synth.Rate);
                double phase = 0;
                for (int i = 0; i < n && i0 + i < b.Length; i++)
                {
                    double t = i / (double)Synth.Rate;
                    phase += f0 * (1 + t * 8) / Synth.Rate;
                    b[i0 + i] += (float)Math.Sin(phase * Math.PI * 2) * vol * (float)Math.Exp(-t * 35);
                }
            }
        }

        /// <summary>string.GetHashCode is randomised per process; effects should sound the same every run.</summary>
        static int StableHash(string s)
        {
            int h = 17;
            foreach (char c in s) h = unchecked(h * 31 + c);
            return h;
        }
    }
}
