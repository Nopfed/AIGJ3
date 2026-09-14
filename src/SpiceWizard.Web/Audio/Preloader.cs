using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SpiceWizard.Web.Audio
{
    /// <summary>
    /// Renders every sound the game can make ahead of its first play, so no frame ever synthesises audio on
    /// demand: a minute of music takes seconds in the browser's .NET interpreter and even a short effect costs
    /// a dropped frame. Effects and ambience loops are small enough to do whole, in <see cref="RenderEffects"/>
    /// while the loading screen is up; the tunes then go through <see cref="Track.Step"/> a slice at a time
    /// under a per-frame budget, in the order the day needs them, and the mixer moves a track to the front
    /// when it wants it sooner.
    /// </summary>
    public sealed class Preloader
    {
        sealed class Job
        {
            public Track Track;         // null for an effect or ambience loop
            public Func<bool> Run;      // true when finished
        }

        readonly List<Job> _jobs = new List<Job>();
        readonly Stopwatch _clock = new Stopwatch();

        public Preloader()
        {
            foreach (var render in Ambience.All())
            {
                var r = render;
                _jobs.Add(new Job { Run = () => { r(); return true; } });
            }
            foreach (var name in Sfx.AllNames())
            {
                var n = name;
                _jobs.Add(new Job { Run = () => { Sfx.Get(n); return true; } });
            }
            foreach (var track in new[] { Music.Tracks[0], Music.MothLamp, Music.Tracks[1], Music.Tracks[2], Music.Festival })
            {
                var t = track;
                _jobs.Add(new Job { Track = t, Run = t.Step });
            }
        }

        public bool Done => _jobs.Count == 0;

        /// <summary>Renders every effect and ambience loop right now, leaving only the tunes for the frames to come.</summary>
        public void RenderEffects()
        {
            while (_jobs.Count > 0 && _jobs[0].Track == null) Step(0);
        }

        /// <summary>The mixer wants this track next: render it before anything else still queued.</summary>
        public void Prioritize(Track track)
        {
            int at = _jobs.FindIndex(j => j.Track == track);
            if (at <= 0) return;
            var job = _jobs[at];
            _jobs.RemoveAt(at);
            _jobs.Insert(0, job);
        }

        /// <summary>Does as much rendering as fits in the budget (always at least one step).</summary>
        public void Step(double budgetMs)
        {
            if (_jobs.Count == 0) return;
            _clock.Restart();
            do
            {
                var job = _jobs[0];
                bool finished;
                try { finished = job.Run(); }
                catch (Exception) { finished = true; /* a sound that will not render is skipped; the game plays on */ }
                if (finished) _jobs.Remove(job);
            }
            while (_jobs.Count > 0 && _clock.Elapsed.TotalMilliseconds < budgetMs);
        }
    }
}
