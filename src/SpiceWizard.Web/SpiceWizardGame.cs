using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SpiceWizard.Core;
using SpiceWizard.Web.Art;
using SpiceWizard.Web.Audio;
using SpiceWizard.Web.Scene;
using SpiceWizard.Web.Ui;

namespace SpiceWizard.Web
{
    /// <summary>
    /// The one and only screen. Update steps time and captures input; Draw renders the yard and then
    /// runs the immediate-mode UI, which is also where clicks are resolved.
    /// </summary>
    public class SpiceWizardGame : Game
    {
        readonly GraphicsDeviceManager _graphics;
        readonly string _savedJson;
        readonly Action<string> _saveHook;
        readonly Action _clearSaveHook;
        readonly string _savedSettings;
        readonly Action<string> _saveSettingsHook;
        readonly Func<int[]> _pollClicks;
        readonly string _demo;

        SpriteBatch _batch;
        Atlas _atlas;
        Canvas _canvas;
        Camera _camera;
        Ui.Ui _ui;
        Session _session;
        SceneRenderer _scene;
        WizardActor _wizard;
        Particles _particles;
        Crowd _crowd;
        Cats _cats;
        AudioMixer _mixer;
        GameState _state;

        MouseState _prevMouse;
        KeyboardState _prevKeys;
        Point _mouse;
        int _wheel;
        bool _clicked;
        bool _down;
        bool _clickLatch;
        bool _escape;
        float _dt;
        float _time;
        float _smokeTimer;
        float _leafTimer;

        // Bedtime, in order: 0 awake, 3 walking to the door, 4 door opening, 5 stepping inside,
        // 6 door closing, 7 snoring at the window, 1 fading to black, 2 fading back in (and coming out).
        int _sleepPhase;
        float _fade;
        float _sleepTimer;
        bool _emerged;
        float _cartTimer;
        float _confettiTimer;
        Station _hover;

        public SpiceWizardGame(string savedJson, Action<string> saveHook, Action clearSaveHook, Func<int[]> pollClicks, string demo = null,
            string savedSettings = null, Action<string> saveSettingsHook = null)
        {
            _demo = demo;
            _savedSettings = savedSettings;
            _saveSettingsHook = saveSettingsHook;
            _graphics = new GraphicsDeviceManager(this);
            _savedJson = savedJson;
            _saveHook = saveHook;
            _clearSaveHook = clearSaveHook;
            _pollClicks = pollClicks;
            IsMouseVisible = true;
        }

        protected override void LoadContent()
        {
            _batch = new SpriteBatch(GraphicsDevice);
            _atlas = new Atlas(GraphicsDevice);
            _canvas = new Canvas(_batch, _atlas);
            _camera = new Camera();
            _ui = new Ui.Ui(_canvas);
            _scene = new SceneRenderer(_canvas);
            _particles = new Particles();
            _crowd = new Crowd();
            _cats = new Cats();
            _wizard = new WizardActor(Layout.WizardStart);
            _mixer = new AudioMixer { Settings = Settings.Parse(_savedSettings) };

            _session = new Session
            {
                Particles = _particles,
                Settings = _mixer.Settings,
                HasSave = SaveSystem.FromJson(_savedJson) != null,
                OnWatered = p => { _particles.Water(p); _mixer.Play(Sfx.Splash); },
                OnSparkle = (p, color) => { _particles.Sparkle(p, color); _mixer.Play(Sfx.Sparkle); },
                RequestSleep = StartSleep,
                RequestNewGame = NewGame,
                RequestContinue = Continue,
                RequestQuit = QuitToTitle,
                SettingsChanged = SaveSettings,
                PlaySfx = _mixer.Play,
                OnAffirm = _mixer.Affirm,
            };
            _wizard.OnStep = _mixer.Footstep;
            _ui.OnClick = () => _mixer.Play(Sfx.Click);
            _state = GameState.NewGame((ulong)DateTime.UtcNow.Ticks);
            _session.Panel = PanelKind.Title;
            if (_demo != null)
            {
                _state = DemoState.Build(_demo == "master");
                if (_demo == "night") _state.Clock.Minute = 21 * 60 + 20;
                if (_demo == "windy") _state.Weather = Weather.Windy;
                if (_demo == "rain") { _state.Weather = Weather.Rain; WeatherInfo.ApplyRain(_state); }
                _session.Close();
                _mixer.Unlock();
            }
        }

