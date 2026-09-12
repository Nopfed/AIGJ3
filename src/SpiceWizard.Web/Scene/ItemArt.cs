using Microsoft.Xna.Framework;
using SpiceWizard.Core;
using SpiceWizard.Web.Art;

namespace SpiceWizard.Web.Scene
{
    /// <summary>Which sprite and tint represents each item, so inventory rows look consistent everywhere.</summary>
    public static class ItemArt
    {
        public static string PepperIcon(PepperSpecies s) => s switch
        {
            PepperSpecies.Bell => "ic_bell",
            PepperSpecies.Banana => "ic_banana",
            PepperSpecies.Bonnet => "ic_bonnet",
            _ => "ic_ghost",
        };

        public static Color SpeciesColor(PepperSpecies s) => s switch
        {
            PepperSpecies.Bell => Palette.Gold,
            PepperSpecies.Banana => Palette.Yellow,
            PepperSpecies.Bonnet => Palette.Orange,
            _ => Palette.GhostShade,
        };

        public static Color SpiceColor(Spice s) => s switch
        {
            Spice.Cumin => Palette.Tan,
            Spice.Cinnamon => Palette.DarkOrange,
            Spice.CurryLeaves => Palette.Green,
            Spice.Coriander => Palette.LightGreen,
            Spice.Cardamom => Palette.Teal,
            Spice.Cloves => Palette.DarkBrown,
            Spice.Fenugreek => Palette.Gold,
            _ => Palette.Skin,
        };

        public static Color SauceColor(int recipeId) => recipeId switch
        {
            1 => Palette.Gold,
            2 => Palette.Yellow,
            3 => Palette.Orange,
            4 => Palette.DarkOrange,
            5 => Palette.Red,
            6 => Palette.DarkRed,
            7 => Palette.GhostShade,
            _ => Palette.LightPurple,
        };

        public static string SauceIcon(Recipe r) => r.Type == SauceType.Hot ? "ic_hot" : "ic_curry";

        public static string MatureSprite(PepperSpecies s) => s switch
        {
            PepperSpecies.Bell => "mature_bell",
            PepperSpecies.Banana => "mature_banana",
            PepperSpecies.Bonnet => "mature_bonnet",
            _ => "mature_ghost",
        };

        public static string Stars(int n) => new string('*', n) + new string('.', 5 - n);
    }
}
