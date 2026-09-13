namespace SpiceWizard.Core;

public sealed class Stats
{
    public int SaucesSold { get; set; }
    public int BlendsSold { get; set; }
    public int PeppercornsEarned { get; set; }
    public int QuotasMet { get; set; }
    public int FiveStarSauces { get; set; }
    public int RushesFilled { get; set; }
}

/// <summary>
/// The peppercorn sign. The renderer draws it as the peppercorn icon wherever it appears in text, so it is
/// used like a currency symbol: <see cref="Pc"/> gives "[icon]5" for a price, cost or payout, and
/// <see cref="Icon"/> goes before the word wherever peppercorns are merely mentioned.
/// </summary>
public static class Peppercorn
{
    public const char Sign = '\uE000';
    public const string Icon = "\uE000";
    public static string Pc(int n) => Icon + n;
    /// <summary>"[icon]+12" / "[icon]-3": a gain or a spend.</summary>
    public static string Signed(int n) => Icon + (n >= 0 ? "+" : "") + n;
}

/// <summary>The whole world. Plain data so it round-trips through JSON for saves.</summary>
public sealed class GameState
{
    public GameClock Clock { get; set; } = new();
    public Inventory Inventory { get; set; } = new();
    public int Peppercorns { get; set; } = Balance.StartingPeppercorns;
    public Garden Garden { get; set; } = new();
    public FermentShelf Shelf { get; set; } = new();
    public ShippingCrate Crate { get; set; } = new();
    public Town Town { get; set; } = new();
    public Quota? Quota { get; set; }
    /// <summary>The open rush order, if any. See <see cref="RushOrder"/>.</summary>
    public RushOrder? Rush { get; set; }
    public Progression Progression { get; set; } = new();
    public SpiceMeter Spice { get; set; } = new();
    public Rng Rng { get; set; } = new();
    public Stats Stats { get; set; } = new();
    /// <summary>Hasten spells cast today; the count resets at dawn. See <see cref="Balance.HastenCastsAt"/>.</summary>
    public int HastensToday { get; set; }
    /// <summary>Today's sky. Rain waters the garden; wind just blows.</summary>
    public Weather Weather { get; set; }
    public bool Won { get; set; }
    public int WonOnDay { get; set; }
    public MorningReport? LastReport { get; set; }

    public int Level => Progression.Level;
    public int UnlockedPlots => Garden.UnlockedPlots(Level);
    public int UnlockedJars => FermentShelf.UnlockedJars(Level);
    public int CrateCapacity => Balance.CrateCapacityAt(Level);
    public int HastenCasts => Balance.HastenCastsAt(Level);
    public int HastensLeft => Math.Max(0, HastenCasts - HastensToday);

    public static GameState NewGame(ulong seed)
    {
        var s = new GameState { Rng = new Rng(seed) };
        s.Inventory.Seeds[(int)PepperSpecies.Bell] = Balance.StartingBellSeeds;
        s.Inventory.Seeds[(int)PepperSpecies.Banana] = Balance.StartingBananaSeeds;
        s.Inventory.Mash[(int)PepperSpecies.Bell] = Balance.StartingBellMash;
        foreach (var spice in Balance.StartingSpices) s.Inventory.Spices[(int)spice]++;
        s.Quota = Quota.Generate(1, 1, s.Rng);
        return s;
    }
}

public sealed class ActionResult
{
    public bool Ok { get; }
    public string Message { get; }

    ActionResult(bool ok, string message) { Ok = ok; Message = message; }

    public static ActionResult Success(string message = "") => new(true, message);
    public static ActionResult Fail(string message) => new(false, message);
}

public sealed class MorningReport
{
    public int Day { get; set; }
    public List<SaleResult> Sales { get; set; } = new();
    public int PeppercornsEarned { get; set; }
    public int XpEarned { get; set; }
    public int LevelsGained { get; set; }
    public int NewLevel { get; set; }
    public bool QuotaEvaluated { get; set; }
    public bool QuotaMet { get; set; }
    public int QuotaBonusPeppercorns { get; set; }
    public int QuotaBonusXp { get; set; }
    public Spice? QuotaBonusSpice { get; set; }
    public bool NewQuotaPosted { get; set; }
    /// <summary>A rush order arrived this morning (it is <see cref="GameState.Rush"/>).</summary>
    public bool RushPosted { get; set; }
    /// <summary>Last night's cart filled the rush order; <see cref="RushRecipeId"/> says which sauce.</summary>
    public bool RushCompleted { get; set; }
    /// <summary>The rush order lapsed unmet last night; <see cref="RushRecipeId"/> says which sauce.</summary>
    public bool RushExpired { get; set; }
    public int RushRecipeId { get; set; }
    public int RushBonusXp { get; set; }
    public int PlantsReady { get; set; }
    public int JarsReady { get; set; }
    public bool BecameMaster { get; set; }
    public Weather Weather { get; set; }
}