        void NewGame()
        {
            _mixer.Unlock();
            _state = GameState.NewGame((ulong)DateTime.UtcNow.Ticks);
            _clearSaveHook?.Invoke();
            _session.HasSave = false;
            _crowd.Stop();
            ResetSleep();
            _wizard = new WizardActor(Layout.WizardStart) { OnStep = _mixer.Footstep };
            _session.Open(PanelKind.Help);
            _session.Say("Welcome to your tower. Click the garden to begin!");
        }

        void Continue()
        {
            _mixer.Unlock();
            var loaded = SaveSystem.FromJson(_savedJson);
            if (loaded == null) { NewGame(); return; }
            _state = loaded;
            ResetSleep();
            _session.Close();
            _session.Say("Welcome back, " + Progression.Title(_state.Level) + ".");
        }

        void ResetSleep()
        {
            _sleepPhase = 0;
            _fade = 0;
            _scene.DoorOpen = _scene.WindowsLit = _scene.Snoring = false;
            _wizard.Hidden = _wizard.FacingAway = _wizard.InDoorway = false;
        }

        void QuitToTitle()
        {
            Save();
            _crowd.Stop();
            _session.Open(PanelKind.Title);
        }

        void SaveSettings()
        {
            try { _saveSettingsHook?.Invoke(_mixer.Settings.Serialize()); }
            catch (Exception) { /* preferences are a nicety */ }
        }

        void Save()
        {
            try { _saveHook?.Invoke(SaveSystem.ToJson(_state)); _session.HasSave = true; }
            catch (Exception) { /* storage may be unavailable; the game still plays */ }
        }

        void StartSleep()
        {
            if (_sleepPhase != 0) return;
            _sleepPhase = 3;
            _fade = 0;
            _session.Close();
            _wizard.WalkTo(Layout.DoorStand, () => { _sleepPhase = 4; _sleepTimer = 0.35f; });
        }

        protected override void Update(GameTime gameTime)
        {
            _dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_dt > 0.1f) _dt = 0.1f;
            _time += _dt;

            var vp = GraphicsDevice.Viewport;
            _camera.Fit(vp.Width, vp.Height);

            var mouse = Mouse.GetState();
            _mouse = _camera.ToVirtual(mouse.X, mouse.Y);
            _clicked = mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released;
            _down = mouse.LeftButton == ButtonState.Pressed;
            _wheel = mouse.ScrollWheelValue - _prevMouse.ScrollWheelValue;
            _prevMouse = mouse;
            // Clicks queued by the page (pointerdown events) catch taps shorter than one frame.
            var queued = _pollClicks?.Invoke();
            bool queuedEscape = queued != null && queued.Length >= 1 && queued[0] > 0;
            if (queued != null && queued.Length >= 3)
            {
                _mouse = _camera.ToVirtual(queued[queued.Length - 2], queued[queued.Length - 1]);
                _clicked = true;
                _clickLatch = true;
            }
            else if (_clickLatch)
            {
                // The polled state may report the same press one frame later; do not double-click.
                _clickLatch = false;
                if (mouse.LeftButton == ButtonState.Pressed) _clicked = false;
            }

            var keys = Keyboard.GetState();
            _escape = queuedEscape || (keys.IsKeyDown(Keys.Escape) && !_prevKeys.IsKeyDown(Keys.Escape));
            _prevKeys = keys;

            _session.Update(_dt);
            _wizard.Update(_dt);
            _particles.Update(_dt);
            _scene.Update(_dt);
            _crowd.Update(_dt);
            _cats.Update(_dt, _scene, _particles);
            bool paused = _session.Panel == PanelKind.Pause || _session.Panel == PanelKind.Options;
            _mixer.WizardFeet = _wizard.Feet;
            _mixer.Deliberating = _session.PanelOpen && !paused && _session.Panel != PanelKind.Celebration;
            _mixer.Update(_dt, _state, paused, _session.Panel == PanelKind.Title);
            _scene.Wind = _mixer.Wind;
            _scene.Weather = _state.Weather;
            _particles.Wind = _scene.WindPush;

