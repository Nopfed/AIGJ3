using System;
using Microsoft.Xna.Framework.Audio;

namespace SpiceWizard.Web.Audio
{
    /// <summary>
    /// The three daytime tunes, the night lullaby and the celebration tune, written as note lists and
    /// rendered through <see cref="Synth"/> the first time they are needed. Each loops seamlessly; the
    /// mixer picks a daytime one per day and brings the others in for the evening and the mastery party.
    /// </summary>
    public sealed class Track
    {
        public string Name;
        public Func<float[]> Render;
        SoundEffect _effect;

        public SoundEffect Effect => _effect ??= Synth.ToSoundEffect(Render());
        public bool IsRendered => _effect != null;
    }

    public static class Music
    {
        public static readonly Track[] Tracks =
        {
            new Track { Name = "Morning Meadow", Render = MorningMeadow },
            new Track { Name = "Simmering Pot", Render = SimmeringPot },
            new Track { Name = "Turmeric Sun", Render = TurmericSun },
        };

        /// <summary>The evening lullaby: plays from 20:00 and while the wizard is deciding to turn in.</summary>
        public static readonly Track MothLamp = new Track { Name = "Moth Lamp", Render = RenderMothLamp };

        /// <summary>The town's party tune, for as long as the crowd is in the yard.</summary>
        public static readonly Track Festival = new Track { Name = "Festival", Render = RenderFestival };

        // A note is (midi, start beat, length in beats).
        struct N { public int P; public double S, L; public N(int p, double s, double l) { P = p; S = s; L = l; } }

        // ---- 1. Morning Meadow: a G major waltz, plucked bass and a bright triangle lead ----------------

        static float[] MorningMeadow()
        {
            const double bpm = 96, beat = 60.0 / bpm;
            const int bars = 16, beatsPerBar = 3;
            double loop = bars * beatsPerBar * beat;
            var buf = Synth.Buffer(loop * 2);       // two passes: the second adds a high arpeggio

            int[] roots = { 55, 52, 48, 50, 55, 47, 48, 50, 55, 52, 48, 50, 55, 47, 48, 50 };  // G Em C D G Bm C D
            int[] thirds = { 4, 3, 4, 4, 4, 3, 4, 4, 4, 3, 4, 4, 4, 3, 4, 4 };
            N[] lead =
            {
                new N(71,0,2), new N(74,2,1),
                new N(76,3,1.5), new N(74,4.5,.5), new N(71,5,1),
                new N(72,6,1), new N(76,7,1), new N(79,8,1),
                new N(78,9,2), new N(74,11,1),
                new N(79,12,1), new N(71,13,1), new N(74,14,1),
                new N(78,15,1.5), new N(74,16.5,.5), new N(71,17,1),
                new N(76,18,1), new N(72,19,1), new N(69,20,1),
                new N(74,21,3),
                new N(74,24,1), new N(79,25,1), new N(83,26,1),
                new N(83,27,2), new N(79,29,1),
                new N(76,30,1), new N(79,31,1), new N(84,32,1),
                new N(81,33,2), new N(78,35,1),
                new N(79,36,1.5), new N(78,37.5,.5), new N(76,38,1),
                new N(74,39,1.5), new N(71,40.5,.5), new N(74,41,1),
                new N(76,42,1), new N(74,43,1), new N(72,44,1),
                new N(74,45,1.5), new N(71,46.5,.5), new N(69,47,1),
            };

            for (int pass = 0; pass < 2; pass++)
            {
                double off = pass * loop;
                for (int bar = 0; bar < bars; bar++)
                {
                    double t = off + bar * beatsPerBar * beat;
                    int root = roots[bar];
                    Synth.Pluck(buf, Synth.Midi(root), t, beat * 1.2, 0.45f);
                    Synth.Pluck(buf, Synth.Midi(root + 12 + thirds[bar]), t + beat, beat * 0.9, 0.22f);
                    Synth.Pluck(buf, Synth.Midi(root + 12 + 7), t + 2 * beat, beat * 0.9, 0.22f);
                    if (pass == 1)
                        for (int e = 0; e < 6; e++)
                        {
                            int tone = e % 3 == 0 ? root + 24 : e % 3 == 1 ? root + 24 + thirds[bar] : root + 24 + 7;
                            Synth.Note(buf, Synth.Wave.Sine, Synth.Midi(tone), t + e * beat / 2, beat * 0.4, 0.09f, 0.005, 0.1, 0.3, 0.05);
                        }
                }
                foreach (var n in lead)
                    Synth.Note(buf, Synth.Wave.Triangle, Synth.Midi(n.P), off + n.S * beat, n.L * beat * 0.92, 0.32f, 0.02, 0.08, 0.75, 0.12, 0.004);
            }
            Synth.Delay(buf, beat * 0.5, 0.3f, 0.35f);
            Synth.FadeEnds(buf, 0.01);
            Synth.Normalize(buf, 0.8f);
            return buf;
        }

