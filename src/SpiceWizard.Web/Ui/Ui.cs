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
        public bool Down { get; private set; }
        public System.Action OnClick;
        public string Tooltip { get; set; }
        int _wheel;
        Rectangle? _hitClip;

        public Ui(Canvas canvas) { C = canvas; }

        public void Begin(Point mouse, bool clicked, int wheel = 0, bool down = false)
        {
            Mouse = mouse;
            Clicked = clicked;
            Down = down;
            Tooltip = null;
            _wheel = wheel;
            _hitClip = null;
        }

        /// <summary>True over the rectangle and, while inside a scroll region, only for its visible part.</summary>
        public bool Hot(Rectangle r) => r.Contains(Mouse) && (_hitClip == null || _hitClip.Value.Contains(Mouse));

        /// <summary>Takes the click for this frame if the mouse is inside the rectangle.</summary>
        public bool Take(Rectangle r)
        {
            if (Clicked && Hot(r)) { Clicked = false; OnClick?.Invoke(); return true; }
            return false;
        }

        public void ConsumeClick() => Clicked = false;

        /// <summary>A push button. <paramref name="icon"/> is drawn before the label; <paramref name="badge"/> is drawn
        /// after it beside a flame, the way every spice cost or gain is shown ("-2", "+3").</summary>
        public bool Button(Rectangle r, string label, bool enabled = true, string tooltip = null, string icon = null, Color? tint = null, string badge = null)
        {
            bool hot = Hot(r);
            C.Rect(r.X + 1, r.Y + 1, r.Width, r.Height, Palette.Shadow);
            C.NineSlice(enabled ? "button" : "button_dim", r);
            if (enabled && hot) C.Rect(r.X + 1, r.Y + 1, r.Width - 2, r.Height - 2, Palette.White * 0.25f);
            var color = enabled ? Palette.Outline : Palette.Charcoal;
            int w = PixelFont.Measure(label) + (icon != null ? IconGap : 0) + (badge != null ? IconGap + PixelFont.Measure(badge) + 1 : 0);
            int x = r.X + (r.Width - w) / 2;
            int ty = r.Y + (r.Height - PixelFont.GlyphHeight) / 2;
            if (icon != null) { C.Sprite(icon, x, ty - 1, enabled ? (tint ?? Color.White) : Palette.LightGrey); x += IconGap; }
            C.Text(label, x, ty, color);
            x += PixelFont.Measure(label);
            if (badge != null)
            {
                C.Sprite("ic_flame", x + 3, ty - 1, enabled ? Color.White : Palette.LightGrey);
                C.Text(badge, x + IconGap + 1, ty, enabled ? Palette.DarkRed : color);
            }
            if (hot && tooltip != null) Tooltip = tooltip;
            return enabled && Take(r);
        }

        const int IconGap = 11; // an 8px icon plus breathing room before the text that follows it

        public bool SmallButton(int x, int y, string label, bool enabled = true, string tooltip = null) =>
            Button(new Rectangle(x, y, PixelFont.Measure(label) + 10, 12), label, enabled, tooltip);

        public void Label(int x, int y, string text) => C.Text(text, x, y, Palette.Outline);

        /// <summary>A section heading: the same dark red everywhere so the eye learns it.</summary>
        public void Heading(int x, int y, string text) => C.Text(text, x, y, Palette.DarkRed);

        /// <summary>A tick or a cross before a short caption; the caption is green when true and grey when not.</summary>
        public void Flag(int x, int y, bool value, string text)
        {
            C.Sprite(value ? "ic_check" : "ic_cross", x, y);
            C.Text(text, x + 10, y + 1, value ? Palette.Green : Palette.Grey);
        }

        /// <summary>A checkbox with a label; clicking either toggles it. Returns true when toggled.</summary>
        public bool Checkbox(int x, int y, string label, ref bool value)
        {
            C.Rect(x, y, 9, 9, Palette.Outline);
            C.Rect(x + 1, y + 1, 7, 7, value ? Palette.Yellow : Palette.Cream);
            if (value) C.Rect(x + 3, y + 3, 3, 3, Palette.Outline);
            C.Text(label, x + 12, y + 1, Palette.Outline);
            if (!Take(new Rectangle(x, y - 1, 12 + PixelFont.Measure(label), 11))) return false;
            value = !value;
            return true;
        }

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
            C.Rect(Camera.View, Palette.Outline * 0.45f);
            C.Rect(r.X + 3, r.Y + 3, r.Width, r.Height, Palette.Shadow);
            C.NineSlice("frame", r);
            C.Rect(r.X + 4, r.Y + 4, r.Width - 8, 11, Palette.Brown);
            C.Rect(r.X + 4, r.Y + 4, r.Width - 8, 1, Palette.Tan);
            C.Rect(r.X + 4, r.Y + 15, r.Width - 8, 1, Palette.Outline);
            C.TextShadow(title, r.X + 7, r.Y + 6, Palette.Cream);
            var close = new Rectangle(r.Right - 16, r.Y + 4, 12, 11);
            bool hot = Hot(close);
            C.Rect(close, hot ? Palette.Red : Palette.DarkRed);
            C.Border(close, Palette.Outline);
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
            C.Rect(r.X + 1, r.Y + 1, r.Width - 2, r.Height - 2, Palette.Charcoal);
            int w = (int)((r.Width - 2) * MathHelper.Clamp(fraction, 0, 1));
            C.Rect(r.X + 1, r.Y + 1, w, r.Height - 2, fill);
            if (w > 0) C.Rect(r.X + 1, r.Y + 1, w, 1, Color.Lerp(fill, Palette.White, 0.4f));
        }

        /// <summary>A horizontal slider: click or drag along the track to set a 0..1 value. Returns true while it changes.</summary>
        public bool Slider(Rectangle r, ref float value)
        {
            bool hot = Hot(r);
            Bar(new Rectangle(r.X, r.Y + r.Height / 2 - 3, r.Width, 7), value, Palette.Gold);
            int kx = r.X + 1 + (int)((r.Width - 4) * MathHelper.Clamp(value, 0, 1));
            var knob = new Rectangle(kx - 1, r.Y, 5, r.Height);
            C.Rect(knob, hot ? Palette.Cream : Palette.Tan);
            C.Border(knob, Palette.Outline);
            if (hot && (Down || Clicked))
            {
                float v = MathHelper.Clamp((Mouse.X - r.X - 1) / (float)(r.Width - 3), 0f, 1f);
                if (Clicked) { Clicked = false; }
                if (System.Math.Abs(v - value) > 0.001f) { value = v; return true; }
            }
            return false;
        }

        const int WheelStep = 40; // virtual pixels scrolled per mouse-wheel notch (120 units)

        /// <summary>Starts a clipped, scrollable content region. Pass the running scroll offset by ref: it is
        /// clamped to the content height and updated from the mouse wheel while the pointer is over the area.
        /// Draw content with y positions already shifted up by the returned offset, then call <see cref="EndScroll"/>.</summary>
        public int BeginScroll(Rectangle area, ref int scroll, int contentHeight)
        {
            int max = System.Math.Max(0, contentHeight - area.Height);
            if (Hot(area) && _wheel != 0) scroll -= _wheel * WheelStep / 120;
            scroll = MathHelper.Clamp(scroll, 0, max);
            C.PushClip(area);
            _hitClip = area;
            return scroll;
        }

        public void EndScroll(Rectangle area, int scroll, int contentHeight)
        {
            _hitClip = null;
            C.PopClip();
            int max = contentHeight - area.Height;
            if (max > 0) DrawScrollbar(area, scroll, max, contentHeight);
        }

        void DrawScrollbar(Rectangle area, int scroll, int max, int contentHeight)
        {
            int x = area.Right - 3;
            C.Rect(x, area.Y, 3, area.Height, Palette.Shadow * 0.6f);
            int thumbH = System.Math.Max(12, area.Height * area.Height / contentHeight);
            int thumbY = area.Y + (int)((area.Height - thumbH) * (scroll / (float)max));
            C.Rect(x, thumbY, 3, thumbH, Palette.Tan);
        }

        public void DrawTooltip()
        {
            if (Tooltip == null) return;
            int w = PixelFont.Measure(Tooltip) + 6;
            int x = MathHelper.Clamp(Mouse.X + 8, Camera.Left + 2, Camera.Right - w - 2);
            int y = MathHelper.Clamp(Mouse.Y + 10, Camera.Top + 2, Camera.Bottom - 12);
            C.Rect(x, y, w, 11, Palette.Outline * 0.9f);
            C.Border(new Rectangle(x, y, w, 11), Palette.Gold * 0.6f);
            C.Text(Tooltip, x + 3, y + 2, Palette.White);
        }
    }
}
