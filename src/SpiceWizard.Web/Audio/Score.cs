using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Audio;

namespace SpiceWizard.Web.Audio
{
    /// <summary>
    /// A tune as a list of voices (notes, plucks, pre-rendered clips) that is rendered a short slice of
    /// samples at a time rather than voice by voice. A whole minute of music takes seconds to synthesise in
    /// the browser's .NET interpreter, so the music has to be built across many frames: <see cref="Step"/>
    /// does a twentieth of a second of audio per call and <see cref="Preloader"/> spreads the calls out
    /// under a per-frame budget. The arithmetic per sample is the same as <see cref="Synth"/>'s, so a
    /// track rendered this way is bit-identical to the one-shot render.
    /// </summary>
    public sealed class Score
    {
        const double TwoPi = Math.PI * 2;
        /// <summary>Samples rendered per step: a twentieth of a second, a few milliseconds of work in the browser.</summary>
        const int Slice = Synth.Rate / 20;

        abstract class Voice
        {
            public int Start, End;   // sample range this voice touches (End exclusive, clamped to the buffer)
            public abstract void Render(float[] buf, int i0, int i1);
        }

        sealed class NoteVoice : Voice
        {
            public Synth.Wave Wave; public double Freq, Len, A, D, S, R, Vibrato, Detune; public float Vol;
            double _phase, _phase2;
            public override void Render(float[] buf, int i0, int i1)
            {
                for (int i = i0; i < i1; i++)
                {
                    double t = (i - Start) / (double)Synth.Rate;
                    double f = Freq * (1 + Vibrato * Math.Sin(t * TwoPi * 5.5));
                    _phase += f / Synth.Rate;
                    float v = Synth.Osc(Wave, _phase);
                    if (Detune > 0)
                    {
                        _phase2 += f * (1 + Detune) / Synth.Rate;
                        v = (v + Synth.Osc(Wave, _phase2)) * 0.5f;
                    }
                    buf[i] += v * Vol * Synth.Env(t, Len, A, D, S, R);
                }
            }
        }

        sealed class PluckVoice : Voice
        {
            public double Freq; public float Vol;
            public override void Render(float[] buf, int i0, int i1)
            {
                for (int i = i0; i < i1; i++)
                {
                    double t = (i - Start) / (double)Synth.Rate;
                    float env = (float)Math.Exp(-t * 6) * (t < 0.005 ? (float)(t / 0.005) : 1f);
                    float v = (float)(Math.Sin(t * TwoPi * Freq) + 0.35 * Math.Sin(t * TwoPi * Freq * 2) * Math.Exp(-t * 10));
                    buf[i] += v * Vol * env;
                }
            }
        }

        /// <summary>A rising sine blip, pitched like a bubble breaking the surface.</summary>
        sealed class BubbleVoice : Voice
        {
            public double F0; public float Vol;
            double _phase;
            public override void Render(float[] buf, int i0, int i1)
            {
                for (int i = i0; i < i1; i++)
                {
                    double t = (i - Start) / (double)Synth.Rate;
                    _phase += (F0 * (1 + t * 6)) / Synth.Rate;
                    buf[i] += (float)Math.Sin(_phase * Math.PI * 2) * Vol * (float)Math.Exp(-t * 30);
                }
            }
        }

        /// <summary>A small buffer (a tick, a clap) added at an offset; made on demand, or shared with the voice that wraps it.</summary>
        sealed class ClipVoice : Voice
        {
            public Func<float[]> Make; public ClipVoice Source; public int Offset;   // buf[i] += Clip[i - Offset]
            public float[] Clip;
            public void Bake() { Clip = Source != null ? Source.Clip : Make(); }
            public override void Render(float[] buf, int i0, int i1)
            {
                for (int i = i0; i < i1; i++) buf[i] += Clip[i - Offset] * 1f;
            }
        }

