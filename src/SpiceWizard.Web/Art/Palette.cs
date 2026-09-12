using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace SpiceWizard.Web.Art
{
    /// <summary>
    /// The whole game uses one small palette. Sprites are written as text where each character
    /// picks a colour from this table ('.' is transparent).
    /// </summary>
    public static class Palette
    {
        public static readonly Color Outline   = new Color(26, 20, 35);
        public static readonly Color White     = new Color(247, 243, 232);
        public static readonly Color Cream     = new Color(230, 216, 189);
        public static readonly Color LightGrey = new Color(184, 180, 174);
        public static readonly Color Grey      = new Color(110, 106, 115);
        public static readonly Color Stone     = new Color(142, 140, 133);
        public static readonly Color DarkStone = new Color(90, 89, 85);
        public static readonly Color Red       = new Color(217, 65, 47);
        public static readonly Color DarkRed   = new Color(143, 43, 30);
        public static readonly Color Orange    = new Color(232, 118, 43);
        public static readonly Color DarkOrange= new Color(176, 79, 26);
        public static readonly Color Yellow    = new Color(242, 201, 76);
        public static readonly Color Gold      = new Color(201, 148, 42);
        public static readonly Color LightGreen= new Color(139, 195, 74);
        public static readonly Color Green     = new Color(79, 138, 58);
        public static readonly Color DarkGreen = new Color(45, 90, 42);
        public static readonly Color Tan       = new Color(198, 154, 106);
        public static readonly Color Brown     = new Color(138, 90, 52);
        public static readonly Color DarkBrown = new Color(84, 51, 28);
        public static readonly Color Soil      = new Color(107, 74, 47);
        public static readonly Color WetSoil   = new Color(74, 50, 32);
        public static readonly Color LightPurple = new Color(155, 123, 209);
        public static readonly Color Purple    = new Color(90, 61, 138);
        public static readonly Color DarkPurple= new Color(46, 31, 77);
        public static readonly Color Sky       = new Color(127, 184, 230);
        public static readonly Color Blue      = new Color(59, 111, 182);
        public static readonly Color Navy      = new Color(27, 33, 72);
        public static readonly Color GhostPale = new Color(220, 239, 255) * 0.85f;
        public static readonly Color GhostShade= new Color(169, 203, 224) * 0.85f;
        public static readonly Color Skin      = new Color(243, 198, 160);
        public static readonly Color Pink      = new Color(229, 138, 180);
        public static readonly Color Teal      = new Color(58, 156, 138);
        public static readonly Color Charcoal  = new Color(46, 42, 42);
        public static readonly Color Grass     = new Color(98, 160, 72);
        public static readonly Color DarkGrass = new Color(72, 128, 56);
        public static readonly Color Road      = new Color(196, 168, 120);
        // Depth pass: highlights, shadows and distance haze.
        public static readonly Color LightStone  = new Color(178, 176, 168);
        public static readonly Color LightTan    = new Color(224, 194, 150);
        public static readonly Color DeepSoil    = new Color(56, 36, 22);
        public static readonly Color LightGrass  = new Color(126, 184, 90);
        public static readonly Color FarHill     = new Color(118, 158, 168);
        public static readonly Color MidHill     = new Color(92, 146, 104);
        public static readonly Color LightCharcoal = new Color(84, 80, 84);
        public static readonly Color DarkTeal    = new Color(36, 108, 96);
        public static readonly Color DarkSkin    = new Color(214, 158, 118);
        public static readonly Color LightYellow = new Color(252, 234, 160);
        public static readonly Color DarkGold    = new Color(150, 105, 30);
        public static readonly Color PaleBlue    = new Color(176, 214, 242);
        public static readonly Color CloudShade  = new Color(204, 220, 238);
        public static readonly Color DarkPink    = new Color(184, 96, 140);
        public static readonly Color Lilac       = new Color(190, 165, 230);
        public static readonly Color Shadow      = Outline * 0.3f;
        public static readonly Color Glass       = PaleBlue * 0.3f;
        public static readonly Color Glow        = new Color(255, 190, 90) * 0.35f;

        static readonly Dictionary<char, Color> Map = new Dictionary<char, Color>
        {
            ['K'] = Outline,   ['W'] = White,     ['w'] = Cream,      ['g'] = LightGrey,  ['G'] = Grey,
            ['m'] = Stone,     ['M'] = DarkStone, ['r'] = Red,        ['R'] = DarkRed,    ['o'] = Orange,
            ['O'] = DarkOrange,['y'] = Yellow,    ['Y'] = Gold,       ['l'] = LightGreen, ['L'] = Green,
            ['D'] = DarkGreen, ['b'] = Tan,       ['B'] = Brown,      ['d'] = DarkBrown,  ['s'] = Soil,
            ['S'] = WetSoil,   ['p'] = LightPurple,['P'] = Purple,    ['v'] = DarkPurple, ['c'] = Sky,
            ['C'] = Blue,      ['n'] = Navy,      ['t'] = GhostPale,  ['T'] = GhostShade, ['k'] = Skin,
            ['i'] = Pink,      ['e'] = Teal,      ['x'] = Charcoal,   ['a'] = Grass,      ['A'] = DarkGrass,
            ['h'] = Road,
            ['f'] = LightStone,['N'] = LightTan,  ['0'] = DeepSoil,   ['u'] = LightGrass, ['F'] = FarHill,
            ['H'] = MidHill,   ['X'] = LightCharcoal, ['E'] = DarkTeal, ['U'] = DarkSkin, ['Z'] = LightYellow,
            ['J'] = DarkGold,  ['j'] = PaleBlue,  ['z'] = CloudShade, ['I'] = DarkPink,   ['V'] = Lilac,
            ['q'] = Shadow,    ['Q'] = Glass,     ['1'] = Glow,
        };

        public static bool TryGet(char c, out Color color) => Map.TryGetValue(c, out color);
    }
}