        // ---- 2. Simmering Pot: D dorian, bouncing bass, square lead and bubbling blips -----------------

        static float[] SimmeringPot()
        {
            const double bpm = 110, beat = 60.0 / bpm;
            const int bars = 16;
            var buf = Synth.Buffer(bars * 4 * beat);
            var rng = new Random(7);

            int[] roots = { 38, 43, 38, 36, 38, 43, 38, 36, 38, 43, 38, 36, 38, 43, 36, 38 };  // Dm G Dm C ...
            N[] phrase =
            {
                new N(74,0,.5), new N(77,.5,.5), new N(81,1,1), new N(79,2.5,.5), new N(77,3,1),
                new N(79,4,1), new N(71,5.5,.5), new N(74,6,.5), new N(76,6.5,.5), new N(79,7,1),
                new N(81,8,.5), new N(77,8.5,.5), new N(74,9,1.5), new N(76,10.5,.5), new N(77,11,.5), new N(76,11.5,.5),
                new N(76,12,1), new N(72,13,.5), new N(74,13.5,.5), new N(76,14,2),
                new N(74,16,.5), new N(74,16.5,.5), new N(77,17,.5), new N(81,17.5,1.5), new N(79,19,1),
                new N(71,20,.5), new N(74,20.5,.5), new N(79,21,1), new N(77,22.5,.5), new N(76,23,1),
                new N(77,24,.5), new N(76,24.5,.5), new N(74,25,1), new N(72,26,.5), new N(74,26.5,.5), new N(76,27,1),
                new N(72,28,2), new N(74,31,1),
            };
            N[] ending = { new N(72,28,1), new N(74,29,.5), new N(76,29.5,.5), new N(74,30,2) };

            for (int bar = 0; bar < bars; bar++)
            {
                double t = bar * 4 * beat;
                int root = roots[bar];
                // Bouncing bass: root and octave on alternate eighths, a fifth before the bar turns.
                for (int e = 0; e < 8; e++)
                {
                    int p = e % 2 == 0 ? root : root + 12;
                    if (e == 6) p = root + 7;
                    Synth.Note(buf, Synth.Wave.Triangle, Synth.Midi(p), t + e * beat / 2, beat * 0.35, 0.5f, 0.005, 0.06, 0.5, 0.04);
                }
                // A ticking noise on every eighth, louder on the beat.
                for (int e = 0; e < 8; e++)
                {
                    var tick = Synth.Buffer(0.03);
                    Synth.Noise(tick, rng, e % 2 == 0 ? 0.18f : 0.08f);
                    Synth.LowPass(tick, 0.5f);
                    Mix(buf, tick, t + e * beat / 2, 1f, true);
                }
                // Bubbles: a rising blip after beats 2 and 4, pitched by a hash so it burbles.
                for (int e = 1; e < 4; e += 2)
                {
                    double f0 = 900 + (bar * 37 + e * 91) % 7 * 120;
                    Bubble(buf, f0, t + (e + 0.5) * beat, 0.08f);
                }
            }
            for (int rep = 0; rep < 2; rep++)
            {
                double off = rep * 32 * beat;
                foreach (var n in phrase)
                {
                    if (rep == 1 && n.S >= 28) continue;
                    Synth.Note(buf, Synth.Wave.Square, Synth.Midi(n.P), off + n.S * beat, n.L * beat * 0.85, 0.2f, 0.01, 0.05, 0.7, 0.05, 0.003);
                }
                if (rep == 1) foreach (var n in ending)
                    Synth.Note(buf, Synth.Wave.Square, Synth.Midi(n.P), off + n.S * beat, n.L * beat * 0.85, 0.2f, 0.01, 0.05, 0.7, 0.05, 0.003);
            }
            Synth.Delay(buf, beat * 0.75, 0.25f, 0.3f);
            Synth.FadeEnds(buf, 0.01);
            Synth.Normalize(buf, 0.8f);
            return buf;
        }