        /// <summary>Another score rendered into its own buffer, low-passed and mixed in: the pads under a tune.</summary>
        sealed class LayerVoice : Voice
        {
            public Score Layer; public float Cutoff; public float Vol;
            float _y;
            public override void Render(float[] buf, int i0, int i1)
            {
                var pad = Layer._buf;
                Layer.RenderVoices(i0, i1);
                for (int i = i0; i < i1; i++)
                {
                    _y += Cutoff * (pad[i] - _y);
                    pad[i] = _y;
                    buf[i] += pad[i] * Vol;
                }
            }
        }

        readonly List<Voice> _voices = new List<Voice>();
        readonly float[] _buf;
        int _delay; float _feedback, _mix; double _fade; float _peak = 0.9f;
        int _at;                    // next sample to render
        int _phase;                 // 0 bake clips, 1 voices+delay, 2 fade, 3 peak scan, 4 gain, 5 done
        int _bake;                  // next voice to look at for a clip to make
        float _max; int _scan;
        public int Length => _buf.Length;

        public Score(double seconds) { _buf = Synth.Buffer(seconds); }

        void Add(Voice v, int start, int end)
        {
            v.Start = start; v.End = Math.Min(_buf.Length, end);
            if (v.End > Math.Max(0, v.Start)) _voices.Add(v);
        }

        /// <summary>Same voice as <see cref="Synth.Note"/>.</summary>
        public void Note(Synth.Wave wave, double freq, double start, double len, float vol,
            double a = 0.01, double d = 0.05, double s = 0.7, double r = 0.08, double vibrato = 0, double detune = 0)
        {
            int i0 = (int)(start * Synth.Rate);
            Add(new NoteVoice { Wave = wave, Freq = freq, Len = len, Vol = vol, A = a, D = d, S = s, R = r, Vibrato = vibrato, Detune = detune },
                i0, (int)((start + len + r) * Synth.Rate));
        }

        /// <summary>Same voice as <see cref="Synth.Pluck"/>.</summary>
        public void Pluck(double freq, double start, double len, float vol)
        {
            int i0 = (int)(start * Synth.Rate);
            Add(new PluckVoice { Freq = freq, Vol = vol }, i0, (int)((start + len) * Synth.Rate));
        }

        public void Bubble(double f0, double start, float vol)
        {
            int i0 = (int)(start * Synth.Rate);
            Add(new BubbleVoice { F0 = f0, Vol = vol }, i0, i0 + (int)(0.12 * Synth.Rate));
        }

        /// <summary>
        /// Adds a clip of the given length at a time offset; with wrap, anything past the end folds to the start.
        /// The clip itself is made later, in the first rendering steps, so composing a tune stays instant.
        /// </summary>
        public void Clip(double seconds, Func<float[]> make, double start, bool wrap)
        {
            int i0 = (int)(start * Synth.Rate), length = (int)(seconds * Synth.Rate);
            var clip = new ClipVoice { Make = make, Offset = i0 };
            Add(clip, i0, i0 + length);
            int over = i0 + length - _buf.Length;
            if (wrap && over > 0)
                Add(new ClipVoice { Source = clip, Offset = i0 - _buf.Length }, 0, over);
        }

        /// <summary>Mixes in another score of the same length, low-passed, at this point in the voice order.</summary>
        public void Layer(Score layer, float cutoff, float vol = 1f)
        {
            foreach (var v in layer._voices) if (v is ClipVoice) throw new ArgumentException("clips inside a layer are never baked");
            Add(new LayerVoice { Layer = layer, Cutoff = cutoff, Vol = vol }, 0, _buf.Length);
        }

        /// <summary>The tail every tune gets: a feedback delay, faded ends and normalisation to a peak.</summary>
        public void Finish(double delaySeconds, float feedback, float mix, double fadeSeconds, float peak)
        {
            _delay = (int)(delaySeconds * Synth.Rate);
            _feedback = feedback; _mix = mix; _fade = fadeSeconds; _peak = peak;
        }

        void RenderVoices(int i0, int i1)
        {
            // Voices are visited in the order they were written, so each sample sums in the same order as a one-shot render.
            foreach (var v in _voices)
                if (v.Start < i1 && v.End > i0)
                    v.Render(_buf, Math.Max(v.Start, i0), Math.Min(v.End, i1));
        }

