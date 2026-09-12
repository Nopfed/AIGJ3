namespace SpiceWizard.Core;

public enum IngredientKind { Pepper, Powder, Mash, Spice, Peppercorn }

public sealed record Ingredient(IngredientKind Kind, int Index, int Count)
{
    public static Ingredient Pepper(PepperSpecies s, int n = 1) => new(IngredientKind.Pepper, (int)s, n);
    public static Ingredient Powder(PepperSpecies s, int n = 1) => new(IngredientKind.Powder, (int)s, n);
    public static Ingredient Mash(PepperSpecies s, int n = 1) => new(IngredientKind.Mash, (int)s, n);
    public static Ingredient Of(Spice s, int n = 1) => new(IngredientKind.Spice, (int)s, n);
    public static Ingredient Peppercorns(int n) => new(IngredientKind.Peppercorn, 0, n);

    public string Name => Kind switch
    {
        IngredientKind.Pepper => Species.NameOf((PepperSpecies)Index) + " pepper",
        IngredientKind.Powder => Species.NameOf((PepperSpecies)Index) + " powder",
        IngredientKind.Mash => Species.NameOf((PepperSpecies)Index) + " mash",
        IngredientKind.Spice => SpiceInfo.Name((Spice)Index),
        _ => "Peppercorn",
    };
}

public sealed record Recipe(
    int Id,
    string Name,
    SauceType Type,
    int BaseValue,
    int UnlockLevel,
    Ingredient[] Ingredients)
{
    /// <summary>Tier drives XP: 1 for the first two recipes, 2 for the next two, and so on.</summary>
    public int Tier => (Id + 1) / 2;
}

public static class RecipeBook
{
    public static readonly Recipe[] All =
    {
        new(1, "Bell Hot Sauce", SauceType.Hot,   12, 1, new[] { Ingredient.Mash(PepperSpecies.Bell), Ingredient.Peppercorns(1) }),
        new(2, "Golden Curry",   SauceType.Curry, 16, 1, new[] { Ingredient.Pepper(PepperSpecies.Bell, 2), Ingredient.Of(Spice.Cumin), Ingredient.Of(Spice.Coriander), Ingredient.Of(Spice.CurryLeaves) }),
        new(3, "Banana Blaze",   SauceType.Hot,   20, 2, new[] { Ingredient.Mash(PepperSpecies.Banana), Ingredient.Of(Spice.Ginger), Ingredient.Peppercorns(1) }),
        new(4, "Sunset Curry",   SauceType.Curry, 24, 3, new[] { Ingredient.Powder(PepperSpecies.Banana), Ingredient.Of(Spice.Cinnamon), Ingredient.Of(Spice.Cardamom), Ingredient.Of(Spice.Ginger) }),
        new(5, "Bonnet Fire",    SauceType.Hot,   32, 5, new[] { Ingredient.Mash(PepperSpecies.Bonnet), Ingredient.Of(Spice.Coriander), Ingredient.Of(Spice.Cumin), Ingredient.Peppercorns(2) }),
        new(6, "Bonnet Curry",   SauceType.Curry, 40, 6, new[] { Ingredient.Powder(PepperSpecies.Bonnet), Ingredient.Of(Spice.Fenugreek), Ingredient.Of(Spice.Cumin), Ingredient.Of(Spice.CurryLeaves), Ingredient.Of(Spice.Cloves) }),
        new(7, "Phantom Sauce",  SauceType.Hot,   55, 9, new[] { Ingredient.Mash(PepperSpecies.Ghost), Ingredient.Of(Spice.Cloves), Ingredient.Of(Spice.Cinnamon), Ingredient.Peppercorns(2) }),
        new(8, "Spectral Curry", SauceType.Curry, 70, 11, new[] { Ingredient.Powder(PepperSpecies.Ghost), Ingredient.Of(Spice.Cardamom), Ingredient.Of(Spice.Cloves), Ingredient.Of(Spice.Fenugreek), Ingredient.Peppercorns(3) }),
    };

    public static Recipe Get(int id) => All[id - 1];

    public static IEnumerable<Recipe> UnlockedAt(int level) => All.Where(r => r.UnlockLevel <= level);
}

/// <summary>Every tunable number that is not a species or recipe stat. Adjust here, then re-run the bot tests.</summary>
public static class Balance
{
    public const int MaxLevel = 20;
    public const int StartingPeppercorns = 30;
    public const int StartingBellSeeds = 4;

    public const int CookSpiceCost = 3;
    public const int GrindSpiceCost = 1;
    public const int PepTalkSpiceCost = 1;
    public const int BaseSpiceMax = 8;
    public const int SpiceMaxCap = 14;

    public const int CrateCapacity = 6;
    public const int BoredomWindowDays = 3;
    public const int BoredomThreshold = 2;

    /// <summary>XP per star per recipe tier when a sauce sells.</summary>
    public const int XpPerStarTier = 6;

    /// <summary>Peppercorn multiplier indexed by stars 1..5 (index 0 unused).</summary>
    public static readonly double[] StarMultiplier = { 0, 0.5, 0.8, 1.0, 1.4, 2.0 };

    public const double DayLengthSeconds = 240;
    public const int DayStartMinute = 6 * 60;
    public const int DayEndMinute = 22 * 60;

    public static int XpToNext(int level) => 25 + 15 * (level - 1);
    public static int SpiceMaxAt(int level) => Math.Min(SpiceMaxCap, BaseSpiceMax + (level - 1) / 3);
    public static int QuotaBonusPeppercorns(int week) => 40 + 25 * week;
    public static int QuotaBonusXp(int week) => 30 + 10 * week;
}
