using System;
using Microsoft.Xna.Framework;
using SpiceWizard.Core;
using SpiceWizard.Web.Art;

namespace SpiceWizard.Web.Ui
{
    /// <summary>HUD strip, toast, title screen and the level-20 celebration.</summary>
    public static class Overlays
    {
        public static void Hud(Ui ui, GameState s, Session ss, float time)
        {
            var c = ui.C;
            int top = Camera.Top, left = Camera.Left, right = Camera.Right;
            c.Rect(left, top, Camera.View.Width, Scene.Layout.HudHeight, Palette.Outline * 0.75f);
            c.Rect(left, top + Scene.Layout.HudHeight - 1, Camera.View.Width, 1, Palette.Gold * 0.5f);
            c.Text("Day " + s.Clock.Day + "  " + s.Clock.TimeText(), left + 4, top + 4, Palette.Cream);
            if (s.Rush is RushOrder rush)
            {
                // The pip blinks on the due day so the deadline is hard to miss.
                bool dueTonight = rush.DaysLeft(s.Clock.Day) <= 0;
                var tint = dueTonight && (int)(time * 3) % 2 == 0 ? Palette.Yellow : Color.White;
                c.Sprite("ic_envelope", left + 106, top + 3, tint);
                if (ui.Hot(new Rectangle(left + 105, top + 2, 10, 10)))
                    ui.Tooltip = "Rush order: " + rush.Count + "x " + RecipeBook.Get(rush.RecipeId).Name + ", " + rush.DueText(s.Clock.Day) + ". Pays double.";
            }
            string sky = s.Weather == Weather.Rain ? "ic_rain" : s.Weather == Weather.Windy ? "ic_wind" : "ic_sun";
            c.Sprite(sky, left + 90, top + 3);
            if (ui.Hot(new Rectangle(left + 88, top + 2, 12, 10))) ui.Tooltip = WeatherInfo.Name(s.Weather) + ": " + WeatherInfo.Describe(s.Weather);

            c.Sprite("ic_peppercorn", left + 126, top + 3);
            c.Text(s.Peppercorns.ToString(), left + 137, top + 4, Palette.Yellow);

            // The right-hand group hugs the right edge of the window.
            int rx = right - Camera.Width;
            c.Sprite("ic_flame", rx + 176, top + 3);
            ui.Bar(new Rectangle(rx + 187, top + 4, 40, 7), s.Spice.Current / (float)s.Spice.Max, Palette.Orange);
            c.Text(s.Spice.Current + "/" + s.Spice.Max, rx + 230, top + 4, Palette.Cream);
            if (ui.Hot(new Rectangle(rx + 176, top + 2, 80, 10))) ui.Tooltip = "Spice: cooking energy. Eat peppers or sleep.";

            c.Sprite("ic_hat", rx + 268, top + 3);
            c.Text("Lv " + s.Level, rx + 279, top + 4, Palette.LightPurple);
            ui.Bar(new Rectangle(rx + 312, top + 4, 34, 7), (float)s.Progression.Fraction, Palette.LightPurple);
            if (ui.Hot(new Rectangle(rx + 268, top + 2, 78, 10)))
            {
                string next = Progression.UnlockAt(s.Level + 1);
                ui.Tooltip = s.Progression.IsMaster ? "Master Spice Wizard" : s.Progression.Xp + "/" + s.Progression.XpToNext + " fame" + (next.Length > 0 ? ". Next: " + next : "");
            }

            // The moon does what the tower door does: turn in early once the day's chores are done.
            if (ui.Button(new Rectangle(rx + 349, top + 2, 14, 10), "", !ss.PanelOpen, ss.PanelOpen ? null : "Go to bed early", icon: "ic_moon")) ss.Open(PanelKind.Door);
            if (ui.Button(new Rectangle(rx + 366, top + 2, 14, 10), "?", true, "How to play")) ss.Open(PanelKind.Help);
        }

        public static void Toast(Ui ui, Session ss)
        {
            if (ss.ToastTime <= 0 || string.IsNullOrEmpty(ss.Toast)) return;
            float a = Math.Min(1f, ss.ToastTime / 0.5f);
            int w = PixelFont.Measure(ss.Toast) + 10;
            int x = Camera.Width / 2 - w / 2, y = Camera.Bottom - 16;
            ui.C.Rect(x, y, w, 12, Palette.Outline * (0.85f * a));
            ui.C.Text(ss.Toast, x + 5, y + 2, ss.ToastColor * a);
        }