        /// <summary>Renders the whole tune at once.</summary>
        public float[] Render()
        {
            while (!Step()) { }
            return _buf;
        }

        /// <summary>One slice of work; true once the buffer is complete, after which <see cref="Buffer"/> is ready.</summary>
        public bool Step()
        {
            switch (_phase)
            {
                case 0:
                {
                    // Clips are made in the order they were written so a shared random source is consumed as it would be in one go.
                    for (int made = 0; _bake < _voices.Count && made < 8; _bake++)
                        if (_voices[_bake] is ClipVoice clip) { clip.Bake(); made++; }
                    if (_bake >= _voices.Count) _phase = 1;
                    return false;
                }
                case 1:
                {
                    int i0 = _at, i1 = Math.Min(_buf.Length, _at + Slice);
                    RenderVoices(i0, i1);
                    if (_delay > 0 && _delay < _buf.Length)
                        for (int i = Math.Max(_delay, i0); i < i1; i++) _buf[i] += _buf[i - _delay] * _feedback * _mix;
                    _at = i1;
                    if (_at >= _buf.Length) _phase = 2;
                    return false;
                }
                case 2:
                    Synth.FadeEnds(_buf, _fade);
                    _phase = 3; _scan = 0; _max = 0;
                    return false;
                case 3:
                {
                    int end = Math.Min(_buf.Length, _scan + Slice * 20);
                    for (int i = _scan; i < end; i++) _max = Math.Max(_max, Math.Abs(_buf[i]));
                    _scan = end;
                    if (_scan >= _buf.Length) { _phase = 4; _scan = 0; }
                    return false;
                }
                case 4:
                {
                    if (_max <= 0) { _phase = 5; return true; }
                    float g = _peak / _max;
                    int end = Math.Min(_buf.Length, _scan + Slice * 20);
                    for (int i = _scan; i < end; i++) _buf[i] *= g;
                    _scan = end;
                    if (_scan >= _buf.Length) _phase = 5;
                    return _phase == 5;
                }
                default:
                    return true;
            }
        }

        /// <summary>The finished samples; only valid once <see cref="Step"/> has returned true.</summary>
        public float[] Buffer => _buf;
    }

    /// <summary>
    /// The master chain and 16-bit conversion of <see cref="Synth.ToSoundEffect"/>, run a slice at a time so a
    /// long track's mastering does not stall a frame either.
    /// </summary>
    public sealed class Encoder
    {
        const float Knee = 0.55f, Gain = 0.55f;
        readonly float[] _buf;
        readonly byte[] _pcm;
        readonly float _hp = Synth.Coefficient(40), _lp = Synth.Coefficient(4200);
        float _dc, _y1, _y2;
        int _at;

        public Encoder(float[] buf) { _buf = buf; _pcm = new byte[buf.Length * 2]; }

        /// <summary>Masters and packs the next stretch of samples; true once the whole buffer is done.</summary>
        public bool Step(int samples)
        {
            int end = Math.Min(_buf.Length, _at + samples);
            for (int i = _at; i < end; i++)
            {
                _dc += _hp * (_buf[i] - _dc);
                float x = _buf[i] - _dc;
                float a = Math.Abs(x);
                if (a > Knee) x = Math.Sign(x) * (Knee + (1 - Knee) * (float)Math.Tanh((a - Knee) / (1 - Knee)));
                _y1 += _lp * (x - _y1);
                _y2 += _lp * (_y1 - _y2);
                float v = _y2 * Gain;
                _buf[i] = v;
                if (v > 1) v = 1; else if (v < -1) v = -1;
                short s = (short)(v * 32767);
                _pcm[i * 2] = (byte)(s & 0xff);
                _pcm[i * 2 + 1] = (byte)((s >> 8) & 0xff);
            }
            _at = end;
            return _at >= _buf.Length;
        }

        public SoundEffect Finish() => new SoundEffect(_pcm, Synth.Rate, AudioChannels.Mono);
    }
}
