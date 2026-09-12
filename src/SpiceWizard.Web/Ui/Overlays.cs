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
            c.Rect(0, 0, Camera.Width, Scene.Layout.HudHeight, Palette.Outline * 0.75f);
            c.Text("Day " + s.Clock.Day + " Wk" + s.Clock.Week + " d" + s.Clock.DayOfWeek + " " + s.Clock.TimeText(), 4, 4, Palette.Cream);

            c.Sprite("ic_peppercorn", 126, 3);
            c.Text(s.Peppercorns.ToString(), 137, 4, Palette.Yellow);

            c.Sprite("ic_flame", 176, 3);
            ui.Bar(new Rectangle(187, 4, 40, 7), s.Spice.Current / (float)s.Spice.Max, Palette.Orange);
            c.Text(s.Spice.Current + "/" + s.Spice.Max, 230, 4, Palette.Cream);
            if (ui.Hot(new Rectangle(176, 2, 80, 10))) ui.Tooltip = "Spice: cooking energy. Eat peppers or sleep.";

            c.Sprite("ic_hat", 268, 3);
            c.Text("Lv " + s.Level, 279, 4, Palette.LightPurple);
            ui.Bar(new Rectangle(312, 4, 46, 7), (float)s.Progression.Fraction, Palette.LightPurple);
            if (ui.Hot(new Rectangle(268, 2, 90, 10)))
            {
                string next = Progression.UnlockAt(s.Level + 1);
                ui.Tooltip = s.Progression.IsMaster ? "Master Spice Wizard" : s.Progression.Xp + "/" + s.Progression.XpToNext + " fame" + (next.Length > 0 ? ". Next: " + next : "");
            }

            if (ui.Button(new Rectangle(366, 2, 14, 10), "?", true, "How to play")) ss.Open(PanelKind.Help);
        }

        public static void Toast(Ui ui, Session ss)
        {
            if (ss.ToastTime <= 0 || string.IsNullOrEmpty(ss.Toast)) return;
            float a = Math.Min(1f, ss.ToastTime / 0.5f);
            int w = PixelFont.Measure(ss.Toast) + 10;
            int x = (Camera.Width - w) / 2, y = Camera.Height - 16;
            ui.C.Rect(x, y, w, 12, Palette.Outline * (0.85f * a));
            ui.C.Text(ss.Toast, x + 5, y + 2, ss.ToastColor * a);
        }

        public static void Title(Ui ui, Session ss, float time)
        {
            var c = ui.C;
            c.Rect(0, 0, Camera.Width, Camera.Height, Palette.Outline * 0.55f);
            int cx = Camera.Width / 2;
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
            c.TextCentered(s.Stats.SaucesSold + " sauces sold, " + s.Stats.FiveStarSauces + " of them five-star", cx, 146, Palette.Cream);
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
