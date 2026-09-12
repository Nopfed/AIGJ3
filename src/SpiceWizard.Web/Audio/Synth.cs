using System;
using Microsoft.Xna.Framework.Audio;

namespace SpiceWizard.Web.Audio
{
    /// <summary>
    /// A tiny software synthesiser. Everything the game plays is rendered from code into 16-bit mono PCM,
    /// the way the sprites are strings: there is no content pipeline and no audio files.
    /// Buffers are float arrays in -1..1 that <see cref="ToSoundEffect"/> turns into a KNI SoundEffect.
    /// </summary>
    public static class Synth
    {
        public const int Rate = 22050;
        const double TwoPi = Math.PI * 2;

        public enum Wave { Sine, Triangle, Square, Saw }

        public static float[] Buffer(double seconds) => new float[(int)(seconds * Rate)];

        /// <summary>Raw waveform sample for a phase in 0..1.</summary>
        public static float Osc(Wave wave, double phase)
        {
            phase -= Math.Floor(phase);
            switch (wave)
            {
                case Wave.Sine: return (float)Math.Sin(phase * TwoPi);
                case Wave.Triangle: return (float)(1 - 4 * Math.Abs(phase - 0.5));
                case Wave.Square: return phase < 0.5 ? 0.6f : -0.6f;
                default: return (float)(2 * phase - 1) * 0.7f;
            }
        }

        /// <summary>Attack/decay/sustain/release envelope value at t seconds into a note of length len.</summary>
        public static float Env(double t, double len, double a, double d, double s, double r)
        {
            if (t < 0) return 0;
            if (t < a) return (float)(t / a);
            if (t < a + d) return (float)(1 - (1 - s) * (t - a) / d);
            if (t < len) return (float)s;
            double rt = t - len;
            return rt < r ? (float)(s * (1 - rt / r)) : 0f;
        }

        /// <summary>Adds a note into the buffer. Vibrato and a gentle pitch drop give the plain waves some life.</summary>
        public static void Note(float[] buf, Wave wave, double freq, double start, double len, float vol,
            double a = 0.01, double d = 0.05, double s = 0.7, double r = 0.08, double vibrato = 0, double detune = 0)
        {
            int i0 = (int)(start * Rate);
            int i1 = Math.Min(buf.Length, (int)((start + len + r) * Rate));
            double phase = 0, phase2 = 0;
            for (int i = Math.Max(0, i0); i < i1; i++)
            {
                double t = (i - i0) / (double)Rate;
                double f = freq * (1 + vibrato * Math.Sin(t * TwoPi * 5.5));
                phase += f / Rate;
                float v = Osc(wave, phase);
                if (detune > 0)
                {
                    phase2 += f * (1 + detune) / Rate;
                    v = (v + Osc(wave, phase2)) * 0.5f;
                }
                buf[i] += v * vol * Env(t, len, a, d, s, r);
            }
        }

        /// <summary>A plucked string: a sine with quick decay and a touch of second harmonic.</summary>
        public static void Pluck(float[] buf, double freq, double start, double len, float vol)
        {
            int i0 = (int)(start * Rate);
            int i1 = Math.Min(buf.Length, (int)((start + len) * Rate));
            for (int i = Math.Max(0, i0); i < i1; i++)
            {
                double t = (i - i0) / (double)Rate;
                float env = (float)Math.Exp(-t * 6) * (t < 0.005 ? (float)(t / 0.005) : 1f);
                float v = (float)(Math.Sin(t * TwoPi * freq) + 0.35 * Math.Sin(t * TwoPi * freq * 2) * Math.Exp(-t * 10));
                buf[i] += v * vol * env;
            }
        }

        /// <summary>Frequency of a MIDI note number (69 = A4 = 440 Hz).</summary>
        public static double Midi(int note) => 440.0 * Math.Pow(2, (note - 69) / 12.0);

        public static void Noise(float[] buf, Random rng, float vol)
        {
            for (int i = 0; i < buf.Length; i++) buf[i] += (float)(rng.NextDouble() * 2 - 1) * vol;
        }

