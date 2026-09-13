using System;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework;
using SpiceWizard.Core;
using SpiceWizard.Web.Scene;
using SpiceWizard.Web.Ui;

namespace SpiceWizard.Web.Audio
{
    /// <summary>
    /// Three buses (music, ambience, effects) with the player's volumes applied on top. Music is one of the
    /// three daytime tracks, chosen by the day number, and only plays in daylight; from 20:00 and while the
    /// wizard is turning in, the lullaby takes over, and the party tune plays while the crowd is in the yard.
    /// The wind always blows, birds sing by day and crickets take over at night; rain hisses on the cauldron.
    /// Nothing starts until <see cref="Unlock"/>, which the title
    /// screen calls from a click so the browser lets the audio context run. The cauldron simmers louder
    /// the closer the wizard stands to it, and the wizard hums, wonders and cheers to himself now and then.
    /// </summary>
    public sealed class AudioMixer
    {
        public Settings Settings = new Settings();

        /// <summary>Where the wizard's feet are this frame, for the cauldron's proximity fade.</summary>
        public Vector2 WizardFeet;
        /// <summary>A panel is open: the wizard is deliberating, so his idle noises turn to "hmm".</summary>
        public bool Deliberating;
        /// <summary>The Door panel is open or the wizard is on his way to bed: the lullaby plays whatever the hour.</summary>
        public bool Bedtime;
        /// <summary>The crowd is in the yard: the party tune replaces the day's music.</summary>
        public bool Celebrating;

        SoundEffectInstance _music, _night, _party, _wind, _crickets, _cauldron, _rain, _hiss;
        float _rainLevel;           // rain fade 0..1
        float _windyLevel;          // windy-day fade 0..1
        int _track = -1;
        float _musicLevel;          // current fade 0..1 toward daylight
        float _nightLevel;          // lullaby fade 0..1
        float _partyLevel;          // celebration fade 0..1
        float _duck = 1f;           // lowered while paused
        float _cauldronLevel;       // proximity fade 0..1
        double _windTime;
        float _birdTimer = 3f;
        float _blorpTimer = 1f;
        float _voiceTimer = 6f;
        float _voiceCooldown;       // seconds until the wizard may make another noise
        int _lastVoice = -1;
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

        public void Play(string sfx) => Play(sfx, 1f, 0f);

        public void Play(string sfx, float volume, float pitch)
        {
            if (!_unlocked || Settings.Sfx <= 0) return;
            try { Sfx.Get(sfx).Play(Math.Clamp(Settings.Sfx * volume, 0f, 1f), pitch, 0f); }
            catch (Exception) { /* the browser may still be blocking audio; the game plays on */ }
        }

        /// <summary>One random member of a variant family, with a touch of pitch scatter.</summary>
        public void PlayVariant(string family, int variants, float volume = 1f, float scatter = 0.08f)
        {
            int n = _rng.Next(variants);
            Play(Sfx.Variant(family, n), volume, (float)(_rng.NextDouble() * 2 - 1) * scatter);
        }

        /// <summary>A footfall: called by the wizard on each stride of the walk cycle.</summary>
        public void Footstep() => PlayVariant(Sfx.Step, Sfx.StepVariants, 0.55f, 0.12f);

        /// <summary>The wizard reacts to something going right: usually a small "a-ha", sometimes nothing.</summary>
        public void Affirm()
        {
            if (_voiceCooldown > 0f || _rng.NextDouble() < 0.35) return;
            Speak(Sfx.Affirm, Sfx.AffirmVariants, 0.7f);
            _voiceTimer = Math.Max(_voiceTimer, 4f);
        }

        void Speak(string family, int variants, float volume)
        {
            int n = _rng.Next(variants);
            if (variants > 1 && n == _lastVoice) n = (n + 1) % variants;
            _lastVoice = n;
            Play(Sfx.Variant(family, n), volume, (float)(_rng.NextDouble() * 2 - 1) * 0.06f);
            _voiceCooldown = 2.5f;
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
                UpdateAmbience(dt, s, onTitle, dark);
                UpdateCauldron(dt, onTitle);
                UpdateVoice(dt, onTitle, paused);
            }
            catch (Exception) { /* audio device hiccups must never stop the game */ }
        }

        /// <summary>The pot simmers at a whisper from across the yard and rolls when the wizard stands beside it.</summary>
        void UpdateCauldron(float dt, bool onTitle)
        {
            const float near = 22f, far = 110f;
            float d = Vector2.Distance(WizardFeet, new Vector2(Layout.Cauldron.X + 12, Layout.Cauldron.Y + 20));
            float target = onTitle ? 0f : 0.08f + 0.92f * (1f - Math.Clamp((d - near) / (far - near), 0f, 1f));
            _cauldronLevel = Approach(_cauldronLevel, target, dt * 1.5f);

            if (_cauldron == null)
            {
                _cauldron = Ambience.Cauldron.CreateInstance();
                _cauldron.IsLooped = true;
                _cauldron.Play();
            }
            _cauldron.Volume = Math.Clamp(Settings.Sfx * _duck * _cauldronLevel * 0.6f, 0f, 1f);

            // The odd fat bubble breaks the surface, more often the closer you are.
            if (_cauldronLevel > 0.3f)
            {
                _blorpTimer -= dt;
                if (_blorpTimer <= 0f)
                {
                    _blorpTimer = 0.6f + (float)_rng.NextDouble() * 2.5f / _cauldronLevel;
                    PlayVariant(Sfx.Blorp, Sfx.BlorpVariants, _cauldronLevel * 0.5f, 0.2f);
                }
            }
        }

