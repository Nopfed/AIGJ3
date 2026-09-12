using System;
using Microsoft.Xna.Framework.Audio;

namespace SpiceWizard.Web.Audio
{
    /// <summary>Wind, birds and crickets, all rendered from noise and sine sweeps.</summary>
    public static class Ambience
    {
        /// <summary>The wind loop is this long; <see cref="WindStrength"/> repeats with the same period so the
        /// trees lean when the gusts are audible.</summary>
        public const double WindPeriod = 16;

        /// <summary>0..1 gust strength at a moment in time: two slow sines that never quite line up.</summary>
        public static float WindStrength(double t)
        {
            double w = 0.45 + 0.3 * Math.Sin(t * Math.PI * 2 / WindPeriod) + 0.25 * Math.Sin(t * Math.PI * 2 * 3 / WindPeriod + 1.3);
            return (float)Math.Max(0, Math.Min(1, w));
        }

        static SoundEffect _wind, _crickets, _cauldron, _rain;
        static SoundEffect[] _birds;

        public static SoundEffect Wind => _wind ??= RenderWind();
        public static SoundEffect Crickets => _crickets ??= RenderCrickets();
        public static SoundEffect Cauldron => _cauldron ??= RenderCauldron();
        public static SoundEffect Rain => _rain ??= RenderRain();

        public static SoundEffect Bird(int variant)
        {
            _birds ??= new SoundEffect[4];
            return _birds[variant & 3] ??= RenderBird(variant & 3);
        }

        static SoundEffect RenderWind()
        {
            var buf = Synth.Buffer(WindPeriod);
            var rng = new Random(3);
            // Brown-ish noise: integrate white noise with leak, then open a low-pass with the gusts.
            float b = 0;
            for (int i = 0; i < buf.Length; i++)
            {
                b += (float)(rng.NextDouble() * 2 - 1) * 0.08f;
                b *= 0.985f;
                buf[i] = b;
            }
            Synth.LowPass(buf, i =>
            {
                float g = WindStrength(i / (double)Synth.Rate);
                return 0.01f + g * g * 0.09f;
            });
            for (int i = 0; i < buf.Length; i++)
                buf[i] *= 0.3f + WindStrength(i / (double)Synth.Rate);
            Synth.Normalize(buf, 0.7f);
            return Synth.ToSoundEffect(buf);
        }

        /// <summary>Steady rain: a soft hiss of filtered noise with drops pattering on the roof at random. Loops every 4 s.</summary>
        static SoundEffect RenderRain()
        {
            const double len = 4;
            var buf = Synth.Buffer(len);
            var rng = new Random(31);
            Synth.Noise(buf, rng, 0.5f);
            Synth.LowPass(buf, 0.22f);
            Synth.HighPass(buf, 0.02f);
            // The hiss swells and eases so it never reads as a flat tone.
            for (int i = 0; i < buf.Length; i++)
            {
                double t = i / (double)Synth.Rate;
                buf[i] *= 0.75f + 0.25f * (float)Math.Sin(t * Math.PI * 2 / len + 0.7 * Math.Sin(t * 3.1));
            }
            Synth.Normalize(buf, 0.3f);
            // Individual drops: short bright ticks, wrapped so the loop seam is as busy as the middle.
            for (int k = 0; k < 90; k++)
            {
                int i0 = rng.Next(buf.Length), n = (int)(0.012 * Synth.Rate);
                float vol = 0.08f + (float)rng.NextDouble() * 0.16f;
                double decay = 400 + rng.Next(500);
                for (int i = 0; i < n; i++)
                {
                    double t = i / (double)Synth.Rate;
                    buf[(i0 + i) % buf.Length] += (float)(rng.NextDouble() * 2 - 1) * vol * (float)Math.Exp(-t * decay);
                }
            }
            Synth.FadeEnds(buf, 0.01);
            return Synth.ToSoundEffect(buf);
        }

