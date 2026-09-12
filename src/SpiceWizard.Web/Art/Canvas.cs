using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SpiceWizard.Web.Art
{
    /// <summary>
    /// The yard is designed at 384x216 virtual pixels. The camera picks the largest integer scale that fits
    /// the browser window and then widens the visible area so the scene fills every pixel: the design sits
    /// centred and the view extends into negative coordinates (more sky, more meadow) around it.
    /// </summary>
    public sealed class Camera
    {
        /// <summary>Design size: everything in <see cref="Scene.Layout"/> is placed inside this box.</summary>
        public const int Width = 384;
        public const int Height = 216;

        public int Scale { get; private set; } = 1;
        /// <summary>Window pixel of the design origin (virtual 0,0).</summary>
        public Point Origin { get; private set; }

        /// <summary>The visible virtual area. Always contains the 384x216 design box.</summary>
        public static Rectangle View { get; private set; } = new Rectangle(0, 0, Width, Height);
        public static int Left => View.Left;
        public static int Top => View.Top;
        public static int Right => View.Right;
        public static int Bottom => View.Bottom;

        public void Fit(int windowWidth, int windowHeight)
        {
            Scale = Math.Max(1, Math.Min(windowWidth / Width, windowHeight / Height));
            // Round the view up so no window pixel is left uncovered; the overhang is at most Scale-1 pixels.
            int vw = (windowWidth + Scale - 1) / Scale;
            int vh = (windowHeight + Scale - 1) / Scale;
            int left = -(vw - Width) / 2;
            int top = -(vh - Height) / 2;
            View = new Rectangle(left, top, vw, vh);
            Origin = new Point((windowWidth - vw * Scale) / 2 - left * Scale, (windowHeight - vh * Scale) / 2 - top * Scale);
        }

        public Matrix Transform => Matrix.CreateScale(Scale, Scale, 1) * Matrix.CreateTranslation(Origin.X, Origin.Y, 0);

        public Point ToVirtual(int windowX, int windowY) =>
            new Point((int)Math.Floor((windowX - Origin.X) / (double)Scale), (int)Math.Floor((windowY - Origin.Y) / (double)Scale));
    }

    /// <summary>All drawing goes through here so the rest of the code talks in sprite names and virtual pixels.</summary>
    public sealed class Canvas
    {
        public SpriteBatch Batch { get; }
        public Atlas Atlas { get; }

        public Canvas(SpriteBatch batch, Atlas atlas) { Batch = batch; Atlas = atlas; }

        public void Sprite(string name, int x, int y) => Sprite(name, x, y, Color.White);

        public void Sprite(string name, int x, int y, Color tint)
        {
            var src = Atlas[name];
            Batch.Draw(Atlas.Texture, new Vector2(x, y), src, tint);
        }

        public void Sprite(string name, int x, int y, Color tint, bool flip)
        {
            var src = Atlas[name];
            Batch.Draw(Atlas.Texture, new Vector2(x, y), src, tint, 0f, Vector2.Zero, 1f,
                flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0f);
        }

        public Point Size(string name) { var r = Atlas[name]; return new Point(r.Width, r.Height); }

        public void Rect(int x, int y, int w, int h, Color color)
        {
            if (w <= 0 || h <= 0) return;
            Batch.Draw(Atlas.Texture, new Rectangle(x, y, w, h), Atlas.Pixel, color);
        }

        public void Rect(Rectangle r, Color color) => Rect(r.X, r.Y, r.Width, r.Height, color);

        public void Border(Rectangle r, Color color)
        {
            Rect(r.X, r.Y, r.Width, 1, color);
            Rect(r.X, r.Bottom - 1, r.Width, 1, color);
            Rect(r.X, r.Y, 1, r.Height, color);
            Rect(r.Right - 1, r.Y, 1, r.Height, color);
        }

        public void Text(string text, int x, int y, Color color)
        {
            foreach (char c in text)
            {
                if (c != ' ')
                    Batch.Draw(Atlas.Texture, new Vector2(x, y), Atlas.Glyph(c), color);
                x += PixelFont.Advance;
            }
        }

        public void TextShadow(string text, int x, int y, Color color)
        {
            Text(text, x + 1, y + 1, Palette.Outline);
            Text(text, x, y, color);
        }

        public void TextCentered(string text, int centerX, int y, Color color) =>
            Text(text, centerX - PixelFont.Measure(text) / 2, y, color);

        public void TextRight(string text, int rightX, int y, Color color) =>
            Text(text, rightX - PixelFont.Measure(text), y, color);

        /// <summary>Draws a 9-slice frame whose source sprite is three 4px cells across and down.</summary>
        public void NineSlice(string name, Rectangle dest)
        {
            var src = Atlas[name];
            int c = src.Width / 3;
            int[] sx = { src.X, src.X + c, src.X + 2 * c };
            int[] sw = { c, c, c };
            int[] dx = { dest.X, dest.X + c, dest.Right - c };
            int[] dw = { c, Math.Max(0, dest.Width - 2 * c), c };
            int[] sy = { src.Y, src.Y + c, src.Y + 2 * c };
            int[] dy = { dest.Y, dest.Y + c, dest.Bottom - c };
            int[] dh = { c, Math.Max(0, dest.Height - 2 * c), c };
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    Batch.Draw(Atlas.Texture, new Rectangle(dx[i], dy[j], dw[i], dh[j]), new Rectangle(sx[i], sy[j], sw[i], c), Color.White);
        }
    }
}
