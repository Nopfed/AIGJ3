namespace SpiceWizard.Core;

public enum PepperSpecies { Bell, Banana, Bonnet, Ghost }

public enum Spice { Cumin, Cinnamon, CurryLeaves, Coriander, Cardamom, Cloves, Fenugreek, Ginger }

public enum SauceType { Hot, Curry }

/// <summary>Everything the wizard can hold. Peppercorns live in <see cref="GameState.Peppercorns"/>, not here.</summary>
public sealed class Inventory
{
    public const int SpeciesCount = 4;
    public const int SpiceCount = 8;

    public int[] Seeds { get; set; } = new int[SpeciesCount];
    public int[] Peppers { get; set; } = new int[SpeciesCount];
    public int[] Powder { get; set; } = new int[SpeciesCount];
    public int[] Mash { get; set; } = new int[SpeciesCount];
    public int[] AgedMash { get; set; } = new int[SpeciesCount];
    public int[] Spices { get; set; } = new int[SpiceCount];
    public List<Sauce> Sauces { get; set; } = new();

    public int Seed(PepperSpecies s) => Seeds[(int)s];
    public int Pepper(PepperSpecies s) => Peppers[(int)s];
    public int PowderOf(PepperSpecies s) => Powder[(int)s];
    public int MashOf(PepperSpecies s) => Mash[(int)s] + AgedMash[(int)s];
    public int SpiceOf(Spice s) => Spices[(int)s];
}

/// <summary>A cooked sauce. Quality is 1–5 stars before the town has its say.</summary>
public sealed class Sauce
{
    public int RecipeId { get; set; }
    public int Quality { get; set; }
    public int CookedDay { get; set; }

    public Sauce() { }
    public Sauce(int recipeId, int quality, int cookedDay) { RecipeId = recipeId; Quality = quality; CookedDay = cookedDay; }

    public Recipe Recipe => RecipeBook.Get(RecipeId);
}

public static class SpiceInfo
{
    public static readonly string[] Names = { "Cumin", "Cinnamon", "Curry Leaves", "Coriander", "Cardamom", "Cloves", "Fenugreek", "Ginger" };
    public static readonly int[] Prices = { 3, 3, 4, 3, 6, 6, 5, 4 };
    public static readonly int[] UnlockLevels = { 1, 1, 1, 1, 3, 5, 5, 1 };

    /// <summary>Spices handed out as weekly-quota rewards.</summary>
    public static readonly Spice[] Rare = { Spice.Cardamom, Spice.Cloves, Spice.Fenugreek };

    public static string Name(Spice s) => Names[(int)s];
    public static int Price(Spice s) => Prices[(int)s];
    public static int UnlockLevel(Spice s) => UnlockLevels[(int)s];
}