            if (_session.Panel == PanelKind.Title) { base.Update(gameTime); return; }

            if (_escape)
            {
                if (_session.Panel == PanelKind.Options) _session.Open(PanelKind.Pause);
                else if (_session.PanelOpen && _session.Panel != PanelKind.Celebration) _session.Close();
                else if (!_session.PanelOpen && _sleepPhase == 0) _session.Open(PanelKind.Pause);
            }

            UpdateSleep();

            if (_sleepPhase == 0 && !_session.PausesClock)
            {
                _state.Clock.Advance(_dt);
                if (_state.Clock.IsNightfall)
                {
                    _session.Say("You can barely keep your eyes open...");
                    StartSleep();
                }
            }

            // The merchant's cart trundles in at dawn.
            if (_cartTimer > 0)
            {
                _cartTimer -= _dt;
                float t = 1f - Math.Max(0, _cartTimer) / 2f;
                _scene.CartX = MathHelper.Lerp(-50, Layout.CartPark.X, t);
            }
            else _scene.CartX = Layout.CartPark.X;

            if (_crowd.Active)
            {
                _confettiTimer -= _dt;
                if (_confettiTimer <= 0) { _confettiTimer = 0.05f; _particles.Confetti(Camera.Left, Camera.Right, Camera.Top); }
            }

            // Cauldron steam while a fire is going, and the odd puff of wood smoke that rises past the pot.
            if ((int)(_time * 10) % 4 == 0) _particles.Steam(new Point(Layout.Cauldron.X + 12, Layout.Cauldron.Y + 2));
            _smokeTimer -= _dt;
            if (_smokeTimer <= 0) { _smokeTimer = 0.55f; _particles.Smoke(new Point(Layout.Cauldron.X + 12, Layout.Cauldron.Y - 6)); }

            // On windy days leaves blow in from the left and tumble across the whole yard.
            if (_scene.Blustery)
            {
                _leafTimer -= _dt;
                if (_leafTimer <= 0) { _leafTimer = 0.3f; _particles.Leaf(Camera.Left, Layout.Horizon - 30, Camera.Bottom); }
            }

            _hover = null;
            if (!_session.PanelOpen && _sleepPhase == 0 && _mouse.Y > Camera.Top + Layout.HudHeight)
                foreach (var st in Layout.Stations)
                {
                    if (st.Kind == StationKind.Plot && st.Index >= _state.UnlockedPlots) continue;
                    if (st.Bounds.Contains(_mouse)) { _hover = st; break; }
                }

            base.Update(gameTime);
        }

        void UpdateSleep()
        {
            _sleepTimer -= _dt;
            switch (_sleepPhase)
            {
                case 4: // He pauses on the step, then the door swings open and he turns to go in.
                    if (_sleepTimer > 0) break;
                    _scene.DoorOpen = true;
                    _mixer.Play(Sfx.Click);
                    _wizard.FacingAway = true;
                    _wizard.InDoorway = true;
                    _sleepPhase = 5;
                    _wizard.WalkTo(Layout.DoorInside, () => { _sleepPhase = 6; _sleepTimer = 0.45f; });
                    break;
                case 6: // A beat in the doorway, then the door shuts behind him.
                    if (_sleepTimer > 0) break;
                    _wizard.Hidden = true;
                    _scene.DoorOpen = false;
                    _scene.WindowsLit = true;
                    _mixer.Play(Sfx.Thud);
                    _sleepPhase = 7;
                    _sleepTimer = 0.5f;
                    break;
                case 7: // The bedroom light is on and the snoring starts; then the night rolls in.
                    if (_sleepTimer > 0) break;
                    if (!_scene.Snoring) { _scene.Snoring = true; _sleepTimer = 1.8f; break; }
                    _sleepPhase = 1;
                    break;
                case 1:
                    _fade += _dt / 0.7f;
                    if (_fade >= 1f)
                    {
                        _fade = 1f;
                        var report = DayTick.Sleep(_state);
                        Save();
                        _mixer.Play(report.LevelsGained > 0 ? Sfx.LevelUp : Sfx.Chime);
                        _sleepPhase = 2;
                        _emerged = false;
                        _cartTimer = 2f;
                        _scene.Snoring = false;
                        _scene.WindowsLit = false;
                        _scene.DoorOpen = true;
                        _wizard = new WizardActor(Layout.DoorInside) { OnStep = _mixer.Footstep, InDoorway = true };
                        if (report.BecameMaster) { _crowd.Start(); }
                    }
                    break;
                case 2: // Morning: the wizard steps out as the dark lifts, and the door closes behind him.
                    _fade -= _dt / 0.7f;
                    if (_fade < 0.6f && !_emerged)
                    {
                        _emerged = true;
                        _wizard.WalkTo(Layout.DoorStand, () => { _wizard.InDoorway = false; _scene.DoorOpen = false; _mixer.Play(Sfx.Thud); });
                    }
                    if (_fade <= 0f)
                    {
                        _fade = 0f;
                        _sleepPhase = 0;
                        _session.Open(_state.LastReport != null && _state.LastReport.BecameMaster ? PanelKind.Celebration : PanelKind.Morning);
                    }
                    break;
            }
        }

