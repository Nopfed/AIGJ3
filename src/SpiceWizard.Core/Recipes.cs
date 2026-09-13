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
        // Hot sauces pay a quarter more than the curry of their tier: fermenting costs a jar and two nights.
        new(1,  "Bell Hot Sauce",  SauceType.Hot,    15, 1,  new[] { Ingredient.Mash(PepperSpecies.Bell), Ingredient.Peppercorns(1) }),
        new(2,  "Golden Curry",    SauceType.Curry,  16, 1,  new[] { Ingredient.Pepper(PepperSpecies.Bell, 2), Ingredient.Of(Spice.Cumin), Ingredient.Of(Spice.Coriander), Ingredient.Of(Spice.CurryLeaves) }),
        new(3,  "Banana Blaze",    SauceType.Hot,    25, 2,  new[] { Ingredient.Mash(PepperSpecies.Banana), Ingredient.Of(Spice.Ginger), Ingredient.Peppercorns(1) }),
        new(4,  "Sunset Curry",    SauceType.Curry,  24, 4,  new[] { Ingredient.Powder(PepperSpecies.Banana), Ingredient.Of(Spice.Cinnamon), Ingredient.Of(Spice.Cardamom), Ingredient.Of(Spice.Ginger) }),
        new(5,  "Bonnet Fire",     SauceType.Hot,    40, 5,  new[] { Ingredient.Mash(PepperSpecies.Bonnet), Ingredient.Of(Spice.Coriander), Ingredient.Of(Spice.Cumin), Ingredient.Peppercorns(2) }),
        new(6,  "Bonnet Curry",    SauceType.Curry,  40, 7,  new[] { Ingredient.Powder(PepperSpecies.Bonnet), Ingredient.Of(Spice.Fenugreek), Ingredient.Of(Spice.Cumin), Ingredient.Of(Spice.CurryLeaves), Ingredient.Of(Spice.Cloves) }),
        new(7,  "Phantom Sauce",   SauceType.Hot,    70, 10, new[] { Ingredient.Mash(PepperSpecies.Ghost), Ingredient.Of(Spice.Cloves), Ingredient.Of(Spice.Cinnamon), Ingredient.Peppercorns(2) }),
        new(8,  "Spectral Curry",  SauceType.Curry,  70, 13, new[] { Ingredient.Powder(PepperSpecies.Ghost), Ingredient.Of(Spice.Cardamom), Ingredient.Of(Spice.Cloves), Ingredient.Of(Spice.Fenugreek), Ingredient.Peppercorns(3) }),
        new(9,  "Rainbow Chutney", SauceType.Hot,    85, 15, new[] { Ingredient.Mash(PepperSpecies.Bonnet), Ingredient.Mash(PepperSpecies.Banana), Ingredient.Of(Spice.Ginger), Ingredient.Of(Spice.Cinnamon), Ingredient.Peppercorns(2) }),
        new(10, "Wizard's Curry",  SauceType.Curry, 100, 17, new[] { Ingredient.Powder(PepperSpecies.Ghost), Ingredient.Powder(PepperSpecies.Bonnet), Ingredient.Of(Spice.Cardamom), Ingredient.Of(Spice.Fenugreek), Ingredient.Of(Spice.Cloves), Ingredient.Peppercorns(3) }),
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
    // The starter kit: enough to bottle a Bell Hot Sauce on day 1 and a Golden Curry after the first harvest,
    // plus one Banana seed so the second temperament is on show from the start.
    public const int StartingBananaSeeds = 1;
    public const int StartingBellMash = 1;
    public static readonly Spice[] StartingSpices = { Spice.Cumin, Spice.Coriander, Spice.CurryLeaves };

    public const int CookSpiceCost = 3;
    public const int GrindSpiceCost = 1;
    public const int PepTalkSpiceCost = 1;
    /// <summary>The hasten spell: costs this much spice and pushes a plant or jar this many nights ahead.</summary>
    public const int HastenSpiceCost = 2;
    public const int HastenNights = 2;
    /// <summary>Casts per day: one, then two from <see cref="SecondHastenLevel"/>.</summary>
    public const int SecondHastenLevel = 16;

    // Fermenting.
    public const int NightsToFerment = 2;
    public const int NightsToAge = 4;
    /// <summary>From this level mash ages a night sooner.</summary>
    public const int QuickAgeLevel = 18;

    // Spice blends mixed at the mortar.
    public const int BlendUnlockLevel = 2;
    public const int BlendSpiceCost = 2;
    public const int MinBlendPinches = 2;
    public const int MaxBlendPinches = 5;
    /// <summary>A blend sells for its pinches' merchant worth times this.</summary>
    public const double BlendValueMultiplier = 1.2;
    /// <summary>A pinch of powder is worth base + per-heat × the pepper's heat (Bell 4 ... Ghost 12).</summary>
    public const int PowderWorthBase = 2;
    public const int PowderWorthPerHeat = 2;
    public const int PeppercornPinchWorth = 2;
    /// <summary>A blend starts plain; kick, aroma and a peppercorn each add a star, no pepper at all loses one.</summary>
    public const int BlendBaseQuality = 2;
    /// <summary>Total powder heat from which a blend earns its "kick" star.</summary>
    public const int BlendKickHeat = 3;
    /// <summary>Distinct spices from which a blend earns its "aromatic" star.</summary>
    public const int BlendAromaticSpices = 3;
    public const int BaseSpiceMax = 8;
    public const int SpiceMaxCap = 14;

    public const int CrateCapacity = 6;
    public const int BigCrateCapacity = 8;
    /// <summary>From this level the crate holds <see cref="BigCrateCapacity"/>.</summary>
    public const int BigCrateLevel = 14;
    public const int BoredomWindowDays = 3;
    public const int BoredomThreshold = 2;
    /// <summary>From this level the town forgives one more repeat before it tires of a sauce.</summary>
    public const int ForgivingTownLevel = 19;
    /// <summary>Percent chance each week that the town craves one sauce type (+1 star for every bottle of it).</summary>
    public const int CravingChance = 50;

    /// <summary>XP per star per recipe tier when a sauce sells.</summary>
    public const int XpPerStarTier = 6;

    /// <summary>Peppercorn multiplier indexed by stars 1..5 (index 0 unused).</summary>
    public static readonly double[] StarMultiplier = { 0, 0.5, 0.8, 1.0, 1.4, 2.0 };

    // Weather: rolled each dawn once the opening days are past. Percent chances; the rest is clear.
    public const int ClearDaysAtStart = 2;
    public const int RainChance = 20;
    public const int WindyChance = 25;

    public const double DayLengthSeconds = 360;
    public const int DayStartMinute = 6 * 60;
    public const int DayEndMinute = 22 * 60;

    public static int XpToNext(int level) => 25 + 18 * (level - 1);
    public static int SpiceMaxAt(int level) => Math.Min(SpiceMaxCap, BaseSpiceMax + (level - 1) / 3);
    public static int CrateCapacityAt(int level) => level >= BigCrateLevel ? BigCrateCapacity : CrateCapacity;
    public static int HastenCastsAt(int level) => level >= SecondHastenLevel ? 2 : 1;
    public static int NightsToAgeAt(int level) => level >= QuickAgeLevel ? NightsToAge - 1 : NightsToAge;
    public static int BoredomThresholdAt(int level) => level >= ForgivingTownLevel ? BoredomThreshold + 1 : BoredomThreshold;
    public static int QuotaBonusPeppercorns(int week) => 40 + 25 * week;
    public static int QuotaBonusXp(int week) => 30 + 10 * week;
}
