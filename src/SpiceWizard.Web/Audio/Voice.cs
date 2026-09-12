using System;

namespace SpiceWizard.Web.Audio
{
    /// <summary>
    /// The wizard's voice: a buzzy glottal source pushed through two or three formant resonators, one
    /// syllable at a time. Nothing here is a word; it is the hum, "hmm?" and "a-ha!" of somebody pottering
    /// about who thinks nobody is listening.
    /// </summary>
    public static class Voice
    {
        /// <summary>Formant pairs (F1, F2) for the handful of mouth shapes the wizard uses.</summary>
        public enum Mouth { Mm, Uh, Ah, Oh, Ee }

        static (double f1, double f2, double bright) Shape(Mouth m) => m switch
        {
            Mouth.Mm => (250, 1000, 0.15),
            Mouth.Uh => (520, 1150, 0.6),
            Mouth.Ah => (720, 1250, 1.0),
            Mouth.Oh => (450, 850, 0.7),
            _ => (300, 2200, 0.8),
        };

        public struct Syllable
        {
            public Mouth Mouth;
            public double Start, Len;
            public double PitchFrom, PitchTo; // Hz
            public float Vol;
        }

        /// <summary>Renders syllables into a fresh buffer. base pitch is where the wizard's voice sits (about 140 Hz).</summary>
        public static float[] Render(Syllable[] syllables, Random rng)
        {
            double end = 0;
            foreach (var s in syllables) end = Math.Max(end, s.Start + s.Len + 0.12);
            var src = Synth.Buffer(end);
            var mouth = new float[syllables.Length][];

            for (int k = 0; k < syllables.Length; k++)
            {
                var s = syllables[k];
                var one = Synth.Buffer(end);
                int i0 = (int)(s.Start * Synth.Rate), n = (int)((s.Len + 0.1) * Synth.Rate);
                double phase = 0, jitter = 0;
                for (int i = 0; i < n && i0 + i < one.Length; i++)
                {
                    double t = i / (double)Synth.Rate;
                    double u = Math.Min(1, t / s.Len);
                    if (i % 220 == 0) jitter = (rng.NextDouble() - 0.5) * 0.04;
                    double f = (s.PitchFrom + (s.PitchTo - s.PitchFrom) * u) * (1 + jitter + 0.03 * Math.Sin(t * Math.PI * 2 * 5.5));
                    phase += f / Synth.Rate;
                    double ph = phase - Math.Floor(phase);
                    // A narrow pulse has all the harmonics the formants need; the saw part keeps the low end.
                    float pulse = ph < 0.12 ? 1f : -0.15f;
                    float saw = (float)(2 * ph - 1);
                    float env = Synth.Env(t, s.Len, 0.03, 0.05, 0.85, 0.1);
                    one[i0 + i] = (pulse * 0.6f + saw * 0.4f) * env * s.Vol;
                }
                mouth[k] = one;
            }

            var outBuf = Synth.Buffer(end);
            for (int k = 0; k < syllables.Length; k++)
            {
                var (f1, f2, bright) = Shape(syllables[k].Mouth);
                Synth.Mix(outBuf, Synth.Resonate(mouth[k], f1, 90, 1f), 1f);
                Synth.Mix(outBuf, Synth.Resonate(mouth[k], f2, 140, (float)bright), 0.5f);
                Synth.Mix(outBuf, Synth.Resonate(mouth[k], 2600, 220, (float)bright), 0.18f);
                Synth.Mix(outBuf, mouth[k], 0.08f); // a little raw buzz so it is not all filter
            }
            Synth.LowPass(outBuf, 0.55f);
            Synth.Normalize(outBuf, 0.6f);
            return outBuf;
        }

        static Syllable S(Mouth m, double start, double len, double from, double to, float vol = 1f) =>
            new Syllable { Mouth = m, Start = start, Len = len, PitchFrom = from, PitchTo = to, Vol = vol };

        /// <summary>A few closed-mouth syllables wandering around the base pitch: humming a half-remembered tune.</summary>
        public static float[] Mumble(int variant)
        {
            var rng = new Random(500 + variant);
            int n = 3 + rng.Next(3);
            var list = new Syllable[n];
            double t = 0, p = 130 + rng.Next(30);
            Mouth[] closed = { Mouth.Mm, Mouth.Mm, Mouth.Uh, Mouth.Oh };
            for (int k = 0; k < n; k++)
            {
                double len = 0.12 + rng.NextDouble() * 0.16;
                double p2 = p * (1 + (rng.NextDouble() - 0.5) * 0.18);
                list[k] = S(closed[rng.Next(closed.Length)], t, len, p, p2, 0.8f + (float)rng.NextDouble() * 0.2f);
                t += len + 0.02 + rng.NextDouble() * 0.05;
                p = p2;
            }
            return Render(list, rng);
        }

        /// <summary>"Hmmm?" - one long closed syllable that rises at the end, or "hm-hmm" that dips.</summary>
        public static float[] Think(int variant)
        {
            var rng = new Random(600 + variant);
            double p = 135 + rng.Next(25);
            switch (variant % 3)
            {
                case 0: return Render(new[] { S(Mouth.Mm, 0, 0.55, p, p * 1.35) }, rng);
                case 1: return Render(new[] { S(Mouth.Mm, 0, 0.18, p, p * 0.95), S(Mouth.Mm, 0.22, 0.4, p * 0.95, p * 1.25) }, rng);
                default: return Render(new[] { S(Mouth.Uh, 0, 0.3, p * 1.1, p), S(Mouth.Mm, 0.34, 0.35, p, p * 0.9) }, rng);
            }
        }

        /// <summary>"A-ha!", "mm-hm" and "oh ho": the little noises of a plan coming together.</summary>
        public static float[] Affirm(int variant)
        {
            var rng = new Random(700 + variant);
            double p = 140 + rng.Next(25);
            switch (variant % 3)
            {
                case 0: return Render(new[] { S(Mouth.Ah, 0, 0.12, p, p * 1.05, 0.8f), S(Mouth.Ah, 0.16, 0.28, p * 1.5, p * 1.15) }, rng);
                case 1: return Render(new[] { S(Mouth.Mm, 0, 0.14, p, p * 1.02, 0.7f), S(Mouth.Mm, 0.18, 0.2, p * 1.3, p * 1.2) }, rng);
                default: return Render(new[] { S(Mouth.Oh, 0, 0.18, p * 1.2, p * 1.1), S(Mouth.Oh, 0.24, 0.22, p * 1.45, p * 1.25) }, rng);
            }
        }

        /// <summary>A yawn for bedtime: a long open vowel sliding down and closing to a hum.</summary>
        public static float[] Yawn()
        {
            var rng = new Random(800);
            return Render(new[] { S(Mouth.Ah, 0, 0.5, 190, 150, 0.9f), S(Mouth.Oh, 0.5, 0.35, 150, 120, 0.7f), S(Mouth.Mm, 0.85, 0.3, 120, 105, 0.5f) }, rng);
        }
    }
}
