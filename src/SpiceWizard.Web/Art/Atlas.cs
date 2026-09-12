using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SpiceWizard.Web.Art
{
    /// <summary>
    /// Packs every sprite and font glyph into one texture at load time, so there is no content
    /// pipeline and the art lives in source. Also owns a 1x1 white pixel for rectangles.
    /// </summary>
    public sealed class Atlas : IDisposable
    {
        public const int Size = 512;

        public Texture2D Texture { get; }
        public Rectangle Pixel { get; }
        readonly Dictionary<string, Rectangle> _rects = new Dictionary<string, Rectangle>();
        readonly Rectangle[] _glyphs = new Rectangle[95];

        public Atlas(GraphicsDevice device)
        {
            var pixels = new Color[Size * Size];
            int shelfX = 0, shelfY = 0, shelfH = 0;

            Rectangle Allocate(int w, int h)
            {
                if (shelfX + w > Size) { shelfX = 0; shelfY += shelfH + 1; shelfH = 0; }
                if (shelfY + h > Size) throw new InvalidOperationException("Atlas is full");
                var r = new Rectangle(shelfX, shelfY, w, h);
                shelfX += w + 1;
                shelfH = Math.Max(shelfH, h);
                return r;
            }

            var px = Allocate(1, 1);
            pixels[px.Y * Size + px.X] = Color.White;
            Pixel = px;

            foreach (var kv in Sprites.All)
            {
                string[] rows = kv.Value;
                int w = rows[0].Length, h = rows.Length;
                var r = Allocate(w, h);
                for (int y = 0; y < h; y++)
                {
                    if (rows[y].Length != w) throw new InvalidOperationException("Sprite " + kv.Key + " row " + y + " has the wrong width");
                    for (int x = 0; x < w; x++)
                    {
                        char c = rows[y][x];
                        if (c == '.') continue;
                        if (!Palette.TryGet(c, out var color)) throw new InvalidOperationException("Sprite " + kv.Key + " uses unknown colour '" + c + "'");
                        pixels[(r.Y + y) * Size + r.X + x] = color;
                    }
                }
                _rects[kv.Key] = r;
            }

            for (int i = 0; i < 95; i++)
            {
                var r = Allocate(PixelFont.GlyphWidth, PixelFont.GlyphHeight);
                char ch = (char)(32 + i);
                for (int col = 0; col < PixelFont.GlyphWidth; col++)
                    for (int row = 0; row < PixelFont.GlyphHeight; row++)
                        if (PixelFont.Pixel(ch, col, row))
                            pixels[(r.Y + row) * Size + r.X + col] = Color.White;
                _glyphs[i] = r;
            }

            Texture = new Texture2D(device, Size, Size);
            Texture.SetData(pixels);
        }

        public Rectangle this[string name]
        {
            get
            {
                if (!_rects.TryGetValue(name, out var r)) throw new KeyNotFoundException("No sprite named " + name);
                return r;
            }
        }

        public bool Has(string name) => _rects.ContainsKey(name);

        public Rectangle Glyph(char c)
        {
            if (c < 32 || c > 126) c = '?';
            return _glyphs[c - 32];
        }

        public void Dispose() => Texture.Dispose();
    }
}