        static void Bubble(float[] buf, double f0, double start, float vol)
        {
            int i0 = (int)(start * Synth.Rate), n = (int)(0.12 * Synth.Rate);
            double phase = 0;
            for (int i = 0; i < n && i0 + i < buf.Length; i++)
            {
                double t = i / (double)Synth.Rate;
                phase += (f0 * (1 + t * 6)) / Synth.Rate;
                buf[i0 + i] += (float)Math.Sin(phase * Math.PI * 2) * vol * (float)Math.Exp(-t * 30);
            }
        }

        // ---- 3. Turmeric Sun: F lydian pads, a slow sine melody and a warm delay ---------------------

        static float[] TurmericSun()
        {
            const double bpm = 80, beat = 60.0 / bpm;
            const int bars = 16;
            var buf = Synth.Buffer(bars * 4 * beat);

            // Two bars per chord: Fmaj7 G Am7 C Fmaj7 Dm7 G C
            int[][] chords =
            {
                new[] { 53, 57, 60, 64 }, new[] { 55, 59, 62, 67 }, new[] { 57, 60, 64, 67 }, new[] { 48, 52, 55, 60 },
                new[] { 53, 57, 60, 64 }, new[] { 50, 53, 57, 60 }, new[] { 55, 59, 62, 66 }, new[] { 48, 52, 55, 59 },
            };
            N[] lead =
            {
                new N(81,0,3), new N(84,4,2), new N(83,6,2),
                new N(79,8,3), new N(74,11,1), new N(76,12,4),
                new N(84,16,2), new N(83,18,2), new N(81,20,4),
                new N(79,24,3), new N(76,27,1), new N(72,28,4),
                new N(81,32,2), new N(83,34,2), new N(84,36,4),
                new N(86,40,3), new N(84,43,1), new N(81,44,4),
                new N(83,48,3), new N(79,51,1), new N(74,52,4),
                new N(76,56,3), new N(79,59,1), new N(72,60,3.5),
            };

            var pad = Synth.Buffer(bars * 4 * beat);
            for (int c = 0; c < chords.Length; c++)
            {
                double t = c * 8 * beat;
                foreach (int p in chords[c])
                    Synth.Note(pad, Synth.Wave.Saw, Synth.Midi(p), t, 8 * beat - 0.3, 0.16f, 0.7, 0.4, 0.8, 0.9, 0.002, 0.006);
                Synth.Pluck(buf, Synth.Midi(chords[c][0] - 12), t, beat * 2, 0.4f);
                Synth.Pluck(buf, Synth.Midi(chords[c][0] - 12), t + 4 * beat, beat * 2, 0.3f);
                Synth.Pluck(buf, Synth.Midi(chords[c][2] - 12), t + 6 * beat, beat, 0.2f);
            }
            Synth.LowPass(pad, 0.12f);
            Mix(buf, pad, 0, 1f, false);
            foreach (var n in lead)
                Synth.Note(buf, Synth.Wave.Sine, Synth.Midi(n.P), n.S * beat, n.L * beat * 0.95, 0.3f, 0.08, 0.2, 0.8, 0.3, 0.006);
            Synth.Delay(buf, beat * 0.5, 0.42f, 0.5f);
            Synth.FadeEnds(buf, 0.02);
            Synth.Normalize(buf, 0.8f);
            return buf;
        }