        public static void Title(Ui ui, Session ss, float time)
        {
            var c = ui.C;
            c.Rect(Camera.View, Palette.Outline * 0.55f);
            int cx = Camera.Width / 2;
            c.Rect(Camera.Left, 34, Camera.View.Width, 50, Palette.Outline * 0.45f);
            c.Rect(Camera.Left, 160, Camera.View.Width, 50, Palette.Outline * 0.45f);
            BigText(c, "SPICE WIZARD", cx, 40, 3, Palette.Yellow);
            c.TextCentered("a cooking and farming tale", cx, 72, Palette.Cream);
            int bob = (int)(Math.Sin(time * 2) * 2);
            c.Sprite("wizard0", cx - 6, 86 + bob);
            c.Sprite("ic_bell", cx - 40, 92); c.Sprite("ic_banana", cx - 26, 92);
            c.Sprite("ic_bonnet", cx + 18, 92); c.Sprite("ic_ghost", cx + 32, 92);

            int y = 120;
            if (ss.ConfirmNewGame)
            {
                // Starting over throws the saved game away, so the title asks first.
                c.TextCentered("Start over? Your saved game will be lost.", cx, y - 12, Palette.Yellow);
                if (ui.Button(new Rectangle(cx - 50, y, 100, 16), "Yes, start over", true)) ss.RequestNewGame?.Invoke();
                if (ui.Button(new Rectangle(cx - 50, y + 20, 100, 16), "Back", true)) ss.ConfirmNewGame = false;
            }
            else
            {
                if (ui.Button(new Rectangle(cx - 50, y, 100, 16), "New game", true))
                {
                    if (ss.HasSave) ss.ConfirmNewGame = true;
                    else ss.RequestNewGame?.Invoke();
                }
                if (ss.HasSave && ui.Button(new Rectangle(cx - 50, y + 20, 100, 16), "Continue", true)) ss.RequestContinue?.Invoke();
            }
            c.TextCentered("Grow peppers, brew sauces, feed the town.", cx, 164, Palette.Cream);
            c.TextCentered("Become the Master Spice Wizard by level 20.", cx, 174, Palette.Cream);
            // The replay record squeezes in a line of its own; the footer shuffles down to make room.
            bool record = ss.Settings.BestDay > 0;
            if (record) c.TextCentered("Best so far: Master on day " + ss.Settings.BestDay + ".", cx, 184, Palette.Yellow);
            c.TextCentered("Mouse to play. Esc pauses or closes a panel.", cx, record ? 195 : 188, Palette.LightGrey);
            c.TextCentered("A weekend jam game by Nopfed. Themes: Curry and Pepper.", cx, record ? 206 : 200, Palette.LightGrey);
        }

        public static void Pause(Ui ui, Session ss)
        {
            var c = ui.C;
            int cx = Camera.Width / 2;
            var box = new Rectangle(cx - 60, 60, 120, 96);
            c.Rect(Camera.View, Palette.Outline * 0.55f);
            c.Rect(box.X + 3, box.Y + 3, box.Width, box.Height, Palette.Shadow);
            c.NineSlice("frame", box);
            BigText(c, "PAUSED", cx, box.Y + 8, 2, Palette.Yellow);
            int y = box.Y + 30;
            if (ui.Button(new Rectangle(cx - 50, y, 100, 16), "Resume", true)) ss.Close();
            if (ui.Button(new Rectangle(cx - 50, y + 20, 100, 16), "Options", true)) ss.Open(PanelKind.Options);
            if (ui.Button(new Rectangle(cx - 50, y + 40, 100, 16), "Quit to title", true, "Saves the day so far")) ss.RequestQuit?.Invoke();
        }

        public static void Options(Ui ui, Session ss)
        {
            var c = ui.C;
            int cx = Camera.Width / 2;
            var box = new Rectangle(cx - 90, 48, 180, 120);
            if (ui.Panel(box, "Options")) { ss.Open(PanelKind.Pause); return; }
            var st = ss.Settings;
            int y = box.Y + 26;
            bool changed = false;
            changed |= VolumeRow(ui, box, y, "Music", ref st.Music);
            changed |= VolumeRow(ui, box, y + 22, "Ambience", ref st.Ambience);
            changed |= VolumeRow(ui, box, y + 44, "SFX", ref st.Sfx);
            if (changed) ss.SettingsChanged?.Invoke();
            if (ui.Button(new Rectangle(cx - 30, box.Bottom - 22, 60, 14), "Back", true)) ss.Open(PanelKind.Pause);
        }