        /// <summary>One-pole low-pass, in place. cutoff is 0..1 as a fraction of the sample rate response.</summary>
        public static void LowPass(float[] buf, Func<int, float> cutoff)
        {
            float y = 0;
            for (int i = 0; i < buf.Length; i++)
            {
                float k = cutoff(i);
                y += k * (buf[i] - y);
                buf[i] = y;
            }
        }

        public static void LowPass(float[] buf, float cutoff) => LowPass(buf, _ => cutoff);

        /// <summary>One-pole high-pass, in place: what LowPass would have removed.</summary>
        public static void HighPass(float[] buf, float cutoff)
        {
            float y = 0;
            for (int i = 0; i < buf.Length; i++)
            {
                y += cutoff * (buf[i] - y);
                buf[i] -= y;
            }
        }

        /// <summary>
        /// Two-pole resonator: returns src rung at freq Hz with the given bandwidth. Several of these in
        /// parallel over a buzzy source make the vowel-ish formants the wizard's voice is built from.
        /// </summary>
        public static float[] Resonate(float[] src, double freq, double bandwidth, float gain)
        {
            var dst = new float[src.Length];
            double r = Math.Exp(-Math.PI * bandwidth / Rate);
            double a1 = 2 * r * Math.Cos(TwoPi * freq / Rate), a2 = -r * r;
            float g = (float)((1 - r) * gain);
            double y1 = 0, y2 = 0;
            for (int i = 0; i < src.Length; i++)
            {
                double y = src[i] * g + a1 * y1 + a2 * y2;
                y2 = y1; y1 = y;
                dst[i] = (float)y;
            }
            return dst;
        }

        public static void Mix(float[] dst, float[] src, float gain)
        {
            int n = Math.Min(dst.Length, src.Length);
            for (int i = 0; i < n; i++) dst[i] += src[i] * gain;
        }

        /// <summary>A burst of filtered noise with an exponential tail: the basis of taps, steps and crunches.</summary>
        public static void Burst(float[] buf, Random rng, double start, double len, float vol, float lowpass, double decay)
        {
            int i0 = (int)(start * Rate), n = (int)(len * Rate);
            float y = 0;
            for (int i = 0; i < n && i0 + i < buf.Length; i++)
            {
                double t = i / (double)Rate;
                float x = (float)(rng.NextDouble() * 2 - 1);
                y += lowpass * (x - y);
                buf[i0 + i] += y * vol * (float)Math.Exp(-t * decay) * (t < 0.002 ? (float)(t / 0.002) : 1f);
            }
        }

        /// <summary>Simple feedback delay for a bit of room.</summary>
        public static void Delay(float[] buf, double seconds, float feedback, float mix)
        {
            int n = (int)(seconds * Rate);
            if (n <= 0 || n >= buf.Length) return;
            for (int i = n; i < buf.Length; i++) buf[i] += buf[i - n] * feedback * mix;
        }

        /// <summary>Fades the head and tail so a loop clicks less.</summary>
        public static void FadeEnds(float[] buf, double seconds)
        {
            int n = Math.Min(buf.Length / 2, (int)(seconds * Rate));
            for (int i = 0; i < n; i++)
            {
                float g = i / (float)n;
                buf[i] *= g;
                buf[buf.Length - 1 - i] *= g;
            }
        }

        public static void Gain(float[] buf, float g)
        {
            for (int i = 0; i < buf.Length; i++) buf[i] *= g;
        }

        public static void Normalize(float[] buf, float peak = 0.9f)
        {
            float max = 0;
            foreach (var v in buf) max = Math.Max(max, Math.Abs(v));
            if (max > 0) Gain(buf, peak / max);
        }

        public static SoundEffect ToSoundEffect(float[] buf)
        {
            var pcm = new byte[buf.Length * 2];
            for (int i = 0; i < buf.Length; i++)
            {
                float v = buf[i];
                if (v > 1) v = 1; else if (v < -1) v = -1;
                short s = (short)(v * 32767);
                pcm[i * 2] = (byte)(s & 0xff);
                pcm[i * 2 + 1] = (byte)((s >> 8) & 0xff);
            }
            return new SoundEffect(pcm, Rate, AudioChannels.Mono);
        }
    }
}