        static SoundEffect RenderCrickets()
        {
            var buf = Synth.Buffer(4);
            var rng = new Random(11);
            // Two crickets: a tone pulsed at ~40 Hz, in short bursts with gaps between them.
            Cricket(buf, rng, 4400, 41, 0.35f);
            Cricket(buf, rng, 5100, 47, 0.25f);
            Synth.FadeEnds(buf, 0.01);
            return Synth.ToSoundEffect(buf);
        }

        static void Cricket(float[] buf, Random rng, double freq, double pulseHz, float vol)
        {
            double t = rng.NextDouble() * 0.5;
            while (t < 4)
            {
                double len = 0.25 + rng.NextDouble() * 0.35;
                int i0 = (int)(t * Synth.Rate), n = (int)(len * Synth.Rate);
                for (int i = 0; i < n; i++)
                {
                    int j = (i0 + i) % buf.Length;
                    double s = i / (double)Synth.Rate;
                    float pulse = (float)Math.Max(0, Math.Sin(s * Math.PI * 2 * pulseHz));
                    float env = (float)Math.Min(1, Math.Min(s / 0.03, (len - s) / 0.05));
                    buf[j] += (float)Math.Sin(s * Math.PI * 2 * freq) * pulse * pulse * env * vol;
                }
                t += len + 0.15 + rng.NextDouble() * 0.6;
            }
        }

        /// <summary>A simmering pot: a low rumbling fizz with bubbles surfacing at random. Loops every 3 s.</summary>
        static SoundEffect RenderCauldron()
        {
            const double len = 3;
            var buf = Synth.Buffer(len);
            var rng = new Random(23);
            // The simmer: brown noise, wobbling in level so it rolls rather than hisses.
            float b = 0;
            for (int i = 0; i < buf.Length; i++)
            {
                b += (float)(rng.NextDouble() * 2 - 1) * 0.1f;
                b *= 0.97f;
                double t = i / (double)Synth.Rate;
                buf[i] = b * (0.7f + 0.3f * (float)Math.Sin(t * Math.PI * 2 * 2 / len + 0.5 * Math.Sin(t * 7)));
            }
            Synth.LowPass(buf, 0.06f);
            Synth.Normalize(buf, 0.35f);
            // Bubbles, wrapped so the ones near the end spill over into the start of the loop.
            for (int k = 0; k < 14; k++)
            {
                double f0 = 250 + rng.Next(450), t0 = rng.NextDouble() * len, decay = 25 + rng.Next(20);
                float vol = 0.15f + (float)rng.NextDouble() * 0.2f;
                int i0 = (int)(t0 * Synth.Rate), n = (int)(0.12 * Synth.Rate);
                double phase = 0;
                for (int i = 0; i < n; i++)
                {
                    double t = i / (double)Synth.Rate;
                    phase += f0 * (1 + t * 7) / Synth.Rate;
                    buf[(i0 + i) % buf.Length] += (float)Math.Sin(phase * Math.PI * 2) * vol * (float)Math.Exp(-t * decay);
                }
            }
            Synth.FadeEnds(buf, 0.01);
            return Synth.ToSoundEffect(buf);
        }

        static SoundEffect RenderBird(int variant)
        {
            var rng = new Random(100 + variant);
            int notes = 2 + variant % 3;
            var buf = Synth.Buffer(0.2 * notes + 0.1);
            double t = 0;
            for (int k = 0; k < notes; k++)
            {
                double f0 = 2200 + rng.Next(1400), f1 = f0 + (rng.Next(2) == 0 ? 900 : -600);
                double len = 0.07 + rng.NextDouble() * 0.06;
                int i0 = (int)(t * Synth.Rate), n = (int)(len * Synth.Rate);
                double phase = 0;
                for (int i = 0; i < n && i0 + i < buf.Length; i++)
                {
                    double s = i / (double)n;
                    double f = f0 + (f1 - f0) * s + 60 * Math.Sin(s * 40);
                    phase += f / Synth.Rate;
                    float env = (float)Math.Sin(s * Math.PI);
                    buf[i0 + i] += (float)Math.Sin(phase * Math.PI * 2) * env * 0.5f;
                }
                t += len + 0.04 + rng.NextDouble() * 0.08;
            }
            return Synth.ToSoundEffect(buf);
        }
    }
}