        static bool VolumeRow(Ui ui, Rectangle box, int y, string label, ref float value)
        {
            ui.C.Text(label, box.X + 10, y + 3, Palette.Outline);
            bool changed = ui.Slider(new Rectangle(box.X + 62, y, 80, 11), ref value);
            ui.C.TextRight((int)Math.Round(value * 100) + "%", box.Right - 10, y + 3, Palette.Outline);
            return changed;
        }

        public static void Celebration(Ui ui, GameState s, Session ss, float time)
        {
            var c = ui.C;
            int cx = Camera.Width / 2;
            float pulse = 0.85f + 0.15f * (float)Math.Sin(time * 4);
            c.Rect(cx - 120, 18, 240, 40, Palette.DarkRed * 0.9f);
            c.Border(new Rectangle(cx - 120, 18, 240, 40), Palette.Yellow);
            BigText(c, "MASTER", cx, 22, 2, Palette.Yellow * pulse);
            BigText(c, "SPICE WIZARD!", cx, 40, 2, Palette.Yellow * pulse);

            var box = new Rectangle(cx - 150, 112, 300, 84);
            c.Rect(box, Palette.Outline * 0.85f);
            c.Border(box, Palette.Gold * 0.6f);
            c.TextCentered("The whole town came to cheer!", cx, box.Y + 5, Palette.White);
            if (ss.NewRecord) c.TextCentered("Mastered on day " + s.WonOnDay + ". New record!", cx, box.Y + 17, Palette.Yellow * pulse);
            else c.TextCentered("Mastered on day " + s.WonOnDay + (ss.Settings.BestDay > 0 ? ". Best: day " + ss.Settings.BestDay : ""), cx, box.Y + 17, Palette.Cream);
            int sy = box.Y + 31;
            Stat(c, cx - 140, sy, "ic_hot", Palette.Red, s.Stats.SaucesSold + " sauces sold");
            Stat(c, cx + 8, sy, "ic_blend", Palette.LightPurple, s.Stats.BlendsSold + " blends sold");
            Stat(c, cx - 140, sy + 11, "ic_star", Color.White, s.Stats.FiveStarSauces + " five-star bottles");
            Stat(c, cx + 8, sy + 11, "ic_check", Color.White, s.Stats.QuotasMet + " quotas met");
            Stat(c, cx - 140, sy + 22, "ic_peppercorn", Color.White, s.Stats.PeppercornsEarned + " peppercorns earned");
            Stat(c, cx + 8, sy + 22, "ic_envelope", Color.White, s.Stats.RushesFilled + " rush orders filled");
            if (ui.Button(new Rectangle(cx - 84, box.Bottom - 18, 80, 14), "Keep playing", true)) ss.Close();
            if (ui.Button(new Rectangle(cx + 4, box.Bottom - 18, 80, 14), "New game", true)) ss.RequestNewGame?.Invoke();
        }

        static void Stat(Canvas c, int x, int y, string icon, Color tint, string text)
        {
            c.Sprite(icon, x, y, tint);
            c.Text(text, x + 11, y + 1, Palette.Cream);
        }

        /// <summary>Chunky text: the glyphs are drawn scaled up by an integer factor.</summary>
        public static void BigText(Canvas c, string text, int centerX, int y, int scale, Color color)
        {
            int w = text.Length * (PixelFont.Advance * scale) - scale;
            int x = centerX - w / 2;
            foreach (char ch in text)
            {
                if (ch != ' ')
                {
                    var g = c.Atlas.Glyph(ch);
                    c.Batch.Draw(c.Atlas.Texture, new Rectangle(x + scale, y + scale, g.Width * scale, g.Height * scale), g, Palette.Outline);
                    c.Batch.Draw(c.Atlas.Texture, new Rectangle(x, y, g.Width * scale, g.Height * scale), g, color);
                }
                x += PixelFont.Advance * scale;
            }
        }
    }
}
