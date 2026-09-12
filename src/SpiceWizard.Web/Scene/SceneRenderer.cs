using System;
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

    /// <summary>Draws the yard, the tower, the garden and everyone in it.</summary>
    public sealed class SceneRenderer
    {
        readonly Canvas _c;
        float _time;
        public float CartX = Layout.CartPark.X;

        public SceneRenderer(Canvas canvas) { _c = canvas; }

        public void Update(float dt) { _time += dt; }

        public void Draw(GameState s, WizardActor wizard, Particles particles, Crowd crowd, Station hover)
        {
            double f = s.Clock.DayFraction;
            DrawSky(f);
            DrawTown(f);
            DrawGround();
            DrawTower(f);
            DrawGarden(s, hover);
            DrawStations(s, hover);
            crowd.Draw(_c);
            wizard.Draw(_c);
            particles.Draw(_c);
            var tint = DayNight.Tint(f);
            if (tint.A > 0) _c.Rect(0, 0, Camera.Width, Camera.Height, tint);
        }

        void DrawSky(double f)
        {
            var top = DayNight.SkyTop(f);
            var bottom = DayNight.SkyBottom(f);
            const int bands = 8;
            int h = Layout.Horizon;
            for (int i = 0; i < bands; i++)
            {
                int y0 = i * h / bands, y1 = (i + 1) * h / bands;
                _c.Rect(0, y0, Camera.Width, y1 - y0, Color.Lerp(top, bottom, i / (float)(bands - 1)));
            }
            if (DayNight.IsDark(f))
            {
                for (int i = 0; i < 40; i++)
                {
                    int sx = (i * 97 + 13) % Camera.Width, sy = (i * 53 + 7) % (Layout.Horizon - 30);
                    float tw = 0.5f + 0.5f * (float)Math.Sin(_time * 2 + i);
                    _c.Rect(sx, sy, 1, 1, Palette.White * tw);
                }
            }
            if (f > 0.02 && f < 0.98)
            {
                var sun = DayNight.SunPos(f);
                _c.Sprite("sun", sun.X, sun.Y);
            }
            if (f > 0.85 || f < 0.05)
            {
                var m = DayNight.MoonPos(f < 0.05 ? 1 : f);
                _c.Sprite("moon", m.X, m.Y);
            }
            int cloudX = (int)(_time * 4) % (Camera.Width + 60) - 30;
            _c.Sprite("cloud", cloudX, 28, Color.White * 0.9f);
            _c.Sprite("cloud", (cloudX + 190) % (Camera.Width + 60) - 30, 46, Color.White * 0.7f);
        }

        void DrawTown(double f)
        {
            // Distant town on the horizon: a row of little houses and a church spire.
            bool dark = DayNight.IsDark(f);
            var wall = dark ? Palette.DarkPurple : Palette.DarkStone;
            var roof = dark ? Palette.Navy : Palette.DarkRed;
            int[] xs = { 4, 22, 38, 60, 78, 100, 118, 136 };
            int[] hs = { 12, 16, 10, 22, 14, 12, 18, 10 };
            for (int i = 0; i < xs.Length; i++)
            {
                int x = xs[i], hgt = hs[i], w = 14;
                _c.Rect(x, Layout.Horizon - hgt, w, hgt, wall);
                for (int r = 0; r < 4; r++) _c.Rect(x + 7 - 2 * r, Layout.Horizon - hgt - 4 + r, 2 + 4 * r, 1, roof);
                if (dark) _c.Rect(x + 4, Layout.Horizon - hgt + 4, 2, 2, Palette.Yellow);
                if (i == 3) { _c.Rect(x + 6, Layout.Horizon - hgt - 14, 2, 14, wall); _c.Rect(x + 5, Layout.Horizon - hgt - 16, 4, 2, roof); }
            }
        }

        void DrawGround()
        {
            _c.Rect(0, Layout.Horizon, Camera.Width, Camera.Height - Layout.Horizon, Palette.Grass);
            _c.Rect(0, Layout.Horizon, Camera.Width, 2, Palette.DarkGrass);
            // Road from the town to the yard.
            _c.Rect(0, Layout.RoadTop, 300, Layout.RoadBottom - Layout.RoadTop, Palette.Road);
            for (int x = 0; x < 300; x += 9) _c.Rect(x, Layout.RoadTop + 6, 4, 1, Palette.Tan);
            // Tufts of grass.
            for (int i = 0; i < 40; i++)
            {
                int x = (i * 71 + 5) % Camera.Width, y = Layout.RoadBottom + 4 + (i * 37) % (Camera.Height - Layout.RoadBottom - 8);
                _c.Rect(x, y, 1, 2, Palette.DarkGrass);
                _c.Rect(x + 2, y + 1, 1, 1, Palette.DarkGrass);
            }
            foreach (var t in Layout.Trees) _c.Sprite("tree", t.X, t.Y);
            foreach (var b in Layout.Bushes) _c.Sprite("bush", b.X, b.Y);
        }

        void DrawTower(double f)
        {
            var t = Layout.Tower;
            _c.Rect(t, Palette.Stone);
            for (int y = t.Y; y < t.Bottom; y += 6)
            {
                int off = ((y - t.Y) / 6) % 2 == 0 ? 0 : 6;
                for (int x = t.X + off; x < t.Right; x += 12) _c.Rect(x, y, 1, 6, Palette.DarkStone);
                _c.Rect(t.X, y, t.Width, 1, Palette.DarkStone);
            }
            _c.Rect(t.X, t.Y, 1, t.Height, Palette.DarkStone);
            _c.Rect(t.Right - 1, t.Y, 1, t.Height, Palette.DarkStone);
            _c.Sprite("roof", Layout.Roof.X, Layout.Roof.Y);
            bool lit = f > 0.8 || f < 0.06;
            foreach (var w in Layout.Windows) _c.Sprite(lit ? "window_lit" : "window", w.X, w.Y);
            _c.Sprite("door", Layout.Door.X, Layout.Door.Y);
            // Ivy.
            _c.Rect(t.X + 2, t.Y + 40, 2, 60, Palette.DarkGreen);
            _c.Rect(t.X + 4, t.Y + 52, 2, 20, Palette.Green);
            _c.Rect(t.Right - 5, t.Y + 20, 2, 40, Palette.DarkGreen);
        }

        void DrawGarden(GameState s, Station hover)
        {
            for (int i = 0; i < Garden.MaxPlots; i++)
            {
                var p = Layout.PlotPos(i);
                bool unlocked = i < s.UnlockedPlots;
                var plot = s.Garden.Plots[i];
                bool wet = plot.Plant != null && plot.Plant.WateredToday;
                _c.Sprite(!unlocked ? "plot_locked" : wet ? "plot_wet" : "plot", p.X, p.Y);
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
                        _c.Text("!", p.X + 20, p.Y - 12, Palette.Pink);
                    }
                }
                if (hover != null && hover.Kind == StationKind.Plot && hover.Index == i && unlocked)
                    _c.Border(new Rectangle(p.X, p.Y, 24, 16), Palette.Yellow);
            }
        }

        void DrawStations(GameState s, Station hover)
        {
            // Well and bucket.
            _c.Sprite("well", Layout.Well.X, Layout.Well.Y);
            _c.Sprite("bucket", Layout.Bucket.X, Layout.Bucket.Y);
            if (s.Garden.BucketWater > 0)
                _c.Rect(Layout.Bucket.X + 1, Layout.Bucket.Y + 4, 6, 1, Palette.Sky);

            // Notice board with quota ticks.
            _c.Sprite("board", Layout.Board.X, Layout.Board.Y);
            if (s.Quota != null)
                for (int i = 0; i < s.Quota.Lines.Count; i++)
                    _c.Rect(Layout.Board.X + 15, Layout.Board.Y + 6 + i * 2, 2, 1, s.Quota.Lines[i].IsMet ? Palette.LightGreen : Palette.Red);

            // Merchant cart (slides in at dawn).
            int cartX = (int)CartX;
            _c.Sprite("cart", cartX, Layout.CartPark.Y);
            if (Math.Abs(CartX - Layout.CartPark.X) < 1) _c.Sprite("merchant", Layout.Merchant.X, Layout.Merchant.Y);

            // Shipping crate.
            _c.Sprite(s.Crate.Sauces.Count > 0 ? "crate_full" : "crate", Layout.Crate.X, Layout.Crate.Y);
            if (s.Crate.Sauces.Count > 0)
                _c.Text(s.Crate.Sauces.Count.ToString(), Layout.Crate.X + 20, Layout.Crate.Y + 2, Palette.White);

            // Cauldron over a fire.
            string fire = (int)(_time * 6) % 2 == 0 ? "fire0" : "fire1";
            _c.Sprite(fire, Layout.Cauldron.X, Layout.Cauldron.Y + 16);
            _c.Sprite("cauldron", Layout.Cauldron.X, Layout.Cauldron.Y);
            _c.Rect(Layout.Cauldron.X + 4, Layout.Cauldron.Y + 6, 16, 2, Palette.Green);
            string bubbles = "bubbles" + ((int)(_time * 3) % 3);
            _c.Sprite(bubbles, Layout.Cauldron.X, Layout.Cauldron.Y - 2, Palette.LightGreen);

            // Jar shelf on the tower wall.
            _c.Sprite("shelf", Layout.Shelf.X, Layout.Shelf.Y);
            for (int i = 0; i < FermentShelf.MaxJars; i++)
            {
                int jx = Layout.Shelf.X + 1 + i * 11, jy = Layout.Shelf.Y + 2;
                if (i >= s.UnlockedJars) { _c.Sprite("jar_lock", jx, jy); continue; }
                var jar = s.Shelf.Jars[i];
                if (jar.IsEmpty) _c.Sprite("jar_empty", jx, jy);
                else
                {
                    var color = ItemArt.SpeciesColor(jar.Species.Value);
                    _c.Sprite("jar_full", jx, jy, jar.IsReady ? color : Color.Lerp(color, Palette.Grey, 0.5f));
                    if (jar.IsReady && (int)(_time * 2) % 2 == 0) _c.Rect(jx + 4, jy - 3, 2, 2, jar.IsAged ? Palette.Pink : Palette.White);
                }
            }

            // Mortar on a stump, pantry chest.
            _c.Sprite("stump", Layout.Stump.X, Layout.Stump.Y);
            _c.Sprite("mortar", Layout.Mortar.X, Layout.Mortar.Y);
            _c.Sprite("pantry", Layout.Pantry.X, Layout.Pantry.Y);

            if (hover != null && hover.Kind != StationKind.Plot)
                _c.Border(hover.Bounds, Palette.Yellow);
        }
    }
}