        void HandleSceneClick()
        {
            if (!_ui.Clicked || _session.PanelOpen || _sleepPhase != 0) return;
            if (_hover != null)
            {
                _ui.ConsumeClick();
                var st = _hover;
                _wizard.WalkTo(st.Stand, () => UseStation(st));
            }
            else if (_mouse.Y > Layout.Horizon)
            {
                // Anywhere else in the yard: just stroll over there.
                _ui.ConsumeClick();
                _wizard.WalkTo(Layout.ClampWalk(_mouse), null);
            }
        }

        void UseStation(Station st)
        {
            switch (st.Kind)
            {
                case StationKind.Plot: _session.Open(PanelKind.Plot, st.Index); break;
                case StationKind.Well:
                    var r = Actions.RefillBucket(_state);
                    _session.Say(r, Sfx.Bucket);
                    if (r.Ok) _particles.Water(new Point(Layout.Bucket.X + 4, Layout.Bucket.Y + 2));
                    break;
                case StationKind.Board: _session.Open(PanelKind.Board); break;
                case StationKind.Market: _session.Open(PanelKind.Market); break;
                case StationKind.Cauldron: _session.Open(PanelKind.Cauldron); break;
                case StationKind.Shelf: _session.Open(PanelKind.Shelf); break;
                case StationKind.Mortar: _session.Open(PanelKind.Mortar); break;
                case StationKind.Pantry: _session.Open(PanelKind.Pantry); break;
                case StationKind.Crate: _session.Open(PanelKind.Crate); break;
                case StationKind.Door: _session.Open(PanelKind.Door); break;
            }
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Palette.Outline);
            _batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, Camera.Transform);

            _ui.Begin(_mouse, _clicked, _wheel, _down);
            _scene.Draw(_state, _wizard, _particles, _crowd, _cats, _hover);

            if (_session.Panel == PanelKind.Title)
            {
                Overlays.Title(_ui, _session, _time);
            }
            else
            {
                Overlays.Hud(_ui, _state, _session);
                if (_session.Panel == PanelKind.Celebration) Overlays.Celebration(_ui, _state, _session, _time);
                else if (_session.Panel == PanelKind.Pause) Overlays.Pause(_ui, _session);
                else if (_session.Panel == PanelKind.Options) Overlays.Options(_ui, _session);
                else Panels.Draw(_ui, _state, _session);
                if (_hover != null && !_session.PanelOpen) _ui.Tooltip = _hover.Name + ": " + _hover.Hint;
                HandleSceneClick();
                Overlays.Toast(_ui, _session);
            }
            _ui.DrawTooltip();

            if (_fade > 0)
            {
                _canvas.Rect(Camera.View, Palette.Outline * _fade);
                if (_fade > 0.5f) _canvas.TextCentered("z z z", Camera.Width / 2, Camera.Height / 2 - 4, Palette.Cream * ((_fade - 0.5f) * 2));
            }

            _batch.End();
            base.Draw(gameTime);
        }
    }
}