        /// <summary>Idle chatter: humming while pottering, "hmm" while a panel is open, never while paused.</summary>
        void UpdateVoice(float dt, bool onTitle, bool paused)
        {
            _voiceCooldown = Math.Max(0f, _voiceCooldown - dt);
            if (onTitle || paused) return;
            _voiceTimer -= dt;
            if (_voiceTimer > 0f) return;
            _voiceTimer = 7f + (float)_rng.NextDouble() * 12f;
            if (_voiceCooldown > 0f) return;
            if (Deliberating) Speak(Sfx.Think, Sfx.ThinkVariants, 0.6f);
            else if (_rng.NextDouble() < 0.8) Speak(Sfx.Mumble, Sfx.MumbleVariants, 0.5f);
            else Speak(Sfx.Think, Sfx.ThinkVariants, 0.5f);
        }

        /// <summary>The lullaby fades in over the half hour after 20:00 (and at once at the door); the party tune follows the crowd.</summary>
        static float NightWanted(GameState s, bool onTitle, bool bedtime, bool celebrating)
        {
            if (onTitle || celebrating) return 0f;
            if (bedtime) return 1f;
            return Math.Clamp((float)(s.Clock.Minute - 20 * 60) / 30f, 0f, 1f);
        }

        void UpdateMusic(float dt, GameState s, bool onTitle, float dark)
        {
            float night = NightWanted(s, onTitle, Bedtime, Celebrating);
            float party = !onTitle && Celebrating ? 1f : 0f;
            Loop(ref _night, ref _nightLevel, Music.MothLamp, night, dt / 2f, Settings.Music * _duck);
            Loop(ref _party, ref _partyLevel, Music.Festival, party, dt / 1.5f, Settings.Music * _duck);

            int want = onTitle ? -1 : (s.Clock.Day - 1) % Music.Tracks.Length;
            float target = onTitle || dark > 0.99f || night > 0f || party > 0f ? 0f : 1f - dark;

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

        /// <summary>Runs one looping track toward a target level, starting it when wanted and dropping it once silent.</summary>
        static void Loop(ref SoundEffectInstance inst, ref float level, Track track, float target, float step, float bus)
        {
            if (inst == null && target > 0f)
            {
                inst = track.Effect.CreateInstance();
                inst.IsLooped = true;
                inst.Volume = 0f;
                inst.Play();
                level = 0f;
            }
            if (inst == null) return;
            level = Approach(level, target, step);
            inst.Volume = Math.Clamp(level * bus, 0f, 1f);
            if (level <= 0f && target <= 0f) { inst.Stop(); inst.Dispose(); inst = null; }
        }

        void UpdateAmbience(float dt, GameState s, bool onTitle, float dark)
        {
            float amb = Settings.Ambience * _duck * (onTitle ? 0.5f : 1f);
            _rainLevel = Approach(_rainLevel, !onTitle && s.Weather == Weather.Rain ? 1f : 0f, dt / 2f);
            _windyLevel = Approach(_windyLevel, !onTitle && s.Weather == Weather.Windy ? 1f : 0f, dt / 2f);

            if (_rain == null)
            {
                _rain = Ambience.Rain.CreateInstance();
                _rain.IsLooped = true;
                _rain.Play();
            }
            _rain.Volume = Math.Clamp(amb * 0.7f * _rainLevel, 0f, 1f);

            // Rain spits on the hot cauldron, louder the nearer the wizard stands to it.
            if (_hiss == null && _rainLevel > 0f)
            {
                _hiss = Ambience.Hiss.CreateInstance();
                _hiss.IsLooped = true;
                _hiss.Play();
            }
            if (_hiss != null) _hiss.Volume = Math.Clamp(amb * 0.5f * _rainLevel * _cauldronLevel, 0f, 1f);

            if (_wind == null)
            {
                _wind = Ambience.Wind.CreateInstance();
                _wind.IsLooped = true;
                _wind.Play();
                _windTime = 0;
            }
            _wind.Volume = Math.Clamp(amb * (0.8f + 0.2f * _windyLevel + 0.1f * _rainLevel), 0f, 1f);

            if (_crickets == null)
            {
                _crickets = Ambience.Crickets.CreateInstance();
                _crickets.IsLooped = true;
                _crickets.Play();
            }
            _crickets.Volume = amb * 0.5f * dark;

            // Birds are one-shots at random intervals, quieter and rarer toward dusk; they shelter from the rain.
            if (dark < 0.7f && _rainLevel < 0.5f)
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
