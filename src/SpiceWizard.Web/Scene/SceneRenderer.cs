using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using SpiceWizard.Core;
using SpiceWizard.Web.Art;

namespace SpiceWizard.Web.Scene
{
    /// <summary>Sky colours, sun and moon positions and the night tint, all keyed off the clock.</summary>
    public static class DayNight
    {
        /// <summary>0 on a clear day, 1 under rain clouds; set by the scene each frame and folded into the sky and the haze.</summary>
        public static float Overcast;
        static readonly Color OvercastTop = new Color(96, 106, 124);
        static readonly Color OvercastBottom = new Color(150, 158, 170);
        static readonly Color OvercastHaze = new Color(120, 130, 146);

        public static Color SkyTop(double f) => Cloudy(Blend(f, Palette.Navy, new Color(240, 150, 110), Palette.Sky, Palette.Sky, new Color(230, 120, 90), Palette.Navy), OvercastTop, f);
        public static Color SkyBottom(double f) => Cloudy(Blend(f, new Color(60, 50, 90), new Color(250, 200, 140), new Color(190, 225, 245), new Color(190, 225, 245), new Color(250, 170, 110), new Color(60, 50, 90)), OvercastBottom, f);

        /// <summary>Greys a daytime colour under cloud; the night sky is dark whatever the weather.</summary>
        static Color Cloudy(Color c, Color grey, double f) => Color.Lerp(c, grey, Overcast * (1f - Darkness(f)));

        /// <summary>Overlay drawn over the whole scene; alpha is folded into the colour.</summary>
        public static Color Tint(double f)
        {
            if (f < 0.05) return Palette.Navy * (float)(0.35 * (1 - f / 0.05));
            if (f < 0.75) return Color.Transparent;
            if (f < 0.9) return new Color(200, 90, 40) * (float)(0.22 * (f - 0.75) / 0.15);
            return Blend01(new Color(200, 90, 40) * 0.22f, Palette.Navy * 0.42f, (f - 0.9) / 0.1);
        }

        /// <summary>How much the lamps and fires show, 0 by day and 1 at night; they come up through the
        /// evening a little ahead of the dark so the yard never goes flat before the lights arrive.</summary>
        public static float Lamplight(double f)
        {
            if (f < 0.08) return (float)(1 - f / 0.08);
            if (f > 0.74) return (float)Math.Min(1, (f - 0.74) / 0.2);
            return 0f;
        }

        public static bool IsDark(double f) => f > 0.93 || f < 0.02;

        /// <summary>0 in full daylight, 1 in the middle of the night; used to sink distant things into the dark.</summary>
        public static float Darkness(double f)
        {
            if (f < 0.06) return (float)(1 - f / 0.06);
            if (f > 0.82) return (float)((f - 0.82) / 0.18);
            return 0f;
        }

        /// <summary>Distant colours fade toward the night sky.</summary>
        public static Color Haze(Color c, double f) => Color.Lerp(Color.Lerp(c, OvercastHaze, 0.35f * Overcast), Palette.Navy, 0.7f * Darkness(f));

        public static Point SunPos(double f)
        {
            double a = Math.PI * (1 - f);
            return new Point((int)(192 + 170 * Math.Cos(a)) - 5, (int)(Layout.Horizon - 2 - 62 * Math.Sin(a)) - 5);
        }

        /// <summary>How far a shadow leans sideways per pixel of height: rightward in the morning (sun in the
        /// east, on the left), nothing at noon, leftward through the afternoon.</summary>
        public static float ShadowShear(double f) => (float)(-Math.Cos(Math.PI * (1 - f)) * 2.0);

        /// <summary>How much of a thing's height its shadow keeps along the ground: short at noon, long at either end of the day.</summary>
        public static float ShadowSquash(double f) => 0.2f + 0.34f * (1f - (float)Math.Max(0, Math.Sin(Math.PI * (1 - f))));

        public static Point MoonPos(double f)
        {
            double t = Math.Clamp((f - 0.85) / 0.15, 0, 1);
            return new Point(40 + (int)(60 * t), 60 - (int)(30 * t));
        }

        static Color Blend(double f, Color night, Color dawn, Color day1, Color day2, Color dusk, Color night2)
        {
            if (f < 0.1) return Blend01(night, dawn, f / 0.1);
            if (f < 0.25) return Blend01(dawn, day1, (f - 0.1) / 0.15);
            if (f < 0.7) return day2;
            if (f < 0.88) return Blend01(day2, dusk, (f - 0.7) / 0.18);
            return Blend01(dusk, night2, (f - 0.88) / 0.12);
        }

        static Color Blend01(Color a, Color b, double t) => Color.Lerp(a, b, (float)Math.Clamp(t, 0, 1));
    }

    /// <summary>Draws the yard, the tower, the garden and everyone in it, plus as much meadow as the window shows.</summary>
    public sealed class SceneRenderer
    {
        readonly Canvas _c;
        float _time;
        public float CartX = Layout.CartPark.X;
        /// <summary>0..1 gust strength, set each frame by the audio mixer so the meadow leans with the sound.</summary>
        public float Wind;
        /// <summary>Today's sky, read off the game state each frame.</summary>
        public Weather Weather;
        // The weather eases in over a couple of seconds rather than snapping when the day turns.
        float _windy, _overcast;
        /// <summary>Sideways push on smoke, steam and leaves right now, in pixels per second.</summary>
        public float WindPush => Wind * 6f + _windy * 26f;
        /// <summary>True while it is blowing hard enough for leaves to come loose.</summary>
        public bool Blustery => _windy > 0.5f;
        /// <summary>The tower door stands open while the wizard goes in or comes out.</summary>
        public bool DoorOpen;
        /// <summary>Lights the windows whatever the hour (the wizard is up and about inside).</summary>
        public bool WindowsLit;
        /// <summary>While true, little z's drift up from the bedroom window.</summary>
        public bool Snoring;
        /// <summary>Bobbing "!" markers over anything that wants clicking; off while a panel covers the yard.</summary>
        public bool ShowMarkers = true;
        /// <summary>Extra steam for a moment after a sauce is cooked (the game triples the steam while it runs).</summary>
        public float BurstTimer;
        // The brew takes the colour of the last sauce cooked and settles back to the everyday green over ~20 s.
        Color _brewColor = Palette.LightGreen;
        float _brewFade;
        const float BrewFadeSeconds = 20f;
        readonly List<Point> _markers = new List<Point>();

        // Light sources queued while the yard is drawn, then added over the night tint in one additive pass.
        // Day is how much of the light shows in full daylight (a fire still glows at noon; a lamp does not).
        struct LightSrc { public int X, Y, RX, RY, W, H; public Color Color; public float Day; public bool IsRect; }
        readonly List<LightSrc> _lights = new List<LightSrc>();
        /// <summary>The warm cast of fire and lamplight, and the cold one of the ghost pepper.</summary>
        public static readonly Color FireLight = new Color(255, 150, 60);
        public static readonly Color LampLight = new Color(255, 190, 110);
        public static readonly Color GhostLight = new Color(120, 190, 255);
        /// <summary>0 by day and 1 at night, for anything outside the renderer that wants to light up with the lamps.</summary>
        public float Lamp { get; private set; }
        /// <summary>The tops of the town's chimneys, for hearth smoke; rebuilt with the town each frame.</summary>
        public readonly List<Point> Chimneys = new List<Point>();
        // Thunder: seconds until the next flash, and how bright the sky is right now.
        float _lightningIn = 30f, _flash;
        /// <summary>Fires when the lightning does, so the mixer can roll the thunder.</summary>
        public Action OnLightning;
        // A shooting star: when it started and where it is heading.
        float _starAt = -10f;
        int _starX, _starY;

