using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SpiceWizard.Core;
using SpiceWizard.Web.Art;
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
        GameState _state;

        MouseState _prevMouse;
        KeyboardState _prevKeys;
        Point _mouse;
        bool _clicked;
        bool _clickLatch;
        bool _escape;
        float _dt;
        float _time;

        // Sleep transition: 0 awake, 1 fading to black, 2 fading back in.
        int _sleepPhase;
        float _fade;
        float _cartTimer;
        float _confettiTimer;
        Station _hover;

        public SpiceWizardGame(string savedJson, Action<string> saveHook, Action clearSaveHook, Func<int[]> pollClicks, string demo = null)
        {
            _demo = demo;
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
            _wizard = new WizardActor(Layout.WizardStart);

            _session = new Session
            {
                Particles = _particles,
                HasSave = SaveSystem.FromJson(_savedJson) != null,
                OnWatered = p => _particles.Water(p),
                OnSparkle = (p, color) => _particles.Sparkle(p, color),
                RequestSleep = StartSleep,
                RequestNewGame = NewGame,
                RequestContinue = Continue,
            };
            _state = GameState.NewGame((ulong)DateTime.UtcNow.Ticks);
            _session.Panel = PanelKind.Title;
            if (_demo != null)
            {
                _state = DemoState.Build(_demo == "master");
                _session.Close();
            }
        }

        void NewGame()
        {
            _state = GameState.NewGame((ulong)DateTime.UtcNow.Ticks);
            _clearSaveHook?.Invoke();
            _session.HasSave = false;
            _crowd.Stop();
            _wizard = new WizardActor(Layout.WizardStart);
            _session.Open(PanelKind.Help);
            _session.Say("Welcome to your tower. Click the garden to begin!");
        }

        void Continue()
        {
            var loaded = SaveSystem.FromJson(_savedJson);
            if (loaded == null) { NewGame(); return; }
            _state = loaded;
            _session.Close();
            _session.Say("Welcome back, " + Progression.Title(_state.Level) + ".");
        }

        void Save()
        {
            try { _saveHook?.Invoke(SaveSystem.ToJson(_state)); _session.HasSave = true; }
            catch (Exception) { /* storage may be unavailable; the game still plays */ }
        }

        void StartSleep()
        {
            if (_sleepPhase != 0) return;
            _sleepPhase = 1;
            _fade = 0;
            _session.Close();
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

            if (_session.Panel == PanelKind.Title) { base.Update(gameTime); return; }

            if (_escape && _session.PanelOpen && _session.Panel != PanelKind.Celebration) _session.Close();

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
                if (_confettiTimer <= 0) { _confettiTimer = 0.05f; _particles.Confetti(Camera.Width); }
            }

            // Cauldron steam while a fire is going.
            if ((int)(_time * 10) % 4 == 0) _particles.Steam(new Point(Layout.Cauldron.X + 12, Layout.Cauldron.Y + 2));

            _hover = null;
            if (!_session.PanelOpen && _sleepPhase == 0 && _mouse.Y > Layout.HudHeight)
                foreach (var st in Layout.Stations)
                    if (st.Bounds.Contains(_mouse)) { _hover = st; break; }

            base.Update(gameTime);
        }

        void UpdateSleep()
        {
            if (_sleepPhase == 1)
            {
                _fade += _dt / 0.7f;
                if (_fade >= 1f)
                {
                    _fade = 1f;
                    var report = DayTick.Sleep(_state);
                    Save();
                    _sleepPhase = 2;
                    _cartTimer = 2f;
                    _wizard = new WizardActor(new Point(Layout.Door.X + 7, Layout.Tower.Bottom + 8));
                    if (report.BecameMaster) { _crowd.Start(); }
                }
            }
            else if (_sleepPhase == 2)
            {
                _fade -= _dt / 0.7f;
                if (_fade <= 0f)
                {
                    _fade = 0f;
                    _sleepPhase = 0;
                    _session.Open(_state.LastReport != null && _state.LastReport.BecameMaster ? PanelKind.Celebration : PanelKind.Morning);
                }
            }
        }

        void HandleSceneClick()
        {
            if (!_ui.Clicked || _session.PanelOpen || _sleepPhase != 0 || _hover == null) return;
            _ui.ConsumeClick();
            var st = _hover;
            _wizard.WalkTo(st.Stand, () => UseStation(st));
        }

        void UseStation(Station st)
        {
            switch (st.Kind)
            {
                case StationKind.Plot: _session.Open(PanelKind.Plot, st.Index); break;
                case StationKind.Well:
                    var r = Actions.RefillBucket(_state);
                    _session.Say(r);
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
            _batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, _camera.Transform);

            _ui.Begin(_mouse, _clicked);
            _scene.Draw(_state, _wizard, _particles, _crowd, _hover);

            if (_session.Panel == PanelKind.Title)
            {
                Overlays.Title(_ui, _session, _time);
            }
            else
            {
                Overlays.Hud(_ui, _state, _session);
                if (_session.Panel == PanelKind.Celebration) Overlays.Celebration(_ui, _state, _session, _time);
                else Panels.Draw(_ui, _state, _session);
                if (_hover != null && !_session.PanelOpen) _ui.Tooltip = _hover.Name + ": " + _hover.Hint;
                HandleSceneClick();
                Overlays.Toast(_ui, _session);
            }
            _ui.DrawTooltip();

            if (_fade > 0)
            {
                _canvas.Rect(0, 0, Camera.Width, Camera.Height, Palette.Outline * _fade);
                if (_fade > 0.5f) _canvas.TextCentered("z z z", Camera.Width / 2, Camera.Height / 2 - 4, Palette.Cream * ((_fade - 0.5f) * 2));
            }

            _batch.End();
            base.Draw(gameTime);
        }
    }
}