        // ---- 4. Moth Lamp: an A minor lullaby, slow plucked bass, a soft pad and a sine melody with a wide vibrato ----

        static float[] RenderMothLamp()
        {
            const double bpm = 66, beat = 60.0 / bpm;
            const int bars = 8;
            var buf = Synth.Buffer(bars * 4 * beat);

            // One chord per bar: Am F C G Am F E Am
            int[][] chords =
            {
                new[] { 57, 60, 64 }, new[] { 53, 57, 60 }, new[] { 55, 60, 64 }, new[] { 55, 59, 62 },
                new[] { 57, 60, 64 }, new[] { 53, 57, 60 }, new[] { 52, 56, 59 }, new[] { 57, 60, 64 },
            };
            int[] roots = { 45, 41, 48, 43, 45, 41, 40, 45 };
            N[] lead =
            {
                new N(76,0,2), new N(72,2,1), new N(74,3,1),
                new N(72,4,2), new N(69,6,2),
                new N(67,8,1.5), new N(69,9.5,.5), new N(72,10,2),
                new N(74,12,2), new N(71,14,2),
                new N(76,16,2), new N(79,18,1), new N(77,19,1),
                new N(76,20,2), new N(72,22,2),
                new N(71,24,1.5), new N(68,25.5,.5), new N(71,26,2),
                new N(69,28,3.8),
            };

            var pad = Synth.Buffer(bars * 4 * beat);
            for (int bar = 0; bar < bars; bar++)
            {
                double t = bar * 4 * beat;
                foreach (int p in chords[bar])
                    Synth.Note(pad, Synth.Wave.Triangle, Synth.Midi(p), t, 4 * beat - 0.2, 0.14f, 0.6, 0.3, 0.8, 0.7, 0.003, 0.004);
                Synth.Pluck(buf, Synth.Midi(roots[bar]), t, beat * 2.5, 0.35f);
                Synth.Pluck(buf, Synth.Midi(roots[bar] + 7), t + 2 * beat, beat * 1.5, 0.18f);
                // A slow rocking arpeggio on the off-beats, like a music box winding down.
                for (int e = 1; e < 8; e += 2)
                {
                    int tone = chords[bar][(e / 2) % chords[bar].Length] + 12;
                    Synth.Note(buf, Synth.Wave.Sine, Synth.Midi(tone), t + e * beat / 2, beat * 0.4, 0.07f, 0.01, 0.15, 0.3, 0.2);
                }
            }
            Synth.LowPass(pad, 0.15f);
            Mix(buf, pad, 0, 1f, false);
            foreach (var n in lead)
                Synth.Note(buf, Synth.Wave.Sine, Synth.Midi(n.P), n.S * beat, n.L * beat * 0.95, 0.26f, 0.1, 0.2, 0.8, 0.35, 0.008);
            Synth.Delay(buf, beat * 0.75, 0.4f, 0.4f);
            Synth.FadeEnds(buf, 0.02);
            Synth.Normalize(buf, 0.55f);
            return buf;
        }

        // ---- 5. Festival: a D major jig, oom-pah bass, square lead, claps on the off-beats ------------

