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
        public static Color SkyTop(double f) => Blend(f, Palette.Navy, new Color(240, 150, 110), Palette.Sky, Palette.Sky, new Color(230, 120, 90), Palette.Navy);
        public static Color SkyBottom(double f) => Blend(f, new Color(60, 50, 90), new Color(250, 200, 140), new Color(190, 225, 245), new Color(190, 225, 245), new Color(250, 170, 110), new Color(60, 50, 90));

        /// <summary>Overlay drawn over the whole scene; alpha is folded into the colour.</summary>
        public static Color Tint(double f)
        {
            if (f < 0.05) return Palette.Navy * (float)(0.35 * (1 - f / 0.05));
            if (f < 0.75) return Color.Transparent;
            if (f < 0.9) return new Color(200, 90, 40) * (float)(0.22 * (f - 0.75) / 0.15);
            return Blend01(new Color(200, 90, 40) * 0.22f, Palette.Navy * 0.5f, (f - 0.9) / 0.1);
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
        public static Color Haze(Color c, double f) => Color.Lerp(c, Palette.Navy, 0.7f * Darkness(f));

        public static Point SunPos(double f)
        {
            double a = Math.PI * (1 - f);
            return new Point((int)(192 + 170 * Math.Cos(a)) - 5, (int)(Layout.Horizon - 2 - 62 * Math.Sin(a)) - 5);
        }

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

        // Scenery outside the 384x216 design box, generated once per window size.
        struct Prop { public string Sprite; public int X, Y; public int Base; }
        readonly List<Prop> _props = new List<Prop>();
        Rectangle _propsFor;

        static readonly Rectangle Yard = new Rectangle(0, 0, Camera.Width, Camera.Height);

        public SceneRenderer(Canvas canvas) { _c = canvas; }

        public void Update(float dt) { _time += dt; }

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

        public void Draw(GameState s, WizardActor wizard, Particles particles, Crowd crowd, Station hover)
        {
            double f = s.Clock.DayFraction;
            if (_propsFor != Camera.View) BuildProps();

            DrawSky(f);
            DrawHills(f);
            DrawTreeLine(f);
            DrawTown(f);
            DrawGround();
            DrawProps(f);
            DrawTower(f);
            DrawGarden(s, hover);
            DrawStations(s, hover);
            crowd.Draw(_c);
            wizard.Draw(_c);
            particles.Draw(_c);
            var tint = DayNight.Tint(f);
            if (tint.A > 0) _c.Rect(Camera.View, tint);
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
            if (dark > 0.3f)
            {
                float a = (dark - 0.3f) / 0.7f;
                int rows = Layout.Horizon - 30 - view.Top;
                for (int i = 0; i < 140; i++)
                {
                    int sx = view.Left + Hash(i, 1) % view.Width, sy = view.Top + Hash(i, 2) % Math.Max(1, rows);
                    float tw = 0.45f + 0.55f * (float)Math.Sin(_time * 2 + i);
                    _c.Rect(sx, sy, 1, 1, Palette.White * (tw * a));
                    if (i % 9 == 0) { _c.Rect(sx - 1, sy, 3, 1, Palette.White * (0.25f * a)); _c.Rect(sx, sy - 1, 1, 3, Palette.White * (0.25f * a)); }
                }
            }
            if (f > 0.02 && f < 0.98)
            {
                var sun = DayNight.SunPos(f);
                _c.Rect(sun.X - 2, sun.Y + 1, 14, 8, Palette.LightYellow * 0.18f);
                _c.Rect(sun.X + 1, sun.Y - 2, 8, 14, Palette.LightYellow * 0.18f);
                _c.Sprite("sun", sun.X, sun.Y);
            }
            if (f > 0.85 || f < 0.05)
            {
                var m = DayNight.MoonPos(f < 0.05 ? 1 : f);
                _c.Sprite("moon", m.X, m.Y);
            }
            // Clouds drift at a few heights; the higher ones only show when the window is tall.
            int[] dy = { -86, -70, -56, -40, -30 };
            int[] speed = { 3, 5, 4, 6, 3 };
            string[] kind = { "cloud_big", "cloud", "cloud_big", "cloud", "cloud" };
            float[] alpha = { 0.6f, 0.9f, 0.75f, 0.85f, 0.7f };
            var cloudTint = Color.Lerp(Color.White, Palette.Navy, 0.5f * dark);
            for (int i = 0; i < dy.Length; i++)
            {
                int y = Layout.Horizon + dy[i];
                if (y < view.Top - 12) continue;
                int w = _c.Size(kind[i]).X;
                int span = view.Width + w + 40;
                int x = view.Left - w + (int)(_time * speed[i] + i * 137) % span;
                _c.Sprite(kind[i], x, y, cloudTint * alpha[i]);
            }
        }

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
            var wall = DayNight.Haze(Palette.LightStone, f);
            var wallShade = DayNight.Haze(Palette.Stone, f);
            var roof = DayNight.Haze(Palette.DarkRed, f);
            var roofLit = DayNight.Haze(Palette.Red, f);
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
                // Gable roof.
                int half = w / 2 + 1;
                for (int r = 0; r < 4; r++)
                {
                    int rw = (half * (r + 1)) / 2;
                    int cx = x + w / 2;
                    _c.Rect(cx - rw - 1, H - hgt - 4 + r, rw * 2 + 3, 1, outline);
                    _c.Rect(cx - rw, H - hgt - 4 + r, rw * 2 + 1, 1, r == 0 ? roofLit : roof);
                }
                _c.Rect(x - 1, H - hgt - 1, w + 2, 1, outline);
                // Door and a window.
                _c.Rect(x + w - 5, H - 5, 3, 5, DayNight.Haze(Palette.DarkBrown, f));
                _c.Rect(x + 3, H - hgt + 3, 2, 2, dark ? Palette.Yellow : DayNight.Haze(Palette.Navy, f));
                if (dark) _c.Rect(x + 2, H - hgt + 2, 4, 4, Palette.Glow);
                // The church spire.
                if (i == 3)
                {
                    int sx = x + w / 2;
                    _c.Rect(sx - 2, H - hgt - 16, 5, 13, wall);
                    _c.Rect(sx + 1, H - hgt - 16, 2, 13, wallShade);
                    _c.Rect(sx - 3, H - hgt - 16, 1, 13, outline);
                    _c.Rect(sx + 3, H - hgt - 16, 1, 13, outline);
                    for (int r = 0; r < 4; r++) _c.Rect(sx - r + 1, H - hgt - 20 + r, 2 * r - 1 + 2, 1, r == 0 ? outline : roof);
                    _c.Rect(sx, H - hgt - 23, 1, 3, outline);
                    _c.Rect(sx - 1, H - hgt - 22, 3, 1, outline);
                }
            }
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
                        _c.Rect(x, y, 1, 2, Palette.DarkGrass);
                        _c.Rect(x + 2, y + 1, 1, 1, Palette.DarkGrass);
                        _c.Rect(x + 1, y - 1, 1, 1, Palette.DarkGrass);
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
            foreach (var p in _props) _c.Sprite(p.Sprite, p.X, p.Y);
            foreach (var t in Layout.Trees) _c.Sprite("tree", t.X, t.Y);
            foreach (var b in Layout.Bushes) _c.Sprite("bush", b.X, b.Y);
            for (int i = 0; i < Layout.FenceBays; i++) _c.Sprite("fence", Layout.Fence.X + i * 12, Layout.Fence.Y);
            _c.Rect(Layout.Fence.X + 1, Layout.Fence.Y + 10, Layout.FenceBays * 12, 1, Palette.Shadow);
            foreach (var fl in Layout.Flowers) _c.Sprite(fl.Y % 2 == 0 ? "flower_yellow" : "flower_pink", fl.X, fl.Y);
        }

        // ---- The tower -------------------------------------------------------------------------

        void DrawTower(double f)
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

            bool lit = f > 0.8 || f < 0.06;
            foreach (var w in Layout.Windows)
            {
                if (lit) _c.Rect(w.X - 2, w.Y - 2, 12, 13, Palette.Glow);
                _c.Sprite(lit ? "window_lit" : "window", w.X, w.Y);
                _c.Rect(w.X + 1, w.Y + 10, 7, 1, Palette.Shadow);
            }
            // Doorstep and the door itself.
            _c.Rect(Layout.Door.X - 1, t.Bottom, 16, 2, Palette.LightStone);
            _c.Rect(Layout.Door.X - 1, t.Bottom + 2, 16, 1, Palette.Outline);
            _c.Sprite("door", Layout.Door.X, Layout.Door.Y);
            if (lit) _c.Rect(Layout.Door.X + 3, Layout.Door.Y + 6, 8, 1, Palette.Glow);

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
                _c.Sprite(!unlocked ? "plot_locked" : wet ? "plot_wet" : "plot", p.X, p.Y);
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
                    _c.Sprite(sprite, p.X + 4, p.Y - 10);
                    if (plant.Stage == PlantStage.Mature && plant.Species == PepperSpecies.Ghost)
                    {
                        int bob = (int)(Math.Sin(_time * 3 + i) * 2);
                        _c.Sprite("ghost", p.X + 7, p.Y - 14 + bob);
                    }
                    if (plant.IsMature)
                    {
                        int blink = (int)(_time * 3) % 2;
                        if (blink == 0) _c.Sprite("ic_check", p.X + 16, p.Y - 14);
                    }
                    else if (plant.PepTalkedToday)
                    {
                        _c.TextShadow("!", p.X + 20, p.Y - 12, Palette.Pink);
                    }
                }
                if (hover != null && hover.Kind == StationKind.Plot && hover.Index == i && unlocked)
                    _c.Border(new Rectangle(p.X, p.Y + 1, 24, 14), Palette.Yellow);
            }
        }

        // ---- Stations ----------------------------------------------------------------------------

        void DrawStations(GameState s, Station hover)
        {
            // Well and bucket.
            _c.Sprite("well", Layout.Well.X, Layout.Well.Y);
            _c.Rect(Layout.Bucket.X + 1, Layout.Bucket.Y + 8, 7, 1, Palette.Shadow);
            _c.Sprite("bucket", Layout.Bucket.X, Layout.Bucket.Y);
            if (s.Garden.BucketWater > 0)
                _c.Rect(Layout.Bucket.X + 2, Layout.Bucket.Y + 2, 4, 1, Palette.Sky);

            // Notice board with quota ticks.
            _c.Sprite("board", Layout.Board.X, Layout.Board.Y);
            if (s.Quota != null)
                for (int i = 0; i < s.Quota.Lines.Count; i++)
                    _c.Rect(Layout.Board.X + 15, Layout.Board.Y + 4 + i * 2, 2, 1, s.Quota.Lines[i].IsMet ? Palette.LightGreen : Palette.Red);

            // Merchant cart (slides in at dawn).
            int cartX = (int)CartX;
            _c.Rect(cartX + 3, Layout.CartPark.Y + 20, 30, 3, Palette.Shadow);
            _c.Sprite("cart", cartX, Layout.CartPark.Y);
            if (Math.Abs(CartX - Layout.CartPark.X) < 1)
            {
                _c.Rect(Layout.Merchant.X + 2, Layout.Merchant.Y + 16, 8, 2, Palette.Shadow);
                _c.Sprite("merchant", Layout.Merchant.X, Layout.Merchant.Y);
            }

            // Shipping crate.
            _c.Sprite(s.Crate.Sauces.Count > 0 ? "crate_full" : "crate", Layout.Crate.X, Layout.Crate.Y);
            if (s.Crate.Sauces.Count > 0)
                _c.TextShadow(s.Crate.Sauces.Count.ToString(), Layout.Crate.X + 20, Layout.Crate.Y + 2, Palette.White);

            // Cauldron over a fire, with a flickering glow on the ground.
            var cp = Layout.Cauldron;
            float flicker = 0.7f + 0.3f * (float)Math.Sin(_time * 11) * (float)Math.Cos(_time * 7);
            _c.Rect(cp.X - 3, cp.Y + 17, 30, 7, Palette.Glow * flicker);
            _c.Rect(cp.X + 1, cp.Y + 22, 22, 2, Palette.Shadow);
            string fire = (int)(_time * 6) % 2 == 0 ? "fire0" : "fire1";
            _c.Sprite(fire, cp.X, cp.Y + 16);
            _c.Sprite("cauldron", cp.X, cp.Y);
            string bubbles = "bubbles" + ((int)(_time * 3) % 3);
            _c.Sprite(bubbles, cp.X, cp.Y - 2, Palette.LightGreen);

            // Jar shelf on the tower wall.
            _c.Sprite("shelf", Layout.Shelf.X, Layout.Shelf.Y);
            for (int i = 0; i < FermentShelf.MaxJars; i++)
            {
                int jx = Layout.Shelf.X + 1 + i * 11, jy = Layout.Shelf.Y + 2;
                if (i >= s.UnlockedJars) { _c.Sprite("jar_lock", jx, jy); continue; }
                var jar = s.Shelf.Jars[i];
                if (!jar.IsEmpty)
                {
                    var color = ItemArt.SpeciesColor(jar.Species.Value);
                    _c.Sprite("jar_fill", jx, jy, jar.IsReady ? color : Color.Lerp(color, Palette.Grey, 0.5f));
                }
                _c.Sprite("jar", jx, jy);
                if (!jar.IsEmpty && jar.IsReady && (int)(_time * 2) % 2 == 0) _c.Rect(jx + 4, jy - 3, 2, 2, jar.IsAged ? Palette.Pink : Palette.White);
            }

            // Mortar on a stump, pantry chest.
            _c.Rect(Layout.Stump.X + 2, Layout.Stump.Y + 9, 16, 2, Palette.Shadow);
            _c.Sprite("stump", Layout.Stump.X, Layout.Stump.Y);
            _c.Sprite("mortar", Layout.Mortar.X, Layout.Mortar.Y);
            _c.Sprite("pantry", Layout.Pantry.X, Layout.Pantry.Y);

            if (hover != null && hover.Kind != StationKind.Plot)
                _c.Border(hover.Bounds, Palette.Yellow);
        }
    }
}
