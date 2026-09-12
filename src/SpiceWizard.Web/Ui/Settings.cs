using System;
using System.Globalization;

namespace SpiceWizard.Web.Ui
{
    /// <summary>Player preferences, kept apart from the save so a new game keeps them. Stored as "music=0.7;ambience=0.6;sfx=0.8".</summary>
    public sealed class Settings
    {
        public float Music = 0.7f;
        public float Ambience = 0.6f;
        public float Sfx = 0.8f;

        public string Serialize() =>
            "music=" + F(Music) + ";ambience=" + F(Ambience) + ";sfx=" + F(Sfx);

        public static Settings Parse(string text)
        {
            var s = new Settings();
            if (string.IsNullOrWhiteSpace(text)) return s;
            foreach (var part in text.Split(';'))
            {
                int eq = part.IndexOf('=');
                if (eq < 0) continue;
                if (!float.TryParse(part.Substring(eq + 1), NumberStyles.Float, CultureInfo.InvariantCulture, out float v)) continue;
                v = Math.Clamp(v, 0f, 1f);
                switch (part.Substring(0, eq))
                {
                    case "music": s.Music = v; break;
                    case "ambience": s.Ambience = v; break;
                    case "sfx": s.Sfx = v; break;
                }
            }
            return s;
        }

        static string F(float v) => v.ToString("0.00", CultureInfo.InvariantCulture);
    }
}
