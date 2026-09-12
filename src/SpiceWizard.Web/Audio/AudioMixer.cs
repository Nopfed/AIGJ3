using System;
using Microsoft.Xna.Framework.Audio;
using SpiceWizard.Core;
using SpiceWizard.Web.Scene;
using SpiceWizard.Web.Ui;

namespace SpiceWizard.Web.Audio
{
    /// <summary>
    /// Three buses (music, ambience, effects) with the player's volumes applied on top. Music is one of the
    /// three tracks, chosen by the day number, and only plays in daylight; the wind always blows, birds sing
    /// by day and crickets take over at night. Nothing starts until <see cref="Unlock"/>, which the title
    /// screen calls from a click so the browser lets the audio context run.
    /// </summary>
    public sealed class AudioMixer
    {
        public Settings Settings = new Settings();

        SoundEffectInstance _music, _wind, _crickets;
        int _track = -1;
        float _musicLevel;          // current fade 0..1 toward daylight
        float _duck = 1f;           // lowered while paused
        double _windTime;
        float _birdTimer = 3f;
        bool _unlocked;
        readonly Random _rng = new Random();

        /// <summary>Gust strength right now, shared with the scene so the trees lean with the sound.</summary>
        public float Wind => Ambience.WindStrength(_windTime);

        public bool Unlocked => _unlocked;

        public void Unlock()
        {
            if (_unlocked) return;
            _unlocked = true;
            Play(Sfx.Click);
        }

        public void Play(string sfx)
        {
            if (!_unlocked || Settings.Sfx <= 0) return;
            try { Sfx.Get(sfx).Play(Settings.Sfx, 0f, 0f); }
            catch (Exception) { /* the browser may still be blocking audio; the game plays on */ }
        }

        /// <summary>Call once per frame. Ambience follows the clock; paused ducks everything; onTitle silences the music.</summary>
        public void Update(float dt, GameState s, bool paused, bool onTitle)
        {
            _windTime += dt;
            if (!_unlocked) return;

            double f = s.Clock.DayFraction;
            float dark = DayNight.Darkness(f);
            _duck = Approach(_duck, paused ? 0.35f : 1f, dt * 3);

            try
            {
                UpdateMusic(dt, s, onTitle, dark);
                UpdateAmbience(dt, onTitle, dark);
            }
            catch (Exception) { /* audio device hiccups must never stop the game */ }
        }

        void UpdateMusic(float dt, GameState s, bool onTitle, float dark)
        {
            int want = onTitle ? -1 : (s.Clock.Day - 1) % Music.Tracks.Length;
            float target = onTitle || dark > 0.99f ? 0f : 1f - dark;

            // Fade out before swapping tracks so the day change is a cross-fade through silence.
            if (_music != null && (_track != want || target <= 0f))
            {
                _musicLevel = Approach(_musicLevel, 0f, dt / 1.5f);
                _music.Volume = _musicLevel * Settings.Music * _duck;
                if (_musicLevel <= 0f) { _music.Stop(); _music.Dispose(); _music = null; _track = -1; }
                return;
            }
            if (_music == null && want >= 0 && target > 0f)
            {
                _music = Music.Tracks[want].Effect.CreateInstance();
                _music.IsLooped = true;
                _music.Volume = 0f;
                _music.Play();
                _track = want;
                _musicLevel = 0f;
            }
            if (_music != null)
            {
                _musicLevel = Approach(_musicLevel, target, dt / 2f);
                _music.Volume = _musicLevel * Settings.Music * _duck;
            }
        }

        void UpdateAmbience(float dt, bool onTitle, float dark)
        {
            float amb = Settings.Ambience * _duck * (onTitle ? 0.5f : 1f);

            if (_wind == null)
            {
                _wind = Ambience.Wind.CreateInstance();
                _wind.IsLooped = true;
                _wind.Play();
                _windTime = 0;
            }
            _wind.Volume = amb * 0.8f;

            if (_crickets == null)
            {
                _crickets = Ambience.Crickets.CreateInstance();
                _crickets.IsLooped = true;
                _crickets.Play();
            }
            _crickets.Volume = amb * 0.5f * dark;

            // Birds are one-shots at random intervals, quieter and rarer toward dusk.
            if (dark < 0.7f)
            {
                _birdTimer -= dt;
                if (_birdTimer <= 0f)
                {
                    _birdTimer = 2f + (float)_rng.NextDouble() * 5f + dark * 6f;
                    if (amb > 0f)
                        Ambience.Bird(_rng.Next(4)).Play(amb * (0.45f + 0.35f * (float)_rng.NextDouble()) * (1f - dark), 0f, (float)(_rng.NextDouble() * 1.2 - 0.6));
                }
            }
        }

        static float Approach(float v, float target, float step) =>
            v < target ? Math.Min(target, v + step) : Math.Max(target, v - step);
    }
}
