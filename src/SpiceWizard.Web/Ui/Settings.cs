using System;
using System.Globalization;

namespace SpiceWizard.Web.Ui
{
    /// <summary>
    /// Player preferences and the replay record, kept apart from the save so a new game keeps them.
    /// Stored as "music=0.70;ambience=0.60;sfx=0.80;best=28".
    /// </summary>
    public sealed class Settings
    {
        public float Music = 0.7f;
        public float Ambience = 0.6f;
        public float Sfx = 0.8f;
        /// <summary>The earliest day a game reached Master Spice Wizard; 0 until it happens.</summary>
        public int BestDay;

        public string Serialize() =>
            "music=" + F(Music) + ";ambience=" + F(Ambience) + ";sfx=" + F(Sfx) + ";best=" + BestDay;

        /// <summary>Records a mastery on <paramref name="day"/>; true when it beats (or sets) the record.</summary>
        public bool RecordMastery(int day)
        {
            if (BestDay != 0 && day >= BestDay) return false;
            BestDay = day;
            return true;
        }

        public static Settings Parse(string text)
        {
            var s = new Settings();
            if (string.IsNullOrWhiteSpace(text)) return s;
            foreach (var part in text.Split(';'))
            {
                int eq = part.IndexOf('=');
                if (eq < 0) continue;
                if (part.Substring(0, eq) == "best")
                {
                    if (int.TryParse(part.Substring(eq + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out int day)) s.BestDay = Math.Max(0, day);
                    continue;
                }
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
