namespace SpiceWizard.Core;

public sealed class Stats
{
    public int SaucesSold { get; set; }
    public int BlendsSold { get; set; }
    public int PeppercornsEarned { get; set; }
    public int QuotasMet { get; set; }
    public int FiveStarSauces { get; set; }
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
    public Progression Progression { get; set; } = new();
    public SpiceMeter Spice { get; set; } = new();
    public Rng Rng { get; set; } = new();
    public Stats Stats { get; set; } = new();
    public bool Won { get; set; }
    public int WonOnDay { get; set; }
    public MorningReport? LastReport { get; set; }

    public int Level => Progression.Level;
    public int UnlockedPlots => Garden.UnlockedPlots(Level);
    public int UnlockedJars => FermentShelf.UnlockedJars(Level);

    public static GameState NewGame(ulong seed)
    {
        var s = new GameState { Rng = new Rng(seed) };
        s.Inventory.Seeds[(int)PepperSpecies.Bell] = Balance.StartingBellSeeds;
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
    public int PlantsReady { get; set; }
    public int JarsReady { get; set; }
    public bool BecameMaster { get; set; }
}