        static float[] RenderFestival()
        {
            const double bpm = 140, beat = 60.0 / bpm;
            const int bars = 16;
            var buf = Synth.Buffer(bars * 4 * beat);
            var rng = new Random(19);

            int[] roots = { 50, 45, 43, 50, 50, 45, 43, 50 };   // D A G D, twice
            N[] phrase =
            {
                new N(74,0,.5), new N(78,.5,.5), new N(81,1,.5), new N(78,1.5,.5), new N(74,2,.5), new N(78,2.5,.5), new N(81,3,1),
                new N(83,4,.5), new N(81,4.5,.5), new N(79,5,.5), new N(78,5.5,.5), new N(76,6,1), new N(74,7,1),
                new N(71,8,.5), new N(74,8.5,.5), new N(79,9,.5), new N(74,9.5,.5), new N(71,10,.5), new N(74,10.5,.5), new N(79,11,1),
                new N(78,12,.5), new N(76,12.5,.5), new N(74,13,1), new N(76,14,.5), new N(78,14.5,.5), new N(81,15,1),
                new N(81,16,.5), new N(81,16.5,.5), new N(86,17,1), new N(85,18,.5), new N(83,18.5,.5), new N(81,19,1),
                new N(81,20,.5), new N(83,20.5,.5), new N(85,21,1), new N(83,22,.5), new N(81,22.5,.5), new N(76,23,1),
                new N(79,24,.5), new N(78,24.5,.5), new N(76,25,.5), new N(74,25.5,.5), new N(71,26,1), new N(74,27,1),
                new N(78,28,.5), new N(81,28.5,.5), new N(86,29,1.5), new N(86,30.5,1.4),
            };

            for (int bar = 0; bar < bars; bar++)
            {
                double t = bar * 4 * beat;
                int root = roots[bar % roots.Length];
                // Oom-pah: root on the beat, the fifth and third above on the off-beat.
                for (int e = 0; e < 4; e++)
                {
                    Synth.Note(buf, Synth.Wave.Triangle, Synth.Midi(e % 2 == 0 ? root : root + 7), t + e * beat, beat * 0.4, 0.45f, 0.005, 0.06, 0.5, 0.05);
                    Synth.Note(buf, Synth.Wave.Square, Synth.Midi(root + 16), t + (e + 0.5) * beat, beat * 0.25, 0.1f, 0.005, 0.04, 0.4, 0.04);
                    Synth.Note(buf, Synth.Wave.Square, Synth.Midi(root + 19), t + (e + 0.5) * beat, beat * 0.25, 0.1f, 0.005, 0.04, 0.4, 0.04);
                }
                // A tambourine tick on every eighth and a hand-clap on 2 and 4.
                for (int e = 0; e < 8; e++)
                {
                    var tick = Synth.Buffer(0.03);
                    Synth.Noise(tick, rng, e % 2 == 0 ? 0.12f : 0.07f);
                    Synth.HighPass(tick, 0.5f);
                    Mix(buf, tick, t + e * beat / 2, 1f, true);
                }
                for (int e = 1; e < 4; e += 2)
                {
                    var clap = Synth.Buffer(0.12);
                    for (int k = 0; k < 3; k++) Synth.Burst(clap, rng, k * 0.012, 0.06, 0.6f, 0.4f, 60);
                    Mix(buf, clap, t + e * beat, 1f, true);
                }
            }
            for (int rep = 0; rep < 2; rep++)
            {
                double off = rep * 32 * beat;
                foreach (var n in phrase)
                {
                    Synth.Note(buf, Synth.Wave.Square, Synth.Midi(n.P), off + n.S * beat, n.L * beat * 0.85, 0.2f, 0.01, 0.04, 0.7, 0.05, 0.004);
                    // The second time round a bright triangle doubles the tune an octave up.
                    if (rep == 1) Synth.Note(buf, Synth.Wave.Triangle, Synth.Midi(n.P + 12), off + n.S * beat, n.L * beat * 0.8, 0.1f, 0.01, 0.04, 0.6, 0.05);
                }
            }
            Synth.Delay(buf, beat * 0.5, 0.25f, 0.3f);
            Synth.FadeEnds(buf, 0.01);
            Synth.Normalize(buf, 0.8f);
            return buf;
        }

        /// <summary>Adds src into dst at a time offset; with wrap, anything past the end folds to the start.</summary>
        static void Mix(float[] dst, float[] src, double start, float vol, bool wrap)
        {
            int i0 = (int)(start * Synth.Rate);
            for (int i = 0; i < src.Length; i++)
            {
                int j = i0 + i;
                if (j >= dst.Length) { if (!wrap) break; j %= dst.Length; }
                dst[j] += src[i] * vol;
            }
        }
    }
}