        // Scenery outside the 384x216 design box, generated once per window size.
        public struct Prop { public string Sprite; public int X, Y; public int Base; }
        readonly List<Prop> _props = new List<Prop>();
        Rectangle _propsFor;
        /// <summary>The meadow scenery for the current window (see <see cref="EnsureProps"/>).</summary>
        public IReadOnlyList<Prop> Props => _props;
        public void EnsureProps() { if (_propsFor != Camera.View) BuildProps(); }

        static readonly Rectangle Yard = new Rectangle(0, 0, Camera.Width, Camera.Height);

        // The station under the mouse this frame, and the sprites that make it up. Those are drawn again
        // over a silhouette outline once everything else is down, so the highlight follows the object's
        // shape rather than its click box.
        Station _hover;
        struct Part { public string Sprite; public int X, Y; public float Lean; public Color Tint; }
        readonly List<Part> _hoverParts = new List<Part>();

        public SceneRenderer(Canvas canvas) { _c = canvas; }

        /// <summary>Something was just cooked: the cauldron takes on the sauce's colour and belches steam.</summary>
        public void Brew(Color sauce)
        {
            _brewColor = sauce;
            _brewFade = BrewFadeSeconds;
            BurstTimer = 1.5f;
        }

        public void Update(float dt)
        {
            _time += dt;
            if (_brewFade > 0) _brewFade -= dt;
            if (BurstTimer > 0) BurstTimer -= dt;
            // Storms flash every so often once the rain deck is fully in.
            if (_flash > 0) _flash -= dt * 6f;
            if (_overcast > 0.9f)
            {
                _lightningIn -= dt;
                if (_lightningIn <= 0)
                {
                    _lightningIn = 25f + Hash((int)(_time * 100), 61) % 50;
                    _flash = 1f;
                    OnLightning?.Invoke();
                }
            }
            float windy = Weather == Weather.Windy ? 1f : Weather == Weather.Rain ? 0.35f : 0f;
            float overcast = Weather == Weather.Rain ? 1f : 0f;
            _windy = Approach(_windy, windy, dt * 0.5f);
            _overcast = Approach(_overcast, overcast, dt * 0.5f);
            DayNight.Overcast = _overcast;
        }

        static float Approach(float v, float target, float step) =>
            v < target ? Math.Min(target, v + step) : Math.Max(target, v - step);

