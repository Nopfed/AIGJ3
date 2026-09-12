using System;
using Microsoft.Xna.Framework;
using SpiceWizard.Core;
using SpiceWizard.Web.Art;

namespace SpiceWizard.Web.Ui
{
    /// <summary>HUD strip, toast, title screen and the level-20 celebration.</summary>
    public static class Overlays
    {
        public static void Hud(Ui ui, GameState s, Session ss)
        {
            var c = ui.C;
            int top = Camera.Top, left = Camera.Left, right = Camera.Right;
            c.Rect(left, top, Camera.View.Width, Scene.Layout.HudHeight, Palette.Outline * 0.75f);
            c.Rect(left, top + Scene.Layout.HudHeight - 1, Camera.View.Width, 1, Palette.Gold * 0.5f);
            c.Text("Day " + s.Clock.Day + " Wk" + s.Clock.Week + " d" + s.Clock.DayOfWeek + " " + s.Clock.TimeText(), left + 4, top + 4, Palette.Cream);

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
            ui.Bar(new Rectangle(rx + 312, top + 4, 46, 7), (float)s.Progression.Fraction, Palette.LightPurple);
            if (ui.Hot(new Rectangle(rx + 268, top + 2, 90, 10)))
            {
                string next = Progression.UnlockAt(s.Level + 1);
                ui.Tooltip = s.Progression.IsMaster ? "Master Spice Wizard" : s.Progression.Xp + "/" + s.Progression.XpToNext + " fame" + (next.Length > 0 ? ". Next: " + next : "");
            }

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
            c.Rect(Camera.Left, 160, Camera.View.Width, 40, Palette.Outline * 0.45f);
            BigText(c, "SPICE WIZARD", cx, 40, 3, Palette.Yellow);
            c.TextCentered("a cooking and farming tale", cx, 72, Palette.Cream);
            int bob = (int)(Math.Sin(time * 2) * 2);
            c.Sprite("wizard0", cx - 6, 86 + bob);
            c.Sprite("ic_bell", cx - 40, 92); c.Sprite("ic_banana", cx - 26, 92);
            c.Sprite("ic_bonnet", cx + 18, 92); c.Sprite("ic_ghost", cx + 32, 92);

            int y = 120;
            if (ui.Button(new Rectangle(cx - 50, y, 100, 16), "New game", true)) ss.RequestNewGame?.Invoke();
            if (ss.HasSave && ui.Button(new Rectangle(cx - 50, y + 20, 100, 16), "Continue", true)) ss.RequestContinue?.Invoke();
            c.TextCentered("Grow peppers, brew sauces, feed the town.", cx, 164, Palette.Cream);
            c.TextCentered("Become the Master Spice Wizard by level 20.", cx, 174, Palette.Cream);
            c.TextCentered("Mouse to play. Esc closes a panel.", cx, 190, Palette.LightGrey);
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

            c.Rect(cx - 116, 120, 232, 62, Palette.Outline * 0.8f);
            c.TextCentered("The whole town came to cheer!", cx, 124, Palette.White);
            c.TextCentered("Mastered on day " + s.WonOnDay, cx, 136, Palette.Cream);
            c.TextCentered(s.Stats.SaucesSold + " sauces and " + s.Stats.BlendsSold + " blends sold, " + s.Stats.FiveStarSauces + " of them five-star", cx, 146, Palette.Cream);
            c.TextCentered(s.Stats.PeppercornsEarned + " peppercorns earned, " + s.Stats.QuotasMet + " quotas met", cx, 156, Palette.Cream);
            if (ui.Button(new Rectangle(cx - 84, 166, 80, 14), "Keep playing", true)) ss.Close();
            if (ui.Button(new Rectangle(cx + 4, 166, 80, 14), "New game", true)) ss.RequestNewGame?.Invoke();
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
