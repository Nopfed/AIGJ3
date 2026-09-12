using Microsoft.Xna.Framework;
using SpiceWizard.Web.Art;

namespace SpiceWizard.Web.Ui
{
    /// <summary>
    /// Tiny immediate-mode UI: every frame the panels redraw themselves from game state and
    /// buttons report whether they were clicked. One click is consumed by the first widget that takes it.
    /// </summary>
    public sealed class Ui
    {
        public Canvas C { get; }
        public Point Mouse { get; private set; }
        public bool Clicked { get; private set; }
        public string Tooltip { get; set; }

        public Ui(Canvas canvas) { C = canvas; }

        public void Begin(Point mouse, bool clicked)
        {
            Mouse = mouse;
            Clicked = clicked;
            Tooltip = null;
        }

        public bool Hot(Rectangle r) => r.Contains(Mouse);

        /// <summary>Takes the click for this frame if the mouse is inside the rectangle.</summary>
        public bool Take(Rectangle r)
        {
            if (Clicked && Hot(r)) { Clicked = false; return true; }
            return false;
        }

        public void ConsumeClick() => Clicked = false;

        public bool Button(Rectangle r, string label, bool enabled = true, string tooltip = null)
        {
            bool hot = Hot(r);
            C.NineSlice(enabled ? "button" : "button_dim", r);
            if (enabled && hot) C.Rect(r.X + 1, r.Y + 1, r.Width - 2, r.Height - 2, Palette.White * 0.25f);
            var color = enabled ? Palette.Outline : Palette.Grey;
            C.TextCentered(label, r.X + r.Width / 2, r.Y + (r.Height - PixelFont.GlyphHeight) / 2, color);
            if (hot && tooltip != null) Tooltip = tooltip;
            return enabled && Take(r);
        }

        public bool SmallButton(int x, int y, string label, bool enabled = true, string tooltip = null) =>
            Button(new Rectangle(x, y, PixelFont.Measure(label) + 10, 12), label, enabled, tooltip);

        public void Label(int x, int y, string text) => C.Text(text, x, y, Palette.Outline);

        /// <summary>Word-wraps to maxChars per line. Returns the y just below the last line.</summary>
        public int Paragraph(int x, int y, int maxChars, string text, Color color)
        {
            foreach (var line in Wrap(text, maxChars))
            {
                C.Text(line, x, y, color);
                y += PixelFont.LineHeight;
            }
            return y;
        }

        public static System.Collections.Generic.List<string> Wrap(string text, int maxChars)
        {
            var lines = new System.Collections.Generic.List<string>();
            var current = new System.Text.StringBuilder();
            foreach (var word in text.Split(' '))
            {
                if (current.Length > 0 && current.Length + 1 + word.Length > maxChars)
                {
                    lines.Add(current.ToString());
                    current.Clear();
                }
                if (current.Length > 0) current.Append(' ');
                current.Append(word);
            }
            if (current.Length > 0) lines.Add(current.ToString());
            return lines;
        }
        public void Label(int x, int y, string text, Color color) => C.Text(text, x, y, color);

        /// <summary>Wooden panel with a title bar and an X button. Returns true when the panel wants to close.</summary>
        public bool Panel(Rectangle r, string title)
        {
            C.Rect(0, 0, Camera.Width, Camera.Height, Palette.Outline * 0.45f);
            C.NineSlice("frame", r);
            C.Rect(r.X + 3, r.Y + 3, r.Width - 6, 11, Palette.Brown);
            C.Text(title, r.X + 6, r.Y + 5, Palette.Cream);
            var close = new Rectangle(r.Right - 15, r.Y + 3, 12, 11);
            bool hot = Hot(close);
            C.Rect(close, hot ? Palette.Red : Palette.DarkRed);
            C.Text("x", close.X + 4, close.Y + 2, Palette.White);
            return Take(close);
        }

        /// <summary>Icon plus text, the standard inventory row.</summary>
        public void IconLabel(int x, int y, string icon, Color tint, string text, Color color)
        {
            C.Sprite(icon, x, y, tint);
            C.Text(text, x + 10, y + 1, color);
        }

        public void Stars(int x, int y, int n)
        {
            for (int i = 0; i < 5; i++) C.Sprite(i < n ? "ic_star" : "ic_star_dim", x + i * 7, y);
        }

        public void Bar(Rectangle r, float fraction, Color fill)
        {
            C.Rect(r, Palette.Outline);
            int w = (int)((r.Width - 2) * MathHelper.Clamp(fraction, 0, 1));
            C.Rect(r.X + 1, r.Y + 1, w, r.Height - 2, fill);
        }

        public void DrawTooltip()
        {
            if (Tooltip == null) return;
            int w = PixelFont.Measure(Tooltip) + 6;
            int x = MathHelper.Clamp(Mouse.X + 8, 2, Camera.Width - w - 2);
            int y = MathHelper.Clamp(Mouse.Y + 10, 2, Camera.Height - 12);
            C.Rect(x, y, w, 11, Palette.Outline * 0.9f);
            C.Text(Tooltip, x + 3, y + 2, Palette.White);
        }
    }
}