        /// <summary>Small deterministic hash so scattered details stay put from frame to frame.</summary>
        static int Hash(int x, int y, int salt = 0)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + salt * (int)2246822519u;
                h = (h ^ (h >> 13)) * 1274126177;
                return (h ^ (h >> 16)) & 0x7fffffff;
            }
        }

        public void Draw(GameState s, WizardActor wizard, Particles particles, Crowd crowd, Cats cats, Villagers villagers, Station hover)
        {
            double f = s.Clock.DayFraction;
            Weather = s.Weather;
            EnsureProps();
            _hover = hover;
            _hoverParts.Clear();
            _markers.Clear();
            _lights.Clear();

            DrawSky(f);
            DrawHills(f);
            DrawTreeLine(f);
            DrawTown(f);
            DrawGround();
            DrawCastShadows(f, wizard, cats, villagers);
            DrawProps(f);
            DrawTower(f, wizard);
            DrawGarden(s, hover);
            DrawStations(s, hover);
            DrawHover();
            crowd.Draw(_c);
            villagers.Draw(_c);
            cats.Draw(_c, DayNight.IsDark(f));
            if (!wizard.InDoorway) wizard.Draw(_c);
            particles.Draw(_c);
            DrawMarkers();
            // Under rain clouds the whole yard goes a shade cooler and dimmer, then the rain falls over it.
            if (_overcast > 0.01f) _c.Rect(Camera.View, new Color(60, 76, 110) * (0.2f * _overcast * (1f - DayNight.Darkness(f))));
            DrawRain();
            var tint = DayNight.Tint(f);
            if (tint.A > 0) _c.Rect(Camera.View, tint);
            // Lightning: the whole window whites out for an instant, with a second dimmer flicker behind it.
            if (_flash > 0)
            {
                float a = _flash > 0.7f ? 0.55f : _flash > 0.45f ? 0.05f : _flash > 0.3f ? 0.25f : 0f;
                if (a > 0) _c.Rect(Camera.View, Palette.White * a);
            }
            // Everything that glows goes on last, brightening whatever it falls on.
            _c.BeginAdditive();
            DrawLights(f);
            particles.Draw(_c, true);
            _c.EndAdditive();
        }

        // ---- Lights ------------------------------------------------------------------------------

        /// <summary>Queues a pool of light centred on (x, y); <paramref name="day"/> is its strength in full daylight.</summary>
        void Light(int x, int y, int rx, int ry, Color color, float day = 0f) =>
            _lights.Add(new LightSrc { X = x, Y = y, RX = rx, RY = ry, Color = color, Day = day });

        /// <summary>Queues a lit rectangle (a window pane, a fire-lit rim) for the additive pass.</summary>
        void LightRect(int x, int y, int w, int h, Color color, float day = 0f) =>
            _lights.Add(new LightSrc { X = x, Y = y, W = w, H = h, Color = color, Day = day, IsRect = true });

        void DrawLights(double f)
        {
            float lamp = DayNight.Lamplight(f);
            // Under rain clouds the day is dim enough for the fire and the windows to show a little.
            lamp = Math.Max(lamp, 0.3f * _overcast);
            Lamp = lamp;
            foreach (var l in _lights)
            {
                float k = l.Day + (1f - l.Day) * lamp;
                if (k < 0.02f) continue;
                var col = Canvas.Tone(l.Color, k);
                if (l.IsRect) _c.Rect(l.X, l.Y, l.W, l.H, col);
                else _c.Light(l.X, l.Y, l.RX, l.RY, col);
            }
            _lights.Clear();
        }

        // ---- Rain --------------------------------------------------------------------------------

        /// <summary>Streaks falling over the whole window, slanting with the wind, and drops bouncing off the
        /// ground. Everything is hashed off time so there is nothing to keep track of between frames.</summary>
        void DrawRain()
        {
            if (_overcast < 0.05f) return;
            var view = Camera.View;
            int w = Math.Max(1, view.Width), h = Math.Max(1, view.Height);
            int slant = _windy > 0.5f ? 1 : 0;
            float drift = 12f + 40f * _windy;
            var streak = Palette.PaleBlue * (0.7f * _overcast);
            var streakDim = Palette.Sky * (0.4f * _overcast);
            int n = w * h / 520;
            for (int i = 0; i < n; i++)
            {
                int hh = Hash(i, 51);
                float speed = 190f + (hh % 60);
                int y0 = (hh >> 6) % h, x0 = (hh >> 14) % w;
                int y = view.Top + (int)((y0 + _time * speed) % h);
                int x = view.Left + (int)((x0 + _time * drift + (y - view.Top) * slant) % w);
                var col = i % 3 == 0 ? streakDim : streak;
                for (int r = 0; r < 3; r++) _c.Rect(x + r * slant, y - r, 1, 1, col);
            }
            // Puddles on the road catch the sky: a few fixed pale glints that shimmer as the rain hits them.
            var glint = Palette.PaleBlue * (0.5f * _overcast);
            for (int x = view.Left - (view.Left % 7 + 7) % 7; x < Layout.Tower.X; x += 7)
            {
                if (Hash(x, 53) % 4 != 0) continue;
                int y = Layout.RoadTop + 2 + Hash(x, 54) % (Layout.RoadBottom - Layout.RoadTop - 4);
                int wobble = ((int)(_time * 6) + Hash(x, 55)) % 3;
                _c.Rect(x + wobble - 1, y, 3, 1, glint);
                _c.Rect(x + 1 - wobble, y + 1, 2, 1, glint * 0.6f);
            }
            // Splashes: little flicks that wink on and off wherever a drop lands on grass, soil or road.
            int rows = Math.Max(1, view.Bottom - Layout.Horizon);
            var splash = Palette.PaleBlue * (0.6f * _overcast);
            for (int i = 0; i < n / 3; i++)
            {
                int hh = Hash(i, 52);
                int x = view.Left + (hh >> 4) % w, y = Layout.Horizon + (hh >> 13) % rows;
                int beat = ((int)(_time * 9) + hh) % 7;
                if (beat == 0) _c.Rect(x, y, 1, 1, splash);
                else if (beat == 1) { _c.Rect(x - 1, y - 1, 1, 1, splash); _c.Rect(x + 1, y - 1, 1, 1, splash); }
            }
        }

        bool Hot(StationKind kind, int index = 0) => _hover != null && _hover.Kind == kind && _hover.Index == index;

        /// <summary>Queues a "!" to bob at (x, y): x is the marker's centre, y where its bottom edge hovers.</summary>
        void Marker(int x, int y) { if (ShowMarkers) _markers.Add(new Point(x, y)); }

        void DrawMarkers()
        {
            int bob = (int)Math.Round(Math.Sin(_time * 5) * 1.5);
            foreach (var m in _markers)
            {
                _c.Rect(m.X - 1, m.Y + 1, 3, 1, Palette.Shadow);
                _c.Sprite("ic_bang", m.X - 2, m.Y - 8 + bob);
            }
        }

        /// <summary>Draws one sprite of a station, remembering it for the hover outline when that station is hot.</summary>
        void StationSprite(bool hot, string sprite, int x, int y) => StationSprite(hot, sprite, x, y, Color.White, 0f);
        void StationSprite(bool hot, string sprite, int x, int y, Color tint) => StationSprite(hot, sprite, x, y, tint, 0f);
        void StationSprite(bool hot, string sprite, int x, int y, Color tint, float lean)
        {
            _c.SpriteSway(sprite, x, y, lean, tint);
            if (hot) _hoverParts.Add(new Part { Sprite = sprite, X = x, Y = y, Lean = lean, Tint = tint });
        }

        void DrawHover()
        {
            if (_hoverParts.Count == 0) return;
            var glow = Color.Lerp(Palette.Yellow, Palette.LightYellow, 0.5f + 0.5f * (float)Math.Sin(_time * 6));
            foreach (var p in _hoverParts) _c.SpriteOutline(p.Sprite, p.X, p.Y, p.Lean, glow);
            foreach (var p in _hoverParts) _c.SpriteSway(p.Sprite, p.X, p.Y, p.Lean, p.Tint);
        }

        // ---- Sky ---------------------------------------------------------------------

        void DrawSky(double f)
        {
            var view = Camera.View;
            var top = DayNight.SkyTop(f);
            var bottom = DayNight.SkyBottom(f);
            int h = Layout.Horizon - view.Top;
            const int bands = 16;
            for (int i = 0; i < bands; i++)
            {
                int y0 = view.Top + i * h / bands, y1 = view.Top + (i + 1) * h / bands;
                _c.Rect(view.Left, y0, view.Width, y1 - y0, Color.Lerp(top, bottom, i / (float)(bands - 1)));
            }
            float dark = DayNight.Darkness(f);
            int rows = Layout.Horizon - 30 - view.Top;
            if (dark > 0.3f && _overcast < 0.95f)
            {
                float a = (dark - 0.3f) / 0.7f * (1f - _overcast);
                for (int i = 0; i < 140; i++)
                {
                    int sx = view.Left + Hash(i, 1) % view.Width, sy = view.Top + Hash(i, 2) % Math.Max(1, rows);
                    float tw = 0.45f + 0.55f * (float)Math.Sin(_time * 2 + i);
                    _c.Rect(sx, sy, 1, 1, Palette.White * (tw * a));
                    if (i % 9 == 0) { _c.Rect(sx - 1, sy, 3, 1, Palette.White * (0.25f * a)); _c.Rect(sx, sy - 1, 1, 3, Palette.White * (0.25f * a)); }
                }
            }
            // Once in a while on a clear night a star falls: a short streak that fades as it goes.
            if (dark > 0.6f && _overcast < 0.5f)
            {
                float since = _time - _starAt;
                if (since > 0.8f && Hash((int)(_time * 2), 62) % 90 == 0)
                {
                    _starAt = _time;
                    _starX = view.Left + 20 + Hash((int)_time, 63) % Math.Max(1, view.Width - 60);
                    _starY = view.Top + 10 + Hash((int)_time, 64) % Math.Max(1, rows / 2);
                    since = 0f;
                }
                if (since < 0.6f)
                {
                    int head = (int)(since * 90);
                    for (int k = 0; k < 5; k++)
                        _c.Rect(_starX + head - k * 2, _starY + (head - k * 2) / 3, 1, 1, Palette.White * ((1f - since / 0.6f) * (1f - k * 0.2f)));
                }
            }
            float clear = 1f - _overcast;
            if (f > 0.02 && f < 0.98 && clear > 0.01f)
            {
                var sun = DayNight.SunPos(f);
                _c.Rect(sun.X - 2, sun.Y + 1, 14, 8, Palette.LightYellow * (0.18f * clear));
                _c.Rect(sun.X + 1, sun.Y - 2, 8, 14, Palette.LightYellow * (0.18f * clear));
                _c.Sprite("sun", sun.X, sun.Y, Color.White * clear);
            }
            if ((f > 0.85 || f < 0.05) && clear > 0.01f)
            {
                var m = DayNight.MoonPos(f < 0.05 ? 1 : f);
                _c.Sprite("moon", m.X, m.Y, Color.White * clear);
                Light(m.X + 5, m.Y + 5, 16, 14, Canvas.Tone(new Color(150, 170, 220), 0.35f * clear));
            }
            // Clouds drift at a few heights; the higher ones only show when the window is tall.
            // Each one lives in world space and repeats every CloudPeriod pixels, so the window
            // size only decides how many copies are visible, never where a cloud is.
            const int CloudPeriod = 640;
            int[] dy = { -86, -70, -56, -40, -30 };
            int[] speed = { 3, 5, 4, 6, 3 };
            string[] kind = { "cloud_big", "cloud", "cloud_big", "cloud", "cloud" };
            float[] alpha = { 0.6f, 0.9f, 0.75f, 0.85f, 0.7f };
            var cloudTint = Color.Lerp(Color.Lerp(Color.White, new Color(150, 156, 168), _overcast), Palette.Navy, 0.5f * dark);
            // Wind hurries the clouds along; the timer below keeps their spacing when the speed changes.
            float hurry = 1f + 3f * _windy;
            _cloudTime += hurry * (_time - _cloudClock); _cloudClock = _time;
            for (int i = 0; i < dy.Length; i++)
            {
                int y = Layout.Horizon + dy[i];
                if (y < view.Top - 12) continue;
                int w = _c.Size(kind[i]).X;
                int x = (int)(_cloudTime * speed[i] + i * 137) % CloudPeriod;
                while (x + w > view.Left) x -= CloudPeriod;
                for (x += CloudPeriod; x < view.Right; x += CloudPeriod)
                    _c.Sprite(kind[i], x, y, cloudTint * alpha[i]);
            }
            // The rain deck: a low, close-packed layer of dark cloud that slides in over the hills.
            if (_overcast > 0.01f)
            {
                var deck = Color.Lerp(new Color(88, 96, 114), Palette.Navy, 0.6f * dark) * _overcast;
                var deckLit = Color.Lerp(new Color(112, 120, 138), Palette.Navy, 0.6f * dark) * _overcast;
                const int DeckPeriod = 236;
                int[] ddy = { -64, -52, -44 };
                int[] dsp = { 9, 7, 11 };
                for (int i = 0; i < ddy.Length; i++)
                {
                    string k = i == 1 ? "cloud" : "cloud_big";
                    int y = Layout.Horizon + ddy[i] - (int)(14 * (1f - _overcast));
                    int w = _c.Size(k).X;
                    int x = (int)(_cloudTime * dsp[i] + i * 91) % DeckPeriod;
                    while (x + w > view.Left) x -= DeckPeriod;
                    for (x += DeckPeriod; x < view.Right; x += DeckPeriod)
                        _c.Sprite(k, x + (i == 2 ? 60 : 0), y, i == 0 ? deck : deckLit);
                }
            }
        }
        float _cloudTime, _cloudClock;

        // ---- Distance: hills, tree line, the town ----------------------------------------

        static int FarHill(int x) => Layout.Horizon - 16 - (int)(9 * Math.Sin(x * 0.019) + 5 * Math.Sin(x * 0.047 + 1.3) + 3 * Math.Sin(x * 0.11 + 2.1));
        static int MidHill(int x) => Layout.Horizon - 7 - (int)(6 * Math.Sin(x * 0.029 + 2.0) + 4 * Math.Sin(x * 0.071 + 0.4));

        void DrawHills(double f)
        {
            var view = Camera.View;
            var far = DayNight.Haze(Palette.FarHill, f);
            var farRim = DayNight.Haze(Color.Lerp(Palette.FarHill, Palette.White, 0.25f), f);
            var mid = DayNight.Haze(Palette.MidHill, f);
            var midRim = DayNight.Haze(Color.Lerp(Palette.MidHill, Palette.LightGrass, 0.5f), f);
            for (int x = view.Left; x < view.Right; x++)
            {
                int y = FarHill(x);
                _c.Rect(x, y, 1, 1, farRim);
                _c.Rect(x, y + 1, 1, Layout.Horizon - y - 1, far);
            }
            for (int x = view.Left; x < view.Right; x++)
            {
                int y = MidHill(x);
                _c.Rect(x, y, 1, 1, midRim);
                _c.Rect(x, y + 1, 1, Layout.Horizon - y - 1, mid);
            }
        }

        void DrawTreeLine(double f)
        {
            // A dark hedge of distant trees right on the horizon, behind the town and the tower.
            var view = Camera.View;
            var dark = DayNight.Haze(Palette.DarkGreen, f);
            var lit = DayNight.Haze(Palette.Green, f);
            for (int x = view.Left - (view.Left % 5 + 5) % 5; x < view.Right; x += 5)
            {
                int h = 3 + Hash(x, 7) % 5;
                int w = 4 + Hash(x, 8) % 3;
                _c.Rect(x, Layout.Horizon - h, w, h, dark);
                _c.Rect(x + 1, Layout.Horizon - h - 1, w - 2, 1, dark);
                _c.Rect(x + 1, Layout.Horizon - h, 1, 1, lit);
            }
        }

        void DrawTown(double f)
        {
            // Little houses along the horizon on the left; the row carries on into the margin.
            bool dark = DayNight.Darkness(f) > 0.5f;
            Chimneys.Clear();
            var wall = DayNight.Haze(Palette.LightStone, f);
            var wallShade = DayNight.Haze(Palette.Stone, f);
            var outline = DayNight.Haze(Palette.Outline, f);
            int H = Layout.Horizon;
            for (int i = -60; i <= 7; i++)
            {
                int x = 4 + i * 19 + Hash(i, 3) % 5;
                int w = 12 + Hash(i, 4) % 4;
                if (x + w > 150) continue;
                if (x + w < Camera.Left - 2 || x > Camera.Right + 2) continue;
                int hgt = 10 + Hash(i, 5) % 11;
                // Walls with a shaded right face.
                _c.Rect(x, H - hgt, w, hgt, wall);
                _c.Rect(x + w - 3, H - hgt, 3, hgt, wallShade);
                _c.Rect(x - 1, H - hgt, 1, hgt, outline);
                _c.Rect(x + w, H - hgt, 1, hgt, outline);
                // Gable roof in the house's own material, eaves overhanging the walls.
                int rh = 4 + Hash(i, 6) % 3;
                int cx = x + w / 2;
                Roof(cx, H - hgt - rh, w / 2 + 2, rh, Hash(i, 7) % 3, f);
                // Chimney poking out of the shaded slope.
                if (Hash(i, 8) % 3 == 0)
                {
                    int chx = cx + w / 4 + 1, chy = H - hgt - rh + 1;
                    _c.Rect(chx - 1, chy - 3, 4, 4, outline);
                    _c.Rect(chx, chy - 2, 2, 3, wallShade);
                    _c.Rect(chx, chy - 2, 1, 1, wall);
                    Chimneys.Add(new Point(chx, chy - 4));
                }
                // Door and a window.
                _c.Rect(x + w - 5, H - 5, 3, 5, DayNight.Haze(Palette.DarkBrown, f));
                _c.Rect(x + 3, H - hgt + 3, 2, 2, dark ? Palette.Yellow : DayNight.Haze(Palette.Navy, f));
                if (dark) Light(x + 4, H - hgt + 4, 5, 4, Canvas.Tone(LampLight, 0.5f));
                // The church spire: a tall slate steeple with a cross.
                if (i == 3)
                {
                    int sx = x + w / 2;
                    _c.Rect(sx - 2, H - hgt - 16, 5, 13, wall);
                    _c.Rect(sx + 1, H - hgt - 16, 2, 13, wallShade);
                    _c.Rect(sx - 3, H - hgt - 16, 1, 13, outline);
                    _c.Rect(sx + 3, H - hgt - 16, 1, 13, outline);
                    Roof(sx, H - hgt - 24, 3, 8, 1, f);
                    _c.Rect(sx, H - hgt - 27, 1, 3, outline);
                    _c.Rect(sx - 1, H - hgt - 26, 3, 1, outline);
                }
            }
        }

        // A pitched roof: apex at (cx, top), widening to `span` either side of centre at the
        // eaves. Lit on the left slope, shaded on the right, with alternating tile courses,
        // a ridge cap and a drip edge. Material 0 = red tile, 1 = slate, 2 = thatch.
        void Roof(int cx, int top, int span, int rh, int material, double f)
        {
            Color lit, mid, dim;
            switch (material)
            {
                case 0: lit = Palette.Red; mid = Palette.DarkRed; dim = Color.Lerp(Palette.DarkRed, Palette.Outline, 0.4f); break;
                case 1: lit = Palette.Stone; mid = Palette.DarkStone; dim = Palette.Charcoal; break;
                default: lit = Palette.Tan; mid = Palette.Brown; dim = Palette.DarkBrown; break;
            }
            lit = DayNight.Haze(lit, f); mid = DayNight.Haze(mid, f); dim = DayNight.Haze(dim, f);
            var outline = DayNight.Haze(Palette.Outline, f);
            for (int r = 0; r < rh; r++)
            {
                int rw = 1 + (span - 1) * r / Math.Max(1, rh - 1);
                int y = top + r;
                bool course = r % 2 == 1;
                _c.Rect(cx - rw - 1, y, rw * 2 + 3, 1, outline);
                _c.Rect(cx - rw, y, rw, 1, course ? mid : lit);
                _c.Rect(cx, y, rw + 1, 1, course ? dim : mid);
                // Tile ends picked out along each course.
                if (course)
                    for (int tx = cx - rw + 1; tx <= cx + rw; tx += 3)
                        _c.Rect(tx, y, 1, 1, tx < cx ? lit : mid);
            }
            _c.Rect(cx - 1, top - 1, 3, 1, outline);          // ridge cap
            _c.Rect(cx - span - 1, top + rh, span * 2 + 3, 1, outline); // drip edge
        }

        // ---- Ground ----------------------------------------------------------------------

        void DrawGround()
        {
            var view = Camera.View;
            int top = Layout.Horizon;
            _c.Rect(view.Left, top, view.Width, view.Bottom - top, Palette.Grass);
            _c.Rect(view.Left, top, view.Width, 1, Palette.DarkGrass);
            _c.Rect(view.Left, top + 1, view.Width, 1, Palette.LightGrass);

            // Sunlit patches and tufts, scattered but fixed.
            int cell = 24;
            for (int gy = view.Top - (view.Top % cell + cell) % cell; gy < view.Bottom; gy += cell)
                for (int gx = view.Left - (view.Left % cell + cell) % cell; gx < view.Right; gx += cell)
                {
                    int h = Hash(gx, gy, 11);
                    int x = gx + h % cell, y = gy + (h >> 8) % cell;
                    if (y < top + 4) continue;
                    int kind = (h >> 16) % 10;
                    if (kind < 3)
                    {
                        int w = 6 + (h >> 20) % 10;
                        _c.Rect(x - w / 2, y, w, 2, Palette.LightGrass);
                        _c.Rect(x - w / 2 + 2, y - 1, w - 4, 1, Palette.LightGrass);
                        _c.Rect(x - w / 2 + 2, y + 2, w - 4, 1, Palette.LightGrass);
                    }
                    else if (kind < 8)
                    {
                        // Tufts lean with the same breeze as the flowers, so the wind shows on the ground too.
                        int lean = (int)Math.Round(Sway("flower", x, y) * 0.6f);
                        _c.Rect(x, y, 1, 2, Palette.DarkGrass);
                        _c.Rect(x + 2 + lean, y + 1, 1, 1, Palette.DarkGrass);
                        _c.Rect(x + 1 + lean, y - 1, 1, 1, Palette.DarkGrass);
                    }
                }

            // Road from the town to the tower, with worn wheel ruts.
            int rt = Layout.RoadTop, rb = Layout.RoadBottom, right = Layout.Tower.X;
            _c.Rect(view.Left, rt - 1, right - view.Left, 1, Palette.DarkGrass);
            _c.Rect(view.Left, rt, right - view.Left, rb - rt, Palette.Road);
            _c.Rect(view.Left, rt, right - view.Left, 1, Palette.LightTan);
            _c.Rect(view.Left, rb - 1, right - view.Left, 1, Palette.Tan);
            _c.Rect(view.Left, rb, right - view.Left, 1, Palette.DarkGrass);
            for (int x = view.Left - (view.Left % 9 + 9) % 9; x < right; x += 9)
            {
                _c.Rect(x, rt + 3, 4, 1, Palette.Tan);
                _c.Rect(x + 4, rt + 8, 4, 1, Palette.Tan);
                if (Hash(x, 12) % 3 == 0) _c.Rect(x + 2 + Hash(x, 13) % 5, rt + 5 + Hash(x, 14) % 3, 1, 1, Palette.LightTan);
            }
            // Where the road meets the tower the grass creeps in.
            _c.Rect(right - 2, rt, 2, rb - rt, Palette.DarkGrass);
        }

        // ---- Sun shadows -----------------------------------------------------------------------

        /// <summary>The shadows the sun throws across the grass. Everything that stands up gets one; they all lean
        /// the same way and stretch out together toward dawn and dusk, then fade with the light.</summary>
        void DrawCastShadows(double f, WizardActor wizard, Cats cats, Villagers villagers)
        {
            float shear = DayNight.ShadowShear(f);
            float squash = DayNight.ShadowSquash(f);
            float sunUp = 1f - DayNight.Darkness(f);
            // Only a straight-overhead sun gives no lean; then the contact shadows alone do the job.
            if (Math.Abs(shear) < 0.35f || sunUp <= 0.01f) return;
            float a = 0.22f * sunUp * (1f - 0.85f * _overcast) * Math.Min(1f, (Math.Abs(shear) - 0.35f) / 0.4f);
            if (a < 0.01f) return;
            var col = Palette.Outline * a;

            // The tower: its wall as a sheared block, cut off at the horizon so it never climbs into the sky.
            var t = Layout.Tower;
            int lastY = int.MinValue;
            for (int up = 0; up < t.Height * 2 / 3; up++)
            {
                int y = t.Bottom - (int)Math.Round(up * squash);
                if (y == lastY) continue;
                lastY = y;
                if (y < Layout.Horizon) break;
                int dx = (int)Math.Round(up * shear);
                int w = t.Width;
                _c.Rect(t.X + dx + (t.Width - w) / 2, y, w, 1, col);
            }
            foreach (var p in _props)
                if (p.Sprite != "rock" && !p.Sprite.StartsWith("flower"))
                    _c.CastShadow(p.Sprite, p.X, p.Base - 1, shear, squash, col);
            foreach (var tr in Layout.Trees) _c.CastShadow("tree", tr.X, tr.Y + 21, shear, squash, col);
            foreach (var b in Layout.Bushes) _c.CastShadow("bush", b.X, b.Y + 7, shear, squash, col);
            for (int i = 0; i <= Layout.FenceBays; i++)
                _c.CastShadow("fence_post", Layout.Fence.X + i * 12, Layout.Fence.Y + 9, shear, squash, col);
            _c.CastShadow("well", Layout.Well.X, Layout.Well.Y + 25, shear, squash, col);
            _c.CastShadow("board", Layout.Board.X, Layout.Board.Y + 23, shear, squash, col);
            _c.CastShadow("cart", (int)CartX, Layout.CartPark.Y + 21, shear, squash, col);
            if (Math.Abs(CartX - Layout.CartPark.X) < 1) _c.CastShadow("merchant", Layout.Merchant.X, Layout.Merchant.Y + 17, shear, squash, col);
            _c.CastShadow("cauldron", Layout.Cauldron.X, Layout.Cauldron.Y + 21, shear, squash, col);
            _c.CastShadow("crate", Layout.Crate.X, Layout.Crate.Y + 13, shear, squash, col);
            _c.CastShadow("pantry", Layout.Pantry.X, Layout.Pantry.Y + 13, shear, squash, col);
            _c.CastShadow("stump", Layout.Stump.X, Layout.Stump.Y + 9, shear, squash, col);
            wizard.DrawCastShadow(_c, shear, squash, col);
            villagers.DrawCastShadows(_c, shear, squash, col);
            cats.DrawCastShadows(_c, shear, squash, col);
        }

        // ---- Meadow outside the yard ---------------------------------------------------------

        void BuildProps()
        {
            _propsFor = Camera.View;
            _props.Clear();
            var view = Camera.View;
            const int cell = 32;
            var road = new Rectangle(view.Left, Layout.RoadTop - 2, Layout.Tower.X - view.Left, Layout.RoadBottom - Layout.RoadTop + 4);
            var yardPad = Yard; yardPad.Inflate(2, 2);
            for (int gy = view.Top - (view.Top % cell + cell) % cell; gy < view.Bottom + cell; gy += cell)
                for (int gx = view.Left - (view.Left % cell + cell) % cell; gx < view.Right + cell; gx += cell)
                {
                    int h = Hash(gx, gy, 21);
                    int roll = h % 100;
                    string sprite;
                    if (roll < 18) sprite = "tree_big";
                    else if (roll < 32) sprite = "tree";
                    else if (roll < 50) sprite = "bush";
                    else if (roll < 57) sprite = "rock";
                    else if (roll < 78) sprite = (roll % 3 == 0) ? "flower_pink" : (roll % 3 == 1) ? "flower_yellow" : "flower_white";
                    else continue;
                    var size = _c.Size(sprite);
                    int x = gx + (h >> 7) % cell, y = gy + (h >> 14) % cell;
                    int baseY = y + size.Y - (sprite.StartsWith("tree") ? 3 : sprite == "bush" ? 1 : 0);
                    var r = new Rectangle(x, y, size.X, size.Y);
                    if (baseY < Layout.Horizon + 2) continue;             // nothing grows in the sky
                    if (r.Intersects(yardPad)) continue;                   // keep the yard as designed
                    if (r.Intersects(road)) continue;
                    _props.Add(new Prop { Sprite = sprite, X = x, Y = y, Base = baseY });
                }
            _props.Sort((a, b) => a.Base.CompareTo(b.Base));
        }

        void DrawProps(double f)
        {
            foreach (var p in _props)
            {
                if (p.Sprite.StartsWith("tree")) _c.GroundShadow(p.X + _c.Size(p.Sprite).X / 2, p.Sprite == "tree_big" ? 12 : 8, p.Base - 1);
                else if (p.Sprite == "bush") _c.GroundShadow(p.X + 6, 10, p.Base - 1, 0.7f);
                else if (p.Sprite == "rock") _c.GroundShadow(p.X + 4, 8, p.Base - 1, 0.7f);
                _c.SpriteSway(p.Sprite, p.X, p.Y, Sway(p.Sprite, p.X, p.Y));
            }
            foreach (var t in Layout.Trees) { _c.GroundShadow(t.X + 8, 8, t.Y + 20); _c.SpriteSway("tree", t.X, t.Y, Sway("tree", t.X, t.Y)); }
            foreach (var b in Layout.Bushes) { _c.GroundShadow(b.X + 6, 10, b.Y + 6, 0.7f); _c.SpriteSway("bush", b.X, b.Y, Sway("bush", b.X, b.Y)); }
            for (int i = 0; i < Layout.FenceBays; i++) _c.Sprite(FenceBay(i), Layout.Fence.X + i * 12, Layout.Fence.Y);
            _c.Sprite("fence_post", Layout.Fence.X + Layout.FenceBays * 12, Layout.Fence.Y);
            _c.Rect(Layout.Fence.X + 1, Layout.Fence.Y + 10, Layout.FenceBays * 12 + 2, 1, Palette.Shadow);
            foreach (var fl in Layout.Flowers) _c.SpriteSway(fl.Y % 2 == 0 ? "flower_yellow" : "flower_pink", fl.X, fl.Y, Sway("flower", fl.X, fl.Y));
        }

        /// <summary>Which fence variant a bay uses: the sagging rail (c) is never doubled up, and the plain
        /// bay fills in between so the run reads as one fence rather than a pattern.</summary>
        static string FenceBay(int i)
        {
            switch (Hash(i, 41) % 7)
            {
                case 0: return "fence_b";
                case 1: return "fence_d";
                case 2: return (i > 0 && Hash(i - 1, 41) % 7 == 2) ? "fence" : "fence_c";
                case 3: return "fence_b";
                default: return "fence";
            }
        }

        /// <summary>How far (in pixels) the top of a plant leans right now: gusts times a per-plant ripple, so
        /// the meadow moves as a wave rather than in lockstep. Rocks do not sway.</summary>
        float Sway(string sprite, int x, int y)
        {
            if (sprite == "rock") return 0f;
            float max = sprite == "tree_big" ? 2.5f : sprite == "tree" ? 2f : 1.2f;
            float phase = (Hash(x, y, 5) % 628) / 100f - x * 0.02f;
            float ripple = 0.55f + 0.45f * (float)Math.Sin(_time * (1.7 + 2.3 * _windy) + phase);
            // On windy days long gusts roll across the meadow from the left on top of the everyday breeze.
            float gust = 0.5f + 0.5f * (float)Math.Sin(_time * 1.1 - x * 0.012);
            float strength = Wind * (1f + 0.5f * _windy) + _windy * (0.8f + 0.9f * gust);
            return strength * ripple * max;
        }

        // ---- The tower -------------------------------------------------------------------------

        void DrawTower(double f, WizardActor wizard)
        {
            var t = Layout.Tower;
            // Ground shadow first so everything sits on top of it.
            _c.Rect(t.X - 2, t.Bottom, t.Width + 10, 3, Palette.Shadow);
            _c.Rect(t.Right, t.Bottom - 6, 8, 6, Palette.Shadow);

            // Round wall: light on the left, stone in the middle, shade on the right.
            _c.Rect(t.X, t.Y, 7, t.Height, Palette.LightStone);
            _c.Rect(t.X + 7, t.Y, t.Width - 18, t.Height, Palette.Stone);
            _c.Rect(t.Right - 11, t.Y, 11, t.Height, Palette.DarkStone);
            // Courses of stone: staggered vertical joints and a mortar line every six rows.
            for (int y = t.Y + 5; y < t.Bottom; y += 6)
            {
                _c.Rect(t.X + 1, y, t.Width - 2, 1, Palette.DarkStone * 0.6f);
                int off = ((y - t.Y) / 6) % 2 == 0 ? 0 : 6;
                for (int x = t.X + 4 + off; x < t.Right - 1; x += 12) _c.Rect(x, y - 5, 1, 5, Palette.DarkStone * 0.6f);
            }
            _c.Rect(t.X, t.Y, 1, t.Height, Palette.Outline);
            _c.Rect(t.Right - 1, t.Y, 1, t.Height, Palette.Outline);
            _c.Rect(t.X, t.Bottom - 1, t.Width, 1, Palette.Outline);

            _c.Sprite("roof", Layout.Roof.X, Layout.Roof.Y);

            bool lit = WindowsLit || f > 0.8 || f < 0.06;
            foreach (var w in Layout.Windows)
            {
                _c.Sprite(lit ? "window_lit" : "window", w.X, w.Y);
                if (lit)
                {
                    float day = WindowsLit ? 0.4f : 0f;
                    Light(w.X + 4, w.Y + 5, 14, 12, Canvas.Tone(LampLight, 0.55f), day);
                    LightRect(w.X + 2, w.Y + 2, 4, 6, Canvas.Tone(LampLight, 0.25f), day);
                }
                _c.Rect(w.X + 1, w.Y + 10, 7, 1, Palette.Shadow);
            }
            if (Snoring)
            {
                // Three little z's drifting up and away from the top window, fading as they go.
                var w = Layout.Windows[0];
                for (int i = 0; i < 3; i++)
                {
                    float rise = (_time * 9 + i * 8) % 24;
                    _c.Text("z", w.X + 10 + (int)(rise / 3), w.Y - 2 - (int)rise, Palette.Cream * (1f - rise / 24f));
                }
            }
            // Doorstep and the door itself.
            _c.Rect(Layout.Door.X - 1, t.Bottom, 16, 2, Palette.LightStone);
            _c.Rect(Layout.Door.X - 1, t.Bottom + 2, 16, 1, Palette.Outline);
            if (DoorOpen)
            {
                // A warm hallway behind the open door: lamplit near the top, dark toward the floor.
                var inside = Layout.DoorInterior;
                _c.Rect(inside, new Color(58, 36, 30));
                _c.Rect(inside.X, inside.Y, inside.Width, 6, new Color(120, 70, 36));
                _c.Rect(inside.X, inside.Y + 6, inside.Width, 6, new Color(90, 52, 32));
                _c.Rect(inside.X, inside.Bottom - 2, inside.Width, 2, Palette.DarkBrown);
                if (wizard.InDoorway) wizard.Draw(_c);
                StationSprite(Hot(StationKind.Door), "door_open", Layout.Door.X - 3, Layout.Door.Y);
                Light(inside.X + inside.Width / 2, inside.Bottom, 16, 8, Canvas.Tone(LampLight, 0.5f), 0.3f);
            }
            else
            {
                StationSprite(Hot(StationKind.Door), "door", Layout.Door.X, Layout.Door.Y);
                if (lit)
                {
                    LightRect(Layout.Door.X + 3, Layout.Door.Y + 6, 8, 1, Canvas.Tone(LampLight, 0.6f));
                    Light(Layout.Door.X + 7, Layout.Door.Y + 7, 8, 4, Canvas.Tone(LampLight, 0.3f));
                }
            }

            // Ivy climbing the left edge and a creeper on the right.
            for (int y = t.Y + 36; y < t.Bottom - 4; y += 3)
            {
                int x = t.X + 1 + Hash(y, 31) % 4;
                _c.Rect(x, y, 2, 2, Palette.DarkGreen);
                _c.Rect(x, y, 1, 1, Palette.Green);
                if (Hash(y, 32) % 3 == 0) { _c.Rect(x + 2, y + 1, 2, 2, Palette.DarkGreen); _c.Rect(x + 2, y + 1, 1, 1, Palette.Green); }
            }
            for (int y = t.Y + 12; y < t.Y + 70; y += 3)
            {
                int x = t.Right - 6 + Hash(y, 33) % 3;
                _c.Rect(x, y, 2, 2, Palette.DarkGreen);
                if (Hash(y, 34) % 4 == 0) _c.Rect(x, y, 1, 1, Palette.Green);
            }
        }

        // ---- The garden ------------------------------------------------------------------------

        void DrawGarden(GameState s, Station hover)
        {
            for (int i = 0; i < Garden.MaxPlots; i++)
            {
                var p = Layout.PlotPos(i);
                bool unlocked = i < s.UnlockedPlots;
                var plot = s.Garden.Plots[i];
                bool wet = plot.Plant != null && plot.Plant.WateredToday;
                bool hot = unlocked && Hot(StationKind.Plot, i);
                StationSprite(hot, !unlocked ? "plot_locked" : wet ? "plot_wet" : "plot", p.X, p.Y);
                if (!unlocked)
                {
                    _c.Rect(new Rectangle(p.X, p.Y - 10, 24, 26), Palette.Outline * 0.45f);
                    _c.Sprite("ic_lock", p.X + 8, p.Y - 6);
                }
                if (plot.Plant != null)
                {
                    var plant = plot.Plant;
                    string sprite = plant.Stage switch
                    {
                        PlantStage.Seed => "seed",
                        PlantStage.Sprout => "sprout",
                        PlantStage.Budding => "bud",
                        _ => ItemArt.MatureSprite(plant.Species),
                    };
                    if (plant.Stage != PlantStage.Seed) _c.GroundShadow(p.X + 12, plant.Stage == PlantStage.Sprout ? 4 : 8, p.Y + 4, 0.5f);
                    StationSprite(hot, sprite, p.X + 4, p.Y - 10, Color.White, plant.Stage == PlantStage.Seed ? 0f : Sway("plant", p.X, i));
                    if (plant.Stage == PlantStage.Mature && plant.Species == PepperSpecies.Ghost)
                    {
                        int bob = (int)(Math.Sin(_time * 3 + i) * 2);
                        StationSprite(hot, "ghost", p.X + 7, p.Y - 14 + bob);
                        // The ghost hovers: its shadow shrinks as it rises, and after dark it gives off a cold glow.
                        _c.GroundShadow(p.X + 12, 6 - bob, p.Y + 2, 0.6f);
                        float pulse = 0.5f + 0.2f * (float)Math.Sin(_time * 3 + i);
                        Light(p.X + 12, p.Y - 9 + bob, 12, 10, Canvas.Tone(GhostLight, pulse), 0.15f);
                    }
                    if (plant.IsMature) Marker(p.X + 19, p.Y - 12);
                    else if (plant.PepTalkedToday)
                    {
                        _c.TextShadow("!", p.X + 20, p.Y - 12, Palette.Pink);
                    }
                }
            }
        }

        // ---- Stations ----------------------------------------------------------------------------

        void DrawStations(GameState s, Station hover)
        {
            // Well and bucket.
            _c.GroundShadow(Layout.Well.X + 10, 20, Layout.Well.Y + 24);
            StationSprite(Hot(StationKind.Well), "well", Layout.Well.X, Layout.Well.Y);
            _c.Rect(Layout.Bucket.X + 1, Layout.Bucket.Y + 8, 7, 1, Palette.Shadow);
            StationSprite(Hot(StationKind.Well), "bucket", Layout.Bucket.X, Layout.Bucket.Y);
            if (s.Garden.BucketWater > 0)
            {
                // Water in the bucket, with a glint that slides across it.
                _c.Rect(Layout.Bucket.X + 2, Layout.Bucket.Y + 2, 4, 1, Palette.Sky);
                _c.Rect(Layout.Bucket.X + 2 + (int)(_time * 2) % 4, Layout.Bucket.Y + 2, 1, 1, Palette.PaleBlue);
            }

            // Notice board with quota ticks.
            _c.GroundShadow(Layout.Board.X + 10, 16, Layout.Board.Y + 22, 0.8f);
            StationSprite(Hot(StationKind.Board), "board", Layout.Board.X, Layout.Board.Y);
            if (s.Quota != null)
                for (int i = 0; i < s.Quota.Lines.Count; i++)
                    _c.Rect(Layout.Board.X + 15, Layout.Board.Y + 4 + i * 2, 2, 1, s.Quota.Lines[i].IsMet ? Palette.LightGreen : Palette.Red);

            // Merchant cart (slides in at dawn).
            int cartX = (int)CartX;
            _c.Rect(cartX + 3, Layout.CartPark.Y + 20, 30, 3, Palette.Shadow);
            StationSprite(Hot(StationKind.Market), "cart", cartX, Layout.CartPark.Y);
            if (Math.Abs(CartX - Layout.CartPark.X) < 1)
            {
                _c.Rect(Layout.Merchant.X + 2, Layout.Merchant.Y + 16, 8, 2, Palette.Shadow);
                StationSprite(Hot(StationKind.Market), "merchant", Layout.Merchant.X, Layout.Merchant.Y);
            }

            // Shipping crate. Late in the day an empty crate with sauces still in the pantry gets a nudge.
            _c.GroundShadow(Layout.Crate.X + 9, 18, Layout.Crate.Y + 12);
            StationSprite(Hot(StationKind.Crate), s.Crate.Sauces.Count > 0 ? "crate_full" : "crate", Layout.Crate.X, Layout.Crate.Y);
            if (s.Crate.Sauces.Count > 0)
                _c.TextShadow(s.Crate.Sauces.Count.ToString(), Layout.Crate.X + 20, Layout.Crate.Y + 2, Palette.White);
            else if (s.Clock.Minute >= 18 * 60 && s.Inventory.Sauces.Count > 0)
                Marker(Layout.Crate.X + 9, Layout.Crate.Y - 2);

            // Cauldron standing over a log fire: the logs sit on the ground under its feet and a
            // flickering pool of firelight spreads out around them.
            var cp = Layout.Cauldron;
            float flicker = 0.7f + 0.3f * (float)Math.Sin(_time * 11) * (float)Math.Cos(_time * 7);
            _c.Glow(cp.X + 12, cp.Y + 20, 18, 4, Palette.Glow * (0.5f * flicker));
            _c.Rect(cp.X + 1, cp.Y + 20, 22, 2, Palette.Shadow);
            // Firelight: a wide pool on the grass and a smaller one up the belly of the pot.
            Light(cp.X + 12, cp.Y + 20, 30, 9, Canvas.Tone(FireLight, 0.55f * flicker), 0.3f);
            Light(cp.X + 12, cp.Y + 14, 12, 7, Canvas.Tone(FireLight, 0.35f * flicker), 0.2f);
            string fire = (int)(_time * 6) % 2 == 0 ? "fire0" : "fire1";
            StationSprite(Hot(StationKind.Cauldron), fire, cp.X - 2, cp.Y + 11);
            StationSprite(Hot(StationKind.Cauldron), "cauldron", cp.X, cp.Y);
            string bubbles = "bubbles" + ((int)(_time * 3) % 3);
            var brew = Color.Lerp(Palette.LightGreen, _brewColor, Math.Clamp(_brewFade / BrewFadeSeconds, 0f, 1f));
            StationSprite(Hot(StationKind.Cauldron), bubbles, cp.X, cp.Y - 2, brew);
            // The iron catches the fire along its lower rim, and the brew throws back a shifting highlight.
            float lick = 0.5f + 0.5f * (float)Math.Sin(_time * 9 + 1);
            LightRect(cp.X + 4, cp.Y + 16, 16, 1, Canvas.Tone(FireLight, 0.25f + 0.2f * lick), 0.5f);
            LightRect(cp.X + 6, cp.Y + 17, 12, 1, Canvas.Tone(FireLight, 0.15f + 0.15f * (1f - lick)), 0.5f);
            int gleam = cp.X + 6 + (int)(6 + 5 * Math.Sin(_time * 1.3));
            LightRect(gleam, cp.Y + 3, 3, 1, Canvas.Tone(Palette.White, 0.25f), 0.6f);

            // Jar shelf on the tower wall.
            bool shelfHot = Hot(StationKind.Shelf);
            StationSprite(shelfHot, "shelf", Layout.Shelf.X, Layout.Shelf.Y);
            for (int i = 0; i < FermentShelf.MaxJars; i++)
            {
                int jx = Layout.Shelf.X + 1 + i * 11, jy = Layout.Shelf.Y + 2;
                if (i >= s.UnlockedJars) { StationSprite(shelfHot, "jar_lock", jx, jy); continue; }
                var jar = s.Shelf.Jars[i];
                if (!jar.IsEmpty)
                {
                    var color = ItemArt.SpeciesColor(jar.Species.Value);
                    StationSprite(shelfHot, "jar_fill", jx, jy, jar.IsReady ? color : Color.Lerp(color, Palette.Grey, 0.5f));
                }
                StationSprite(shelfHot, "jar", jx, jy);
                if (jar.IsReady)
                {
                    Marker(jx + 5, jy - 1);
                    float shimmer = 0.3f + 0.15f * (float)Math.Sin(_time * 2.5 + i);
                    Light(jx + 5, jy + 8, 8, 7, Canvas.Tone(ItemArt.SpeciesColor(jar.Species.Value), shimmer), 0.1f);
                }
            }

            // Mortar on a stump, pantry chest.
            _c.Rect(Layout.Stump.X + 2, Layout.Stump.Y + 9, 16, 2, Palette.Shadow);
            StationSprite(Hot(StationKind.Mortar), "stump", Layout.Stump.X, Layout.Stump.Y);
            StationSprite(Hot(StationKind.Mortar), "mortar", Layout.Mortar.X, Layout.Mortar.Y);
            _c.GroundShadow(Layout.Pantry.X + 9, 18, Layout.Pantry.Y + 12);
            StationSprite(Hot(StationKind.Pantry), "pantry", Layout.Pantry.X, Layout.Pantry.Y);
            if (s.Inventory.Sauces.Count > 0) Marker(Layout.Pantry.X + 9, Layout.Pantry.Y - 2);
        }
    }
}
